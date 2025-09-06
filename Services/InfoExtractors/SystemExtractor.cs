using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using LibreHardwareMonitor.Hardware;
using UnitsNet.Units;

#if WINDOWS
using NAudio.CoreAudioApi; // só é compilado quando o símbolo WINDOWS estiver definido
using System.Runtime.Versioning;
#endif

namespace DravusSensorPanel.Services.InfoExtractors;

public class SystemExtractor : InfoExtractor {
    private const string DateTimeUnitId = "system-datetime";

    private const string DateTimeSourceId = "system-datetime";
    private const string VolumeSourceId = "system-volume";

    public static readonly Dictionary<string, Unit> UnitsByName = new() {
        {
            DateTimeUnitId,
            new UnitFnFormat(DateTimeUnitId, "Date time",
                (obj, pattern) => {
                    if ( obj != null && pattern != null && obj is DateTime dateTime ) {
                        try {
                            return dateTime.ToString(pattern);
                        }
                        catch ( Exception _ ) {
                        }
                    }

                    return obj?.ToString() ?? "";
                })
        },
    };

    public override string SourceName => "system";

    private readonly UnitRepository _unitRepository;
    private bool _started;

    public SystemExtractor(SensorRepository sensorRepository, UnitRepository unitRepository) : base(sensorRepository) {
        _unitRepository = unitRepository;
    }

    public override List<Sensor> Start() {
        if ( !_started ) {
            _started = true;

            SensorRepository.AddSensor(new ObjectSensor {
                Id = Guid.NewGuid().ToString(),
                Source = SourceName,
                SourceId = DateTimeSourceId,
                Type = SensorType.Data,
                Hardware = "System",
                Name = "Date time",
                Unit = UnitsByName[DateTimeUnitId],
                InfoExtractor = this,
            });
            SensorRepository.AddSensor(new NumberSensor {
                Id = Guid.NewGuid().ToString(),
                Source = SourceName,
                SourceId = VolumeSourceId,
                Type = SensorType.Data,
                Hardware = "System",
                Name = "Volume",
                Unit = _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(RatioUnit.Percent))!,
                InfoExtractor = this,
            });
        }

        return Extract();
    }

    protected override void InternalUpdate() {
        Extract();
    }

    public override void Dispose() {
    }

    private List<Sensor> Extract() {
        DateTime extractionTime = DateTime.Now;

        ExtractDateTime(extractionTime);
        ExtractVolume(extractionTime);

        return SensorRepository.GetAllSensors(SourceName);
    }

    private void ExtractDateTime(DateTime _) {
        var sensor = SensorRepository.FindSensor<ObjectSensor>(SourceName, DateTimeSourceId)!;

        if ( !ShouldExtract(sensor) ) return;

        sensor.ObjectValue = DateTime.Now;
    }

    private void ExtractVolume(DateTime extractionTime) {
        var sensor = SensorRepository.FindSensor<NumberSensor>(SourceName, VolumeSourceId)!;
        if (!ShouldExtract(sensor)) return;

        try {
            var (ok, percent, muted) = GetMasterVolume();
            if (!ok) return;

            var value = muted ? -Math.Abs(percent) : Math.Abs(percent);
            sensor.UpdateValue(value, extractionTime);
        }
        catch(Exception ex) {
        }
    }

    private static (bool ok, int percent, bool muted) GetMasterVolume() {
        if (OperatingSystem.IsWindows())
            return GetMasterVolume_Windows();

        if (OperatingSystem.IsMacOS())
            return GetMasterVolume_macOS();

        if (OperatingSystem.IsLinux())
            return GetMasterVolume_Linux();

        return (false, 0, false);
    }

#if WINDOWS
    [SupportedOSPlatform("windows")]
    private static (bool ok, int percent, bool muted) GetMasterVolume_Windows() {
        using var devEnum = new MMDeviceEnumerator();
        using var device = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        var vol = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        var mute = device.AudioEndpointVolume.Mute;
        return (true, Clamp0To100(vol), mute);
    }
#else
    private static (bool ok, int percent, bool muted) GetMasterVolume_Windows()
        => (false, 0, false);
#endif

    private static (bool ok, int percent, bool muted) GetMasterVolume_macOS() {
        // volume 0..100:
        var volStr = RunShell("bash", "-lc", "osascript -e 'output volume of (get volume settings)'")?.Trim();
        if (!int.TryParse(volStr, out var vol)) return (false, 0, false);

        // mute:
        var muteStr = RunShell("bash", "-lc", "osascript -e 'output muted of (get volume settings)'")?.Trim().ToLowerInvariant();
        var muted = muteStr is "true" or "yes" or "on" or "1";

        return (true, Clamp0To100(vol), muted);
    }

    private static (bool ok, int percent, bool muted) GetMasterVolume_Linux() {
        // 1) Try pactl (PulseAudio/PipeWire)
        var muteLine = RunShell("bash", "-lc", "pactl get-sink-mute @DEFAULT_SINK@");
        var volLine = RunShell("bash", "-lc", "pactl get-sink-volume @DEFAULT_SINK@");

        if (!string.IsNullOrWhiteSpace(volLine)) {
            // Ex: "Volume: front-left: 26214 /  40% / -23.00 dB,   front-right: 26214 /  40% / -23.00 dB"
            var m = Regex.Matches(volLine, @"(\d+)%");
            if (m.Count > 0 && int.TryParse(m[0].Groups[1].Value, out var pct))
            {
                var muted = !string.IsNullOrWhiteSpace(muteLine) && muteLine.IndexOf("yes", StringComparison.OrdinalIgnoreCase) >= 0;
                return (true, Clamp0To100(pct), muted);
            }
        }

        // 2) Try wpctl (PipeWire)
        var wp = RunShell("bash", "-lc", "wpctl get-volume @DEFAULT_AUDIO_SINK@");
        if (!string.IsNullOrWhiteSpace(wp)) {
            var mFloat = Regex.Match(wp, @"([0-1](?:\.\d+)?)");
            if (mFloat.Success && double.TryParse(mFloat.Groups[1].Value, System.Globalization.NumberStyles.Float,
                                                  System.Globalization.CultureInfo.InvariantCulture, out var f))
            {
                var pct = (int)Math.Round(f * 100);
                var muted = wp.IndexOf("MUTED", StringComparison.OrdinalIgnoreCase) >= 0;
                return (true, Clamp0To100(pct), muted);
            }
        }

        // 3) Try amixer (ALSA)
        var amixer = RunShell("bash", "-lc", "amixer get Master");
        if (!string.IsNullOrWhiteSpace(amixer)) {
            var mPct = Regex.Match(amixer, @"\[(\d+)%\]");
            if (mPct.Success && int.TryParse(mPct.Groups[1].Value, out var pct))
            {
                var muted = amixer.IndexOf("[off]", StringComparison.OrdinalIgnoreCase) >= 0;
                return (true, Clamp0To100(pct), muted);
            }
        }

        return (false, 0, false);
    }

    private static int Clamp0To100(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);

    private static string? RunShell(string fileName, params string[] args) {
        try {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p == null) return null;

            if (!p.WaitForExit(800)) {
                try { p.Kill(entireProcessTree: true); } catch { }
            }

            return p.StandardOutput.ReadToEnd();
        }
        catch {
            return null;
        }
    }
}
