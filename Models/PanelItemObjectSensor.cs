using System;
using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public sealed class PanelItemObjectSensor : PanelItemSensor, IPanelItemText, IPanelItemHorizontalSizeable,
    IPanelItemTextAlignment {
    private Color _foreground = Colors.White;
    private int _width = 100;
    private int _fontSize = 14;
    private FontFamily _fontFamily = FontFamily.Default;
    private string? _format;
    private TextAlignment _textAlignment = TextAlignment.Center;
    private string _caseStyle = PanelItemTextTransform.NormalCaseStyle;
    private string? _replaceRegexPattern;
    private string? _replaceRegexReplacement;

    private IDisposable? _subscription;

    public ObjectSensor? ObjectSensor {
        get => Sensor as ObjectSensor;
        set => Sensor = value;
    }

    public override SensorPanelItemType Type => SensorPanelItemType.SensorObject;

    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int FontSize {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    public FontFamily FontFamily {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    public string? Format {
        get => _format;
        set {
            if ( !SetField(ref _format, value) ) return;
            RefreshLabels();
        }
    }

    public string CaseStyle {
        get => _caseStyle;
        set {
            if ( value == null && _caseStyle != null ) return;

            string normalized = PanelItemTextTransform.NormalizeCaseStyle(value);
            if ( !SetField(ref _caseStyle, normalized) ) return;
            RefreshLabels();
        }
    }

    public string? ReplaceRegexPattern {
        get => _replaceRegexPattern;
        set {
            if ( !SetField(ref _replaceRegexPattern, value) ) return;
            RefreshLabels();
        }
    }

    public string? ReplaceRegexReplacement {
        get => _replaceRegexReplacement;
        set {
            if ( !SetField(ref _replaceRegexReplacement, value) ) return;
            RefreshLabels();
        }
    }

    public TextAlignment TextAlignment {
        get => _textAlignment;
        set => SetField(ref _textAlignment, value);
    }

    public Color Foreground {
        get => _foreground;
        set {
            if ( !SetField(ref _foreground, value) ) return;
            this.RaisePropertyChanged(nameof(ForegroundBrush));
        }
    }

    public IBrush ForegroundBrush => new SolidColorBrush(_foreground);

    public string Label {
        get {
            if ( ObjectSensor == null ) return string.Empty;

            string rawLabel;
            if ( ObjectSensor.Unit is UnitFnFormat unitFn ) {
                rawLabel = Format != null
                    ? unitFn.WithValueConverter(ObjectSensor.ObjectValue, Format)
                    : unitFn.EmptyValueConverter(ObjectSensor.ObjectValue);
            }
            else {
                rawLabel = ObjectSensor.NotFormatedValue;
            }

            return PanelItemTextTransform.Apply(rawLabel, CaseStyle, ReplaceRegexPattern, ReplaceRegexReplacement);
        }
    }

    public override PanelItem Clone() {
        var clone = new PanelItemObjectSensor {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Transparency = Transparency,
            Description = Description,
            Sort = Sort,
            Sensor = Sensor,
            Unit = Unit,
            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Format = Format,
            TextAlignment = TextAlignment,
            CaseStyle = CaseStyle,
            ReplaceRegexPattern = ReplaceRegexPattern,
            ReplaceRegexReplacement = ReplaceRegexReplacement,
        };

        return clone;
    }

    public override PanelItemObjectDto ToDto() {
        return new PanelItemObjectDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Transparency = Transparency,
            Description = Description,
            Type = Type,
            Sort = Sort,
            Sensor = Sensor == null ? null : new SensorDto { Source = Sensor.Source, SourceId = Sensor.SourceId },
            Unit = Unit?.ToDto(),
            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Format = Format,
            TextAlignment = TextAlignment,
            CaseStyle = CaseStyle,
            ReplaceRegexPattern = ReplaceRegexPattern,
            ReplaceRegexReplacement = ReplaceRegexReplacement,
        };
    }

    public override void Dispose() {
        base.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

    protected override void UnitChanged(Unit? unit) {
        base.UnitChanged(unit);
        RefreshLabels();
    }

    public override void Reload() {
        base.Reload();
        TrackSensorValueChanges();
    }

    protected override void SensorChanged(Sensor? s) {
        base.SensorChanged(s);
        TrackSensorValueChanges();
    }

    private void RefreshLabels() {
        this.RaisePropertyChanged(nameof(Label));
    }

    private void SensorValueChanged(object? value) {
        RefreshLabels();
    }

    private void TrackSensorValueChanges() {
        _subscription?.Dispose();

        if ( Sensor is ObjectSensor objectSensor ) {
            _subscription = objectSensor.WhenAnyValue(s => s.ObjectValue).Subscribe(SensorValueChanged);
            SensorValueChanged(objectSensor.ObjectValue);
        }
    }
}
