using System;
using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models;
using LibreHardwareMonitor.Hardware;
using UnitsNet.Units;

namespace DravusSensorPanel.Services.InfoExtractor;

public class LibreHardwareExtractor : IInfoExtractor {
    public string SourceName => "libre-hardware";

    private static readonly Computer _computer = new() {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsNetworkEnabled = true,
        IsStorageEnabled = true,
        IsPsuEnabled = true,
        IsBatteryEnabled = true,
    };

    private readonly UpdateVisitor _visitor = new();
    private static readonly Dictionary<string, Sensor> SensorsBySourceId = new();
    private static bool _started;

    public List<Sensor> Start() {
        if ( !_started ) {
            _started = true;
            _computer.Open();
            _computer.Accept(_visitor);
        }

        return Extract();
    }

    public void Update() {
        if ( !_started ) return;
        _computer.Traverse(_visitor);

        Extract();
    }

    public void Dispose() {
        // _computer.Close();
    }

    private List<Sensor> Extract() {
        DateTime currentTime = DateTime.Now;

        foreach ( IHardware? hardware in _computer.Hardware ) {
            foreach ( ISensor? libreSensor in hardware.Sensors ) {
                if ( libreSensor.SensorType == SensorType.Factor ) continue;

                string? sourceId = libreSensor.Identifier.ToString();
                if ( !SensorsBySourceId.TryGetValue(sourceId, out Sensor? sensor) ) {
                    sensor = new Sensor {
                        Id = Guid.NewGuid().ToString(),
                        Source = SourceName,
                        SourceId = sourceId,
                        Type = libreSensor.SensorType,
                        Hardware = hardware.Name,
                        Name = libreSensor.Name,
                        Unit = GetUnit(libreSensor, hardware.HardwareType),
                        InfoExtractor = this,
                    };

                    SensorsBySourceId[sensor.SourceId] = sensor;
                }

                sensor.UpdateValue(libreSensor.Value, currentTime);
            }
        }

        return SensorsBySourceId.Values.ToList();
    }

    private class UpdateVisitor : IVisitor {
        public void VisitComputer(IComputer computer) {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware) {
            hardware.Update();
            foreach ( IHardware subHardware in hardware.SubHardware ) subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) {
            sensor.Accept(this);
        }

        public void VisitParameter(IParameter parameter) {
            parameter.Accept(this);
        }
    }

    private Enum GetUnit(ISensor libreSensor, HardwareType hardwareType) {
        switch ( libreSensor.SensorType ) {
            case SensorType.Fan:
                return RotationalSpeedUnit.RevolutionPerMinute;
            case SensorType.Flow:
                return VolumeFlowUnit.LiterPerHour;
            case SensorType.Power:
                return PowerUnit.Watt;
            case SensorType.Energy:
                return EnergyUnit.WattHour;
            case SensorType.Voltage:
                return ElectricPotentialUnit.Volt;
            case SensorType.Current:
                return ElectricCurrentUnit.Ampere;
            case SensorType.Temperature:
                return TemperatureUnit.DegreeCelsius;
            case SensorType.Clock:
            case SensorType.Frequency:
                return FrequencyUnit.Megahertz;
            case SensorType.Data:
                return InformationUnit.Gigabyte;
            case SensorType.SmallData:
                return InformationUnit.Megabyte;
            case SensorType.Throughput:
                if ( hardwareType is HardwareType.GpuNvidia or HardwareType.GpuIntel or HardwareType.GpuAmd ) {
                    return BitRateUnit.KibibitPerSecond;
                }

                return BitRateUnit.BytePerSecond;
            case SensorType.Load:
            case SensorType.Control:
            case SensorType.Level:
            case SensorType.Humidity:
                return RatioUnit.Percent;
            case SensorType.Factor:
                return RatioUnit.DecimalFraction;
            case SensorType.TimeSpan:
                return DurationUnit.Second;
            case SensorType.Noise:
                return LevelUnit.Decibel;
            case SensorType.Conductivity:
                return ElectricConductivityUnit.SiemensPerMeter;
            default:
                throw new NotSupportedException(
                    $"Sensor type {libreSensor.SensorType} was not mapped para UnitsNet.");
        }
    }
}
