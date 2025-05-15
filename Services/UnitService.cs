using System;
using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using UnitsNet;

namespace DravusSensorPanel.Services;

public class UnitService {
    private readonly UnitRepository _unitRepository;
    private readonly Dictionary<Enum, List<Enum>> _unitsCacheDictionary = new();

    public UnitService(UnitRepository unitRepository) {
        _unitRepository = unitRepository;
    }

    public double Convert(QuantityValue value, Unit source, Unit target) {
        if ( source is IUnitConvertableToAnotherUnit sourceConvertable &&
             target is IUnitConvertableToAnotherUnit targetConvertable ) {
            return sourceConvertable.ConvertTo(( double ) value, targetConvertable);
        }

        return ( double ) value;
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

    public List<Unit> GetPossibleUnits(Unit unit) {
        if ( unit is UnitUnitsNet unitUnitsNet ) {
            return GetPossibleUnits(unitUnitsNet.Value)
                   .Select(u => _unitRepository.GetUnitById(UnitUnitsNet.GetIdFromEnum(u)))
                   .Where(u => u != null).Cast<Unit>().ToList();
        }

        return [unit];
    }

    private List<Enum> GetPossibleUnits(Enum unit) {
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
