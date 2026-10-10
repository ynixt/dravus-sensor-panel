using System;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace DravusSensorPanel.Services.InfoExtractors;

/// <summary>
///     Reads the master volume (0..100, negative when muted).
///     Windows uses Core Audio directly. Linux/macOS call command line tools, so they run in background
///     to not block the UI thread: <see cref="Read" /> returns the last value read.
/// </summary>
public sealed class SystemVolumeReader {
    private static readonly TimeSpan RetryWhenUnavailable = TimeSpan.FromSeconds(30);

    private delegate float? Backend();

    private readonly Backend[] _backends;
    private int _workingBackend = -1;
    private DateTime _nextTry = DateTime.MinValue;
    private Task? _pendingRead;
    private volatile object? _lastValue;

    public SystemVolumeReader() {
        if ( OperatingSystem.IsLinux() ) {
            _backends = [ReadWpctl, ReadPactl, ReadAmixer];
        }
        else if ( OperatingSystem.IsMacOS() ) {
            _backends = [ReadOsascript];
        }
        else {
            _backends = [];
        }
    }

    public float? Read() {
        if ( OperatingSystem.IsWindows() ) {
            return ReadWindows();
        }

        if ( _pendingRead is not { IsCompleted: false } && DateTime.Now >= _nextTry ) {
            _pendingRead = Task.Run(ReadInBackground);
        }

        return _lastValue as float?;
    }

    private void ReadInBackground() {
        if ( _workingBackend >= 0 ) {
            float? value = _backends[_workingBackend]();
            if ( value != null ) {
                _lastValue = value;
                return;
            }

            _workingBackend = -1;
        }

        for ( int i = 0; i < _backends.Length; i++ ) {
            float? value = _backends[i]();
            if ( value != null ) {
                _workingBackend = i;
                _lastValue = value;
                return;
            }
        }

        _lastValue = null;
        _nextTry = DateTime.Now + RetryWhenUnavailable;
    }

    [SupportedOSPlatform("windows")]
    private static float? ReadWindows() {
        using var enumerator = new MMDeviceEnumerator();
        using MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        float volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;

        if ( device.AudioEndpointVolume.Mute ) {
            volume *= -1;
        }

        return volume * 100;
    }

    private static float? ToVolume(float percent, bool muted) {
        percent = Math.Max(percent, 0);
        return muted ? -percent : percent;
    }

    /// <summary>PipeWire. Output: "Volume: 0.40" or "Volume: 0.40 [MUTED]"</summary>
    private static float? ReadWpctl() {
        string? output = ProcessRunner.Run("wpctl", ["get-volume", "@DEFAULT_AUDIO_SINK@"]);
        if ( output == null ) return null;

        Match match = Regex.Match(output, @"Volume:\s*([\d.]+)");
        if ( !match.Success ||
             !float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture,
                 out float volume) ) {
            return null;
        }

        return ToVolume(volume * 100, output.Contains("MUTED", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     PulseAudio (or PipeWire with pipewire-pulse).
    ///     Output: "Volume: front-left: 26214 /  40% / -23.00 dB,   front-right: ..." and "Mute: no"
    /// </summary>
    private static float? ReadPactl() {
        string? volumeOutput = ProcessRunner.Run("pactl", ["get-sink-volume", "@DEFAULT_SINK@"]);
        if ( volumeOutput == null ) return null;

        Match match = Regex.Match(volumeOutput, @"(\d+)%");
        if ( !match.Success || !float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out float volume) ) {
            return null;
        }

        string? muteOutput = ProcessRunner.Run("pactl", ["get-sink-mute", "@DEFAULT_SINK@"]);
        bool muted = muteOutput?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false;

        return ToVolume(volume, muted);
    }

    /// <summary>ALSA. Output: "Front Left: Playback 26214 [40%] [-23.00dB] [on]"</summary>
    private static float? ReadAmixer() {
        string? output = ProcessRunner.Run("amixer", ["get", "Master"]);
        if ( output == null ) return null;

        Match match = Regex.Match(output, @"\[(\d+)%\]");
        if ( !match.Success || !float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out float volume) ) {
            return null;
        }

        return ToVolume(volume, output.Contains("[off]", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Output: "output volume:50, input volume:75, alert volume:100, output muted:false"</summary>
    private static float? ReadOsascript() {
        string? output = ProcessRunner.Run("osascript", ["-e", "get volume settings"]);
        if ( output == null ) return null;

        Match match = Regex.Match(output, @"output volume:(\d+)");
        if ( !match.Success || !float.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out float volume) ) {
            return null;
        }

        return ToVolume(volume, output.Contains("output muted:true", StringComparison.OrdinalIgnoreCase));
    }
}
