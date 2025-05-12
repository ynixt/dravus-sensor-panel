using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DravusSensorPanel.Models;

namespace DravusSensorPanel.Services;

public class SensorPanelService : IDisposable {
    private SensorPanel? _sensorPanel;
    private readonly Dictionary<string, Dictionary<string, Sensor>> _sensorsBySourceAndId;

    public SensorPanel SensorPanel {
        get => _sensorPanel!;
        private set {
            if ( _sensorPanel != null ) {
                _sensorPanel.Items.CollectionChanged -= OnCollectionChanged;
            }

            _sensorPanel = value;
            SensorPanel.Items.CollectionChanged += OnCollectionChanged;
        }
    }

    private readonly SensorPanelFileService _sensorPanelFileService;

    public SensorPanelService(SensorPanelFileService sensorPanelFileService) {
        _sensorsBySourceAndId = new Dictionary<string, Dictionary<string, Sensor>>();
        _sensorPanelFileService = sensorPanelFileService;
    }

    public void LoadCurrentSensorPanel() {
        SensorPanel = _sensorPanelFileService.Load() ?? new SensorPanel();
    }

    public List<Sensor> GetAllSensors(string source) {
        _sensorsBySourceAndId.TryGetValue(source, out Dictionary<string, Sensor>? sensors);

        return sensors?.Values.ToList() ?? Enumerable.Empty<Sensor>().ToList();
    }

    public Sensor? FindSensor(string source, string sourceId) {
        if ( _sensorsBySourceAndId.TryGetValue(source, out Dictionary<string, Sensor>? sensorsById) ) {
            if ( sensorsById.TryGetValue(sourceId, out Sensor? sensor) ) {
                return sensor;
            }
        }

        return null;
    }

    public void Dispose() {
        SensorPanel.Items.CollectionChanged -= OnCollectionChanged;
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        if ( e.NewItems != null ) {
            IEnumerable<Sensor?> sensors = e.NewItems.OfType<PanelItem>().Where(item => item is PanelItemSensor)
                                            .Select(item => ( item as PanelItemSensor )?.Sensor);

            foreach ( Sensor? sensor in sensors ) {
                if ( sensor == null ) continue;

                if ( !_sensorsBySourceAndId.TryGetValue(sensor.Source, out Dictionary<string, Sensor>? sensorsById) ) {
                    sensorsById = new Dictionary<string, Sensor>();
                    _sensorsBySourceAndId[sensor.Source] = sensorsById;
                }

                sensorsById.TryAdd(sensor.SourceId, sensor);
            }
        }

        if ( e.OldItems != null ) {
            IEnumerable<Sensor?> sensors = e.OldItems.OfType<PanelItem>().Where(item => item is PanelItemSensor)
                                            .Select(item => ( item as PanelItemSensor )?.Sensor);

            foreach ( Sensor? sensor in sensors ) {
                if ( sensor == null ) continue;

                if ( _sensorsBySourceAndId.TryGetValue(sensor.Source, out Dictionary<string, Sensor>? sensorsById) ) {
                    sensorsById.Remove(sensor.SourceId);

                    if ( sensorsById.Count == 0 ) {
                        _sensorsBySourceAndId.Remove(sensor.Source);
                    }
                }
            }
        }
    }
}
