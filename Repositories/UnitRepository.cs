using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models.Units;

namespace DravusSensorPanel.Repositories;

public class UnitRepository {
    private readonly Dictionary<string, Unit> _allUnitsById;

    public UnitRepository(IEnumerable<Dictionary<string, Unit>> allUnits) {
        _allUnitsById = new Dictionary<string, Unit>();

        foreach ( KeyValuePair<string, Unit> keyValuePair in allUnits.SelectMany(allUnitById => allUnitById) ) {
            _allUnitsById[keyValuePair.Key] = keyValuePair.Value;
        }
    }

    public Unit? GetUnitById(string? id) {
        if ( id == null ) return null;

        return _allUnitsById.GetValueOrDefault(id);
    }
}
