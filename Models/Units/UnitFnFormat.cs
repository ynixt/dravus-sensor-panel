namespace DravusSensorPanel.Models.Units;

public delegate string UnitFnWithValueConverter(object? value, string? format);

public delegate double UnitFnWithValueConverterToNumber(object? value, string? format);

public delegate string UnitFnEmptyFormatConverter(object? value);

public record UnitFnFormat : Unit {
    public UnitFnWithValueConverter WithValueConverter { get; }
    public UnitFnEmptyFormatConverter EmptyValueConverter { get; }
    public UnitFnWithValueConverterToNumber? WithValueConverterToNumber { get; }

    public UnitFnFormat(
        string id,
        string name,
        UnitFnWithValueConverter withValueConverter,
        UnitFnEmptyFormatConverter? defaultStringStringConverter = null,
        UnitFnWithValueConverterToNumber? withValueConverterToNumber = null,
        string? abbreviation = null) : base(id, name, abbreviation) {
        WithValueConverter = withValueConverter;
        WithValueConverterToNumber = withValueConverterToNumber;
        EmptyValueConverter = defaultStringStringConverter ?? ( obj => obj?.ToString() ?? "null" );
    }
}
