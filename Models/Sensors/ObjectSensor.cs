using System;
using DravusSensorPanel.Models.Units;

namespace DravusSensorPanel.Models.Sensors;

public class ObjectSensor : Sensor {
    private string _notFormatedValue = "";
    private object? _objectValue;

    public string NotFormatedValue {
        get => _notFormatedValue;
        set => SetField(ref _notFormatedValue, value);
    }

    public object? ObjectValue {
        get => _objectValue;
        set {
            if ( !SetField(ref _objectValue, value) ) return;

            if ( Unit is UnitFnFormat unitFnPattern ) {
                NotFormatedValue = unitFnPattern.EmptyValueConverter(value);
            }
            else {
                throw new NotSupportedException();
            }
        }
    }
}
