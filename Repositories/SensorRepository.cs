﻿using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models.Sensors;

namespace DravusSensorPanel.Repositories;

public class SensorRepository {
    private readonly Dictionary<string, Dictionary<string, Sensor>> _sensorsBySourceAndId;

    public List<Sensor> GetAllSensors() {
        return _sensorsBySourceAndId.Values.SelectMany(s => s.Values).ToList();
    }

    public List<Sensor> GetAllSensors(string source) {
        _sensorsBySourceAndId.TryGetValue(source, out Dictionary<string, Sensor>? sensors);

        return sensors?.Values.ToList() ?? Enumerable.Empty<Sensor>().ToList();
    }

    public List<T> GetAllSensors<T>(string source) where T : Sensor {
        return GetAllSensors(source).Cast<T>().ToList();
    }

    public Sensor? FindSensor(string source, string sourceId) {
        if ( _sensorsBySourceAndId.TryGetValue(source, out Dictionary<string, Sensor>? sensorsById) ) {
            if ( sensorsById.TryGetValue(sourceId, out Sensor? sensor) ) {
                return sensor;
            }
        }

        return null;
    }

    public T? FindSensor<T>(string source, string sourceId) where T : Sensor {
        return FindSensor(source, sourceId) as T;
    }

    public void AddSensor(Sensor sensor) {
        if ( !_sensorsBySourceAndId.TryGetValue(sensor.Source, out Dictionary<string, Sensor>? sensorsById) ) {
            sensorsById = new Dictionary<string, Sensor>();
            _sensorsBySourceAndId[sensor.Source] = sensorsById;
        }

        sensorsById.TryAdd(sensor.SourceId, sensor);
    }

    public void RemoveSensor(Sensor sensor) {
        if ( _sensorsBySourceAndId.TryGetValue(sensor.Source, out Dictionary<string, Sensor>? sensorsById) ) {
            sensorsById.Remove(sensor.SourceId);

            if ( sensorsById.Count == 0 ) {
                _sensorsBySourceAndId.Remove(sensor.Source);
            }
        }
    }

    public SensorRepository() {
        _sensorsBySourceAndId = new Dictionary<string, Dictionary<string, Sensor>>();
    }
}
