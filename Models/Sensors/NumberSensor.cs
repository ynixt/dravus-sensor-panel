using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace DravusSensorPanel.Models.Sensors;

public sealed record DateValue(DateTime DateTime, float? Value);

public class NumberSensor : Sensor {
    public ObservableCollection<DateValue> Values { get; } = new();
    public ObservableCollection<DateValue> Mins { get; } = new();
    public ObservableCollection<DateValue> Maxs { get; } = new();

    private float? _value;
    private float? _min;
    private float? _max;

    public void UpdateValue(float? newValue, DateTime updateTime, bool ifZeroReset = false) {
        if ( newValue != null ) {
            Values.Add(new DateValue(updateTime, newValue));

            if ( Values.Count > 240 ) {
                Values.RemoveAt(0);
            }

            if ( ifZeroReset ) {
                if ( !Min.HasValue || Min <= 0 || ( newValue > 0 && newValue < Min.Value ) ) {
                    Min = newValue;
                }
            }
            else {
                Min = Min is null ? newValue : Math.Min(Min.Value, newValue.Value);
            }

            Max = Max is null ? newValue : Math.Max(Max.Value, newValue.Value);
        }

        Value = newValue;
    }

    public float? Value {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public float? Min {
        get => _min;
        set {
            if ( value != null ) {
                Mins.Add(new DateValue(DateTime.Now, value.Value));

                if ( Mins.Count > 100 ) {
                    Mins.RemoveAt(0);
                }
            }

            this.RaiseAndSetIfChanged(ref _min, value);
        }
    }

    public float? Max {
        get => _max;
        set {
            if ( value != null ) {
                Maxs.Add(new DateValue(DateTime.Now, value.Value));

                if ( Maxs.Count > 100 ) {
                    Maxs.RemoveAt(0);
                }
            }

            this.RaiseAndSetIfChanged(ref _max, value);
        }
    }

    public override string ToString() {
        return
            $"Type: {Type} - Hardware: {Hardware} - Name: {Name} - Value: {Value} - Min: {Min?.ToString() ?? "n/a"} - Max: {Max?.ToString() ?? "n/a"}";
    }
}
