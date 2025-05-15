using System;
using System.Collections.Generic;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using LibreHardwareMonitor.Hardware;
using NAudio.CoreAudioApi;
using UnitsNet.Units;

namespace DravusSensorPanel.Services.InfoExtractor;

public class SystemExtractor : IInfoExtractor {
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

    public string SourceName => "system";

    private readonly SensorRepository _sensorRepository;
    private readonly UnitRepository _unitRepository;
    private bool _started;

    public SystemExtractor(SensorRepository sensorRepository, UnitRepository unitRepository) {
        _sensorRepository = sensorRepository;
        _unitRepository = unitRepository;
    }

    public List<Sensor> Start() {
        if ( !_started ) {
            _started = true;

            _sensorRepository.AddSensor(new ObjectSensor {
                Id = Guid.NewGuid().ToString(),
                Source = SourceName,
                SourceId = DateTimeSourceId,
                Type = SensorType.Data,
                Hardware = "System",
                Name = "Date time",
                Unit = UnitsByName[DateTimeUnitId],
                InfoExtractor = this,
            });
            _sensorRepository.AddSensor(new NumberSensor {
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

    public void Update() {
        Extract();
    }

    public void Dispose() {
    }

    private List<Sensor> Extract() {
        DateTime extractionTime = DateTime.Now;

        ExtractDateTime(extractionTime);
        ExtractVolume(extractionTime);

        return _sensorRepository.GetAllSensors(SourceName);
    }

    private void ExtractDateTime(DateTime _) {
        _sensorRepository.FindSensor<ObjectSensor>(SourceName, DateTimeSourceId)!.ObjectValue = DateTime.Now;
    }

    private void ExtractVolume(DateTime extractionTime) {
        var sensor = _sensorRepository.FindSensor<NumberSensor>(SourceName, VolumeSourceId)!;

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        float volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;

        if ( device.AudioEndpointVolume.Mute ) {
            volume *= -1;
        }

        sensor.UpdateValue(volume * 100, extractionTime);
    }
}
