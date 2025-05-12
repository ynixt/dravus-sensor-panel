using System;
using System.Collections.Generic;
using System.Linq;
using UnitsNet;

namespace DravusSensorPanel.Services;

public class UnitService {
    private readonly Dictionary<Enum, List<Enum>> _unitsCacheDictionary = new();

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
}
