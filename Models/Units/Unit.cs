using DravusSensorPanel.Models.Dtos;

namespace DravusSensorPanel.Models.Units;

public abstract record Unit {
    public string Id { get; }
    public string Name { get; }
    public virtual string? Abbreviation { get; }

    protected Unit(string id, string name, string? abbreviation = null) {
        Id = id;
        Name = name;
        Abbreviation = abbreviation;
    }

    public virtual UnitDto ToDto() {
        return new UnitDto { Id = Id };
    }
}
