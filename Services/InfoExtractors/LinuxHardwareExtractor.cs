using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using LibreHardwareMonitor.Hardware;
using UnitsNet.Units;

namespace DravusSensorPanel.Services.InfoExtractors;

/// <summary>
///     On Linux, Libre Hardware Monitor only gives CPU load, memory and network.
///     This extractor reads what the kernel exposes for regular users:
///     - /sys/class/hwmon: temperatures, fans, voltages, currents, power... (CPU, AMD GPU, NVMe, motherboard, ...)
///     - /sys/class/drm: AMD GPU load and VRAM
///     - /sys/devices/system/cpu: CPU clocks
///     - nvidia-smi: NVIDIA GPU (proprietary driver doesn't use hwmon)
/// </summary>
public class LinuxHardwareExtractor : InfoExtractor {
    public override string SourceName => "linux";

    private static readonly Regex HwmonFileRegex = new(@"^(temp|fan|in|curr|power|freq|energy)(\d+)_(input|average)$");

    private readonly UnitRepository _unitRepository;
    private readonly string _sysRoot;
    private readonly string _procRoot;

    // How to read each sensor (by SourceId). Null means "no value now".
    private readonly Dictionary<string, Func<float?>> _readers = new();

    private readonly NvidiaSmiReader _nvidia = new();
    private readonly HashSet<string> _nvidiaSourceIds = new();
    private bool _started;

    public LinuxHardwareExtractor(SensorRepository sensorRepository, UnitRepository unitRepository)
        : this(sensorRepository, unitRepository, "/sys", "/proc") {
    }

    internal LinuxHardwareExtractor(
        SensorRepository sensorRepository,
        UnitRepository unitRepository,
        string sysRoot,
        string procRoot) : base(sensorRepository) {
        _unitRepository = unitRepository;
        _sysRoot = sysRoot;
        _procRoot = procRoot;
    }

    public override List<Sensor> Start() {
        if ( !_started ) {
            _started = true;

            TryDiscover("hwmon", DiscoverHwmon);
            TryDiscover("amdgpu", DiscoverAmdGpu);
            TryDiscover("cpufreq", DiscoverCpuFrequency);
            TryDiscover("nvidia", DiscoverNvidia);
        }

        return Extract();
    }

    protected override void InternalUpdate() {
        if ( !_started ) return;

        Extract();
    }

    public override void Dispose() {
        // Will be started again in the next update if any NVIDIA sensor is still in use
        _nvidia.Stop();
    }

    private List<Sensor> Extract() {
        DateTime now = DateTime.Now;
        bool nvidiaInUse = false;

        foreach ( (string sourceId, Func<float?> reader) in _readers ) {
            var sensor = SensorRepository.FindSensor<NumberSensor>(SourceName, sourceId);
            if ( sensor == null || !ShouldExtract(sensor) ) continue;

            if ( _nvidiaSourceIds.Contains(sourceId) ) {
                nvidiaInUse = true;
            }

            sensor.UpdateValue(reader(), now);
        }

        if ( nvidiaInUse ) {
            _nvidia.EnsureRunning();
        }
        else {
            _nvidia.Stop();
        }

        return SensorRepository.GetAllSensors(SourceName);
    }

    private static void TryDiscover(string name, Action discover) {
        try {
            discover();
        }
        catch ( Exception e ) {
            Console.WriteLine($"[linux] failed to discover {name} sensors: {e.Message}");
        }
    }

    private void AddSensor(string sourceId, string hardware, string name, SensorType type, Func<float?> reader) {
        if ( _readers.ContainsKey(sourceId) ) return;

        _readers[sourceId] = reader;

        if ( SensorRepository.FindSensor(SourceName, sourceId) != null ) return;

        SensorRepository.AddSensor(new NumberSensor {
            Id = Guid.NewGuid().ToString(),
            Source = SourceName,
            SourceId = sourceId,
            Type = type,
            Hardware = hardware,
            Name = name,
            Unit = GetUnit(type),
            InfoExtractor = this,
        });
    }

#region hwmon

    private void DiscoverHwmon() {
        string hwmonRoot = Path.Combine(_sysRoot, "class", "hwmon");
        if ( !Directory.Exists(hwmonRoot) ) return;

        var usedHardwareNames = new Dictionary<string, int>();

        foreach ( string hwmonDir in Directory.GetDirectories(hwmonRoot).OrderBy(d => d, new NaturalComparer()) ) {
            string chip = ReadText(Path.Combine(hwmonDir, "name")) ?? Path.GetFileName(hwmonDir);
            string? deviceDir = Directory.Exists(Path.Combine(hwmonDir, "device"))
                ? Path.Combine(hwmonDir, "device")
                : null;

            // hwmonN numbers change between boots, so the id uses the device (PCI address, nvme0, coretemp.0...)
            string deviceId = GetDeviceId(hwmonDir);
            string hardware = MakeUnique(GetHardwareName(chip, deviceDir), usedHardwareNames);

            // Most drivers put the files in hwmonN, some old ones in hwmonN/device
            var sensorFiles = new Dictionary<string, string>();
            foreach ( string? dir in new[] { deviceDir, hwmonDir } ) {
                if ( dir == null || !Directory.Exists(dir) ) continue;

                foreach ( string file in Directory.GetFiles(dir) ) {
                    sensorFiles[Path.GetFileName(file)] = file;
                }
            }

            foreach ( (string fileName, string filePath) in sensorFiles.OrderBy(f => f.Key, new NaturalComparer()) ) {
                Match match = HwmonFileRegex.Match(fileName);
                if ( !match.Success ) continue;

                string kind = match.Groups[1].Value;
                string index = match.Groups[2].Value;
                string suffix = match.Groups[3].Value;

                // power1_average and power1_input can both exist: prefer input
                if ( suffix == "average" && sensorFiles.ContainsKey($"{kind}{index}_input") ) continue;

                (SensorType type, float divisor, string defaultName) = kind switch {
                    "temp" => ( SensorType.Temperature, 1000f, "Temperature" ),
                    "fan" => ( SensorType.Fan, 1f, "Fan" ),
                    "in" => ( SensorType.Voltage, 1000f, "Voltage" ),
                    "curr" => ( SensorType.Current, 1000f, "Current" ),
                    "power" => ( SensorType.Power, 1_000_000f, "Power" ),
                    "freq" => ( SensorType.Clock, 1_000_000f, "Clock" ),
                    "energy" => ( SensorType.Energy, 3_600_000_000f, "Energy" ),
                    _ => throw new NotSupportedException(kind),
                };

                // Unreadable sensors (EIO, disabled channels...) are ignored
                if ( ReadNumber(filePath) == null ) continue;

                string? label = ReadText(Path.Combine(Path.GetDirectoryName(filePath)!, $"{kind}{index}_label"));
                string name = string.IsNullOrWhiteSpace(label) ? $"{defaultName} #{index}" : label;
                string sourceId = $"hwmon/{chip}/{deviceId}/{fileName}";

                AddSensor(sourceId, hardware, name, type, () => ReadNumber(filePath) / divisor);
            }
        }
    }

    /// <summary>
    ///     Name of the device behind "dir/device" (a symlink like "../../../0000:03:00.0"), or the dir name.
    /// </summary>
    private static string GetDeviceId(string dir) {
        try {
            string? linkTarget = new DirectoryInfo(Path.Combine(dir, "device")).LinkTarget;
            if ( !string.IsNullOrWhiteSpace(linkTarget) ) {
                return Path.GetFileName(linkTarget.TrimEnd('/'));
            }
        }
        catch ( Exception ) {
            // Not a symlink
        }

        return Path.GetFileName(dir);
    }

    private string GetHardwareName(string chip, string? deviceDir) {
        switch ( chip ) {
            case "coretemp":
            case "k10temp":
            case "zenpower":
                return GetCpuName() ?? "CPU";
            case "amdgpu":
                return "AMD GPU";
            case "nouveau":
                return "NVIDIA GPU";
            case "i915":
            case "xe":
                return "Intel GPU";
            case "acpitz":
                return "ACPI Thermal Zone";
        }

        // nvme, drivetemp: disk model
        if ( deviceDir != null ) {
            string? model = ReadText(Path.Combine(deviceDir, "model"));
            if ( !string.IsNullOrWhiteSpace(model) ) {
                return model;
            }
        }

        return chip;
    }

    private string? _cpuName;

    private string? GetCpuName() {
        if ( _cpuName != null ) return _cpuName;

        string? cpuInfo = ReadText(Path.Combine(_procRoot, "cpuinfo"));
        Match? match = cpuInfo == null ? null : Regex.Match(cpuInfo, @"^model name\s*:\s*(.+)$", RegexOptions.Multiline);

        _cpuName = match is { Success: true } ? match.Groups[1].Value.Trim() : null;
        return _cpuName;
    }

    private static string MakeUnique(string name, Dictionary<string, int> used) {
        used.TryGetValue(name, out int count);
        used[name] = count + 1;

        return count == 0 ? name : $"{name} #{count + 1}";
    }

#endregion

#region AMD GPU (/sys/class/drm)

    private void DiscoverAmdGpu() {
        string drmRoot = Path.Combine(_sysRoot, "class", "drm");
        if ( !Directory.Exists(drmRoot) ) return;

        var usedHardwareNames = new Dictionary<string, int>();

        foreach ( string cardDir in Directory.GetDirectories(drmRoot, "card*")
                                             .Where(d => Regex.IsMatch(Path.GetFileName(d), @"^card\d+$"))
                                             .OrderBy(d => d, new NaturalComparer()) ) {
            string deviceDir = Path.Combine(cardDir, "device");
            if ( ReadText(Path.Combine(deviceDir, "vendor")) != "0x1002" ) continue;

            string busyFile = Path.Combine(deviceDir, "gpu_busy_percent");
            if ( !File.Exists(busyFile) ) continue;

            string deviceId = GetDeviceId(cardDir);
            string hardware = MakeUnique("AMD GPU", usedHardwareNames);
            string prefix = $"amdgpu/{deviceId}/";

            AddSensor(prefix + "gpu_busy_percent", hardware, "GPU Core", SensorType.Load, () => ReadNumber(busyFile));

            string memBusyFile = Path.Combine(deviceDir, "mem_busy_percent");
            if ( ReadNumber(memBusyFile) != null ) {
                AddSensor(prefix + "mem_busy_percent", hardware, "GPU Memory Controller", SensorType.Load,
                    () => ReadNumber(memBusyFile));
            }

            string vramUsedFile = Path.Combine(deviceDir, "mem_info_vram_used");
            string vramTotalFile = Path.Combine(deviceDir, "mem_info_vram_total");
            if ( ReadNumber(vramUsedFile) != null && ReadNumber(vramTotalFile) != null ) {
                const float bytesInMegabyte = 1024f * 1024f;

                AddSensor(prefix + "mem_info_vram_used", hardware, "GPU Memory Used", SensorType.SmallData,
                    () => ReadNumber(vramUsedFile) / bytesInMegabyte);
                AddSensor(prefix + "mem_info_vram_total", hardware, "GPU Memory Total", SensorType.SmallData,
                    () => ReadNumber(vramTotalFile) / bytesInMegabyte);
                AddSensor(prefix + "mem_info_vram_load", hardware, "GPU Memory", SensorType.Load, () => {
                    float? used = ReadNumber(vramUsedFile);
                    float? total = ReadNumber(vramTotalFile);
                    return total is > 0 ? used / total * 100 : null;
                });
            }
        }
    }

#endregion

#region CPU clocks (/sys/devices/system/cpu)

    private void DiscoverCpuFrequency() {
        string cpuRoot = Path.Combine(_sysRoot, "devices", "system", "cpu");
        if ( !Directory.Exists(cpuRoot) ) return;

        string hardware = GetCpuName() ?? "CPU";

        foreach ( string cpuDir in Directory.GetDirectories(cpuRoot, "cpu*")
                                            .Where(d => Regex.IsMatch(Path.GetFileName(d), @"^cpu\d+$"))
                                            .OrderBy(d => d, new NaturalComparer()) ) {
            string freqFile = Path.Combine(cpuDir, "cpufreq", "scaling_cur_freq");
            if ( ReadNumber(freqFile) == null ) continue;

            int cpuNumber = int.Parse(Path.GetFileName(cpuDir)[3..], CultureInfo.InvariantCulture);

            // kHz -> MHz
            AddSensor($"cpufreq/cpu{cpuNumber}", hardware, $"CPU Thread #{cpuNumber + 1}", SensorType.Clock,
                () => ReadNumber(freqFile) / 1000f);
        }
    }

#endregion

#region NVIDIA (nvidia-smi)

    private void DiscoverNvidia() {
        if ( !_nvidia.Discover() ) return;

        var usedHardwareNames = new Dictionary<string, int>();

        foreach ( NvidiaSmiReader.Gpu gpu in _nvidia.Gpus ) {
            string hardware = MakeUnique(gpu.Name, usedHardwareNames);

            foreach ( NvidiaSmiReader.Field field in NvidiaSmiReader.Fields ) {
                string sourceId = $"nvidia/{gpu.Uuid}/{field.Query}";
                string uuid = gpu.Uuid;
                int fieldIndex = Array.IndexOf(NvidiaSmiReader.Fields, field);

                _nvidiaSourceIds.Add(sourceId);
                AddSensor(sourceId, hardware, field.Name, field.Type, () => _nvidia.GetValue(uuid, fieldIndex));
            }

            string memoryLoadId = $"nvidia/{gpu.Uuid}/memory.load";
            _nvidiaSourceIds.Add(memoryLoadId);
            AddSensor(memoryLoadId, hardware, "GPU Memory", SensorType.Load, () => _nvidia.GetMemoryLoad(gpu.Uuid));
        }
    }

    private sealed class NvidiaSmiReader {
        public sealed record Gpu(string Uuid, string Name);

        public sealed record Field(string Query, string Name, SensorType Type);

        public static readonly Field[] Fields = [
            new("temperature.gpu", "GPU Core", SensorType.Temperature),
            new("utilization.gpu", "GPU Core", SensorType.Load),
            new("utilization.memory", "GPU Memory Controller", SensorType.Load),
            new("memory.used", "GPU Memory Used", SensorType.SmallData),
            new("memory.free", "GPU Memory Free", SensorType.SmallData),
            new("memory.total", "GPU Memory Total", SensorType.SmallData),
            new("power.draw", "GPU Package", SensorType.Power),
            new("clocks.gr", "GPU Core", SensorType.Clock),
            new("clocks.mem", "GPU Memory", SensorType.Clock),
            new("fan.speed", "GPU Fan", SensorType.Control),
        ];

        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(10);

        private readonly ConcurrentDictionary<string, float?[]> _valuesByUuid = new();
        private readonly object _lock = new();
        private Process? _process;
        private DateTime _nextStart = DateTime.MinValue;

        public List<Gpu> Gpus { get; } = new();

        public bool Discover() {
            string? output = ProcessRunner.Run("nvidia-smi", ["--query-gpu=uuid,name", "--format=csv,noheader"],
                5000);
            if ( output == null ) return false;

            foreach ( string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries) ) {
                string[] parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
                if ( parts.Length == 2 && parts[0].StartsWith("GPU-", StringComparison.Ordinal) ) {
                    Gpus.Add(new Gpu(parts[0], parts[1]));
                }
            }

            return Gpus.Count > 0;
        }

        public float? GetValue(string uuid, int fieldIndex) {
            return _valuesByUuid.TryGetValue(uuid, out float?[]? values) ? values[fieldIndex] : null;
        }

        public float? GetMemoryLoad(string uuid) {
            int used = Array.FindIndex(Fields, f => f.Query == "memory.used");
            int total = Array.FindIndex(Fields, f => f.Query == "memory.total");
            float? usedValue = GetValue(uuid, used);
            float? totalValue = GetValue(uuid, total);

            return totalValue is > 0 ? usedValue / totalValue * 100 : null;
        }

        /// <summary>
        ///     Keeps a single nvidia-smi running in loop mode (one line per GPU each second) instead of
        ///     starting a new process every update.
        /// </summary>
        public void EnsureRunning() {
            lock ( _lock ) {
                if ( _process is { HasExited: false } || DateTime.Now < _nextStart ) return;

                _process?.Dispose();
                _process = null;
                _nextStart = DateTime.Now + RestartDelay;

                string query = "--query-gpu=uuid," + string.Join(',', Fields.Select(f => f.Query));

                try {
                    var process = new Process {
                        StartInfo = ProcessRunner.CreateStartInfo("nvidia-smi",
                            [query, "--format=csv,noheader,nounits", "-lms", "1000"], true),
                    };
                    process.OutputDataReceived += (_, e) => ParseLine(e.Data);
                    process.ErrorDataReceived += (_, _) => { };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    _process = process;
                }
                catch ( Exception e ) {
                    Console.WriteLine($"[linux] failed to start nvidia-smi: {e.Message}");
                }
            }
        }

        public void Stop() {
            lock ( _lock ) {
                if ( _process == null ) return;

                ProcessRunner.Kill(_process);
                _process.Dispose();
                _process = null;
                _nextStart = DateTime.MinValue;
                _valuesByUuid.Clear();
            }
        }

        private void ParseLine(string? line) {
            if ( string.IsNullOrWhiteSpace(line) ) return;

            string[] parts = line.Split(',', StringSplitOptions.TrimEntries);
            if ( parts.Length != Fields.Length + 1 ) return;

            var values = new float?[Fields.Length];
            for ( int i = 0; i < Fields.Length; i++ ) {
                // "[N/A]" and "[Not Supported]" become null
                values[i] = float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out float value)
                    ? value
                    : null;
            }

            _valuesByUuid[parts[0]] = values;
        }
    }

#endregion

#region Helpers

    private static string? ReadText(string path) {
        try {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch ( Exception ) {
            return null;
        }
    }

    private static float? ReadNumber(string path) {
        string? text = ReadText(path);

        return text != null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
            out double value)
            ? ( float ) value
            : null;
    }

    private Unit GetUnit(SensorType type) {
        Enum unit = type switch {
            SensorType.Temperature => TemperatureUnit.DegreeCelsius,
            SensorType.Fan => RotationalSpeedUnit.RevolutionPerMinute,
            SensorType.Voltage => ElectricPotentialUnit.Volt,
            SensorType.Current => ElectricCurrentUnit.Ampere,
            SensorType.Power => PowerUnit.Watt,
            SensorType.Clock => FrequencyUnit.Megahertz,
            SensorType.Energy => EnergyUnit.WattHour,
            SensorType.SmallData => InformationUnit.Megabyte,
            SensorType.Load or SensorType.Control => RatioUnit.Percent,
            _ => throw new NotSupportedException($"Sensor type {type} was not mapped to UnitsNet."),
        };

        return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(unit))!;
    }

    /// <summary>Sorts "hwmon2" before "hwmon10" and "temp2_input" before "temp10_input".</summary>
    private sealed class NaturalComparer : IComparer<string> {
        public int Compare(string? x, string? y) {
            return string.Compare(Pad(x), Pad(y), StringComparison.Ordinal);
        }

        private static string Pad(string? s) {
            return Regex.Replace(s ?? "", @"\d+", m => m.Value.PadLeft(10, '0'));
        }
    }

#endregion
}
