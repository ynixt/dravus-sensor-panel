namespace DravusSensorPanel.Models.Units;

public interface IUnitConvertableToAnotherUnit {
    public double ConvertTo(double value, IUnitConvertableToAnotherUnit target);
}
