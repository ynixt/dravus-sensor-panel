using System;
using System.Collections.Generic;
using DravusSensorPanel.Models;
using DravusSensorPanel.Repositories;
using LibreHardwareMonitor.Hardware;
using UnitsNet.Units;

namespace DravusSensorPanel.Services.InfoExtractor;

public class LibreHardwareExtractor : IInfoExtractor {
    public string SourceName => "libre-hardware";

    private readonly Computer _computer = new() {
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
    private readonly SensorRepository _sensorRepository;
    private bool _started;

    public LibreHardwareExtractor(SensorRepository sensorRepository) {
        _sensorRepository = sensorRepository;
    }

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
                Sensor? sensor = _sensorRepository.FindSensor(SourceName, sourceId);

                if ( sensor == null ) {
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

                    _sensorRepository.AddSensor(sensor);
                }

                sensor.UpdateValue(libreSensor.Value, currentTime);
            }
        }

        return _sensorRepository.GetAllSensors(SourceName);
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
