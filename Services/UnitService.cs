using System;
using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Enums;
using UnitsNet;

namespace DravusSensorPanel.Services;

public class UnitService {
    private readonly Dictionary<string, Enum> _unitsByNameAndQuantityName = new();
    private readonly Dictionary<Enum, List<Enum>> _unitsCacheDictionary = new();

    public UnitService() {
        foreach ( UnitInfo unitInfo in Quantity
                                       .Infos
                                       .SelectMany(q => q.UnitInfos) ) {
            _unitsByNameAndQuantityName[JoinUnitAndQuantityName(unitInfo.QuantityName, unitInfo.Value)] =
                unitInfo.Value;
        }

        _unitsByNameAndQuantityName[JoinUnitAndQuantityName(nameof(FrameUnit), FrameUnit.Fps)] = FrameUnit.Fps;
    }

    public Enum? GetUnitFromNameAndQuantityName(string? nameAndQuantityName) {
        if ( nameAndQuantityName == null ) return null;

        return _unitsByNameAndQuantityName.GetValueOrDefault(nameAndQuantityName);
    }

    public string GetUnitWithQuantityName(Enum unit) {
        try {
            UnitInfo? unitInfo = Quantity.GetUnitInfo(unit);

            return JoinUnitAndQuantityName(unitInfo.QuantityName, unit);
        }
        catch ( Exception e ) {
            if ( Equals(unit, FrameUnit.Fps) ) {
                return JoinUnitAndQuantityName(nameof(FrameUnit), unit);
            }

            return unit.ToString();
        }
    }

    public double Convert(QuantityValue value, Enum fromUnitValue, Enum toUnitValue) {
        if ( Equals(fromUnitValue, toUnitValue) ) {
            return ( double ) value;
        }

        try {
            return UnitConverter.Convert(value, fromUnitValue, toUnitValue);
        }
        catch ( Exception ex ) {
            return ( double ) value;
        }
    }

    public string GetAbbreviation(Enum unit) {
        try {
            return UnitAbbreviationsCache.Default.GetDefaultAbbreviation(unit);
        }
        catch ( Exception ex ) {
            Console.WriteLine($"Unable to get abbreviation for {unit}");
            return unit.ToString();
        }
    }

    public IList<Enum> GetPossibleUnits(Enum unit) {
        if ( _unitsCacheDictionary.TryGetValue(unit, out List<Enum>? units) ) return units;

        Type unitType = unit.GetType();

        try {
            QuantityInfo? quantityInfo = Quantity.Infos.FirstOrDefault(q =>
                q.UnitType == unitType);

            if ( quantityInfo != null ) {
                units = quantityInfo.UnitInfos
                                    .Select(u => new {
                                        Unit = u.Value,
                                        ValueToSort = Convert(1, quantityInfo.UnitInfos[0].Value, u.Value),
                                    })
                                    .OrderByDescending(x => x.ValueToSort)
                                    .Select(u => u.Unit)
                                    .ToList();
            }
            else {
                units = [unit];
            }
        }
        catch ( Exception ex ) {
            units = [unit];
        }

        _unitsCacheDictionary[unit] = units!;

        return units!;
    }

    private string JoinUnitAndQuantityName(string? quantityName, Enum unit) {
        return quantityName + "---" + unit;
    }
}
