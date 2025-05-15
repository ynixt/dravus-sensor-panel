using System;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Services.InfoExtractor;
using LibreHardwareMonitor.Hardware;

namespace DravusSensorPanel.Models.Sensors;

public abstract class Sensor : SuperReactiveObject, IComparable {
    public string Id { get; set; }
    public IInfoExtractor InfoExtractor { get; set; }
    public string Source { get; set; }
    public string SourceId { get; set; }
    public SensorType Type { get; set; }
    public string Hardware { get; set; }
    public string Name { get; set; }
    public Unit Unit { get; set; }


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
