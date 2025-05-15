using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;

namespace DravusSensorPanel.Models;

public abstract class PanelItemSensor : PanelItem {
    protected Sensor? _sensor;
    protected Unit? _unit;

    public Sensor? Sensor {
        get => _sensor;
        set {
            if ( !SetField(ref _sensor, value) ) return;

            SensorChanged(value);
        }
    }

    public Unit? Unit {
        get => _unit;
        set {
            if ( !SetField(ref _unit, value) ) return;
            UnitChanged(value);
        }
    }

    protected virtual void SensorChanged(Sensor? _) {
    }

    protected virtual void UnitChanged(Unit? _) {
    }
}
