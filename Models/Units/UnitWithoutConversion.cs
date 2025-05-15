namespace DravusSensorPanel.Models.Units;

public record UnitWithoutConversion : Unit {
    public override string Abbreviation => base.Abbreviation!;

    public UnitWithoutConversion(string id, string name, string abbreviation) : base(id, name, abbreviation) {
    }
}
