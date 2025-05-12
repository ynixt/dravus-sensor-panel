using System;
using System.Collections.ObjectModel;
using DravusSensorPanel.Services.InfoExtractor;
using LibreHardwareMonitor.Hardware;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public sealed record DateValue(DateTime DateTime, float? Value);

public class Sensor : ReactiveObject, IComparable {
    public string Id { get; set; }
    public IInfoExtractor InfoExtractor { get; set; }
    public string Source { get; set; }
    public string SourceId { get; set; }

    public SensorType Type { get; set; }
    public string Hardware { get; set; }
    public string Name { get; set; }
    public Enum Unit { get; set; }
    public ObservableCollection<DateValue> Values { get; } = new();
    public ObservableCollection<DateValue> Mins { get; } = new();
    public ObservableCollection<DateValue> Maxs { get; } = new();

    private float? _value;
    private float? _min;
    private float? _max;

    public void UpdateValue(float? newValue, DateTime updateTime) {
        if ( newValue != null ) {
            Values.Add(new DateValue(updateTime, newValue));

            if ( Values.Count > 240 ) {
                Values.RemoveAt(0);
            }

            if ( !Min.HasValue || Min <= 0 || ( newValue > 0 && newValue < Min.Value ) ) {
                Min = newValue;
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

    public int CompareTo(object? obj) {
        if ( obj is not Sensor other ) {
            return 0;
        }

        int result = string.Compare(Hardware, other.Hardware, StringComparison.Ordinal);

        if ( result != 0 ) {
            return result;
        }

        result = string.Compare(Type.ToString(), other.Type.ToString(), StringComparison.Ordinal);

        if ( result != 0 ) {
            return result;
        }

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}
