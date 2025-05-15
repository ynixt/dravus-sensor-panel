using System;
using System.Collections.Generic;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
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
    private readonly UnitRepository _unitRepository;
    private bool _started;

    public LibreHardwareExtractor(SensorRepository sensorRepository, UnitRepository unitRepository) {
        _sensorRepository = sensorRepository;
        _unitRepository = unitRepository;
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
                var sensor = _sensorRepository.FindSensor<NumberSensor>(SourceName, sourceId);

                if ( sensor == null ) {
                    sensor = new NumberSensor {
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

    private Unit GetUnit(ISensor libreSensor, HardwareType hardwareType) {
        switch ( libreSensor.SensorType ) {
            case SensorType.Fan:
                return _unitRepository.GetUnitById(
                    UnitUnitsNet.GetIdFromEnum(RotationalSpeedUnit.RevolutionPerMinute))!;
            case SensorType.Flow:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(VolumeFlowUnit.LiterPerHour))!;
            case SensorType.Power:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(PowerUnit.Watt))!;
            case SensorType.Energy:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(EnergyUnit.WattHour))!;
            case SensorType.Voltage:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(ElectricPotentialUnit.Volt))!;
            case SensorType.Current:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(ElectricCurrentUnit.Ampere))!;
            case SensorType.Temperature:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(TemperatureUnit.DegreeCelsius))!;
            case SensorType.Clock:
            case SensorType.Frequency:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(FrequencyUnit.Megahertz))!;
            case SensorType.Data:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(InformationUnit.Gigabyte))!;
            case SensorType.SmallData:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(InformationUnit.Megabyte))!;
            case SensorType.Throughput:
                if ( hardwareType is HardwareType.GpuNvidia or HardwareType.GpuIntel or HardwareType.GpuAmd ) {
                    return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(BitRateUnit.KibibitPerSecond))!;
                }

                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(BitRateUnit.BytePerSecond))!;
            case SensorType.Load:
            case SensorType.Control:
            case SensorType.Level:
            case SensorType.Humidity:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(RatioUnit.Percent))!;
            case SensorType.Factor:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(RatioUnit.DecimalFraction))!;
            case SensorType.TimeSpan:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(DurationUnit.Second))!;
            case SensorType.Noise:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(LevelUnit.Decibel))!;
            case SensorType.Conductivity:
                return _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(ElectricConductivityUnit.SiemensPerMeter))
                    !;
            default:
                throw new NotSupportedException(
                    $"Sensor type {libreSensor.SensorType} was not mapped para UnitsNet.");
        }
    }
}
