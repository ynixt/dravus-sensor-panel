using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnitsNet;

namespace DravusSensorPanel.Models.Units;

public record UnitUnitsNet : Unit, IUnitConvertableToAnotherUnit {
    public Enum Value { get; }
    public override string Abbreviation => base.Abbreviation!;

    public static string GetIdFromEnum(Enum unit) {
        try {
            UnitInfo? unitInfo = Quantity.GetUnitInfo(unit);

            return JoinUnitAndQuantityName(unitInfo.QuantityName, unit);
        }
        catch ( Exception e ) {
            return unit.ToString();
        }
    }

    public static string GetNameFromEnum(Enum unit) {
        try {
            UnitInfo? unitInfo = Quantity.GetUnitInfo(unit);

            if ( unitInfo != null ) {
                string name = Regex.Replace(unitInfo.Name, "([a-z0-9])([A-Z])", "$1 $2").Trim()
                                   .ToLower(CultureInfo.CurrentCulture);

                return char.ToUpper(name[0], CultureInfo.CurrentCulture)
                       + name.Substring(1);
            }
        }
        catch ( Exception _ ) {
        }

        return unit.ToString();
    }

    private static string JoinUnitAndQuantityName(string? quantityName, Enum unit) {
        return quantityName + "---" + unit;
    }

    public UnitUnitsNet(Enum value) : base(GetIdFromEnum(value),
        GetNameFromEnum(value), UnitAbbreviationsCache.Default.GetDefaultAbbreviation(value)) {
        Value = value;
    }


    public double ConvertTo(double value, IUnitConvertableToAnotherUnit target) {
        if ( Equals(this, target) ) {
            return value;
        }

        if ( target is not UnitUnitsNet targetUnitEnum ) {
            return value;
        }

        try {
            return UnitConverter.Convert(value, Value, targetUnitEnum.Value);
        }
        catch ( Exception ex ) {
            return value;
        }
    }
}
