using System;
using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DravusSensorPanel.Services.InfoExtractors;

public abstract class InfoExtractor : IDisposable {
    private static bool _byPassNoUse;

    protected SensorRepository SensorRepository;

    public static bool ByPassNoUse {
        get => _byPassNoUse;
        set {
            if ( _byPassNoUse != value ) {
                _byPassNoUse = value;

                if ( !_byPassNoUse ) {
                    ClearDataOfAllNotUsedSensors();
                }
            }
        }
    }

    public abstract string SourceName { get; }

    public InfoExtractor(SensorRepository sensorRepository) {
        SensorRepository = sensorRepository;
    }

    private static void ClearDataOfAllNotUsedSensors() {
        IEnumerable<Sensor>? allSensorsNotInUse = App
                                                  .ServiceProvider?.GetRequiredService<SensorRepository>()
                                                  .GetAllSensors()
                                                  .Where(s => !s.InUse);

        if ( allSensorsNotInUse != null ) {
            foreach ( Sensor sensor in allSensorsNotInUse ) {
                if ( sensor is NumberSensor numberSensor ) {
                    numberSensor.ClearAllValues();
                }
            }
        }
    }

    public abstract List<Sensor> Start();

    public void Update() {
        if ( ShouldExtract() ) {
            InternalUpdate();
        }
    }

    public abstract void Dispose();

    public List<Sensor> GetSensors() {
        return SensorRepository.GetAllSensors(SourceName);
    }

    protected abstract void InternalUpdate();

    protected bool ShouldExtract() {
        return ByPassNoUse || GetSensors().FirstOrDefault(s => s.InUse) != null;
    }

    protected bool ShouldExtract(Sensor s) {
        return ByPassNoUse || s.InUse;
    }
}
