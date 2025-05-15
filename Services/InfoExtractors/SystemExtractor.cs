using System;
using System.Collections.Generic;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using LibreHardwareMonitor.Hardware;
using NAudio.CoreAudioApi;
using UnitsNet.Units;

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

        if ( !ShouldExtract(sensor) ) return;

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        float volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;

        if ( device.AudioEndpointVolume.Mute ) {
            volume *= -1;
        }

        sensor.UpdateValue(volume * 100, extractionTime);
    }
}
