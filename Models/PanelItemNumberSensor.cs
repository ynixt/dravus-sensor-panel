using System;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public abstract class PanelItemNumberSensor : PanelItemSensor {
    private int _numDecimalsPlace = 1;
    private PanelItemSensorValueType _valueType = PanelItemSensorValueType.Value;
    private IDisposable? _subscription;

    public int NumDecimalPlaces {
        get => _numDecimalsPlace;
        set => SetField(ref _numDecimalsPlace, value);
    }

    public PanelItemSensorValueType ValueType {
        get => _valueType;
        set => SetField(ref _valueType, value);
    }

    public string UnitSymbol => Unit?.Abbreviation ?? string.Empty;

    public override void Reload() {
        base.Reload();
        TrackSensorValueChanges();
    }

    public override void Dispose() {
        base.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

    protected override void SensorChanged(Sensor? s) {
        base.SensorChanged(s);
        TrackSensorValueChanges();
    }

    protected override void UnitChanged(Unit? u) {
        base.UnitChanged(u);
        this.RaisePropertyChanged(nameof(UnitSymbol));
    }

    protected virtual void SensorValueChanged(float? value) {
    }

    protected string Format(double value) {
        double rounded = Math.Round(value, NumDecimalPlaces, MidpointRounding.ToEven);
        return rounded.ToString($"F{NumDecimalPlaces}");
    }

    private void TrackSensorValueChanges() {
        _subscription?.Dispose();

        if ( Sensor is NumberSensor numberSensor ) {
            if ( Sensor != null ) {
                _subscription = numberSensor.WhenAnyValue(s => s.Value).Subscribe(SensorValueChanged);
            }

            SensorValueChanged(numberSensor?.Value);
        }
    }
}
