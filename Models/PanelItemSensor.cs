using System;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public abstract class PanelItemSensor : PanelItem {
    private Sensor? _sensor;
    private Enum? _unit;
    private int _numDecimalsPlace = 1;
    private PanelItemSensorValueType _valueType = PanelItemSensorValueType.Value;
    private IDisposable? _subscription;

    public Sensor? Sensor {
        get => _sensor;
        set {
            if ( !SetField(ref _sensor, value) ) return;

            SensorChanged(value);
        }
    }

    public Enum? Unit {
        get => _unit;
        set {
            if ( !SetField(ref _unit, value) ) return;
            this.RaisePropertyChanged(nameof(UnitSymbol));
            UnitChanged(value);
        }
    }

    public int NumDecimalPlaces {
        get => _numDecimalsPlace;
        set => SetField(ref _numDecimalsPlace, value);
    }

    public PanelItemSensorValueType ValueType {
        get => _valueType;
        set => SetField(ref _valueType, value);
    }

    public string UnitSymbol => Unit == null
        ? string.Empty
        : App.ServiceProvider!
             .GetRequiredService<UnitService>().GetAbbreviation(Unit);

    public override void Reload() {
        base.Reload();
        TrackSensorValueChanges();
    }

    public override void Dispose() {
        base.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

    protected virtual void SensorChanged(Sensor? _) {
        TrackSensorValueChanges();
    }

    protected virtual void UnitChanged(Enum? _) {
    }

    protected virtual void SensorValueChanged(float? value) {
    }

    protected string Format(double value) {
        double rounded = Math.Round(value, NumDecimalPlaces, MidpointRounding.ToEven);
        return rounded.ToString($"F{NumDecimalPlaces}");
    }

    private void TrackSensorValueChanges() {
        _subscription?.Dispose();

        if ( Sensor != null ) {
            _subscription = Sensor.WhenAnyValue(s => s.Value).Subscribe(SensorValueChanged);
        }

        SensorValueChanged(Sensor?.Value);
    }
}
