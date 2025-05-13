using System;
using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
using DravusSensorPanel.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public sealed class PanelItemValue : PanelItemSensor, IPanelItemText, IPanelItemHorizontalSizeable {
    private bool _showUnit = true;
    private Color _foreground = Colors.White;
    private Color _unitForeground = Colors.Gray;
    private int _width = 100;
    private int _fontSize = 14;
    private FontFamily _fontFamily = FontFamily.Default;

    public override SensorPanelItemType Type => SensorPanelItemType.SensorValue;

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

    public bool ShowUnit {
        get => _showUnit;
        set {
            if ( !SetField(ref _showUnit, value) ) return;
            RefreshLabels();
        }
    }

    public Color Foreground {
        get => _foreground;
        set {
            if ( !SetField(ref _foreground, value) ) return;
            this.RaisePropertyChanged(nameof(ForegroundBrush));
        }
    }

    public Color UnitForeground {
        get => _unitForeground;
        set {
            if ( !SetField(ref _unitForeground, value) ) return;
            this.RaisePropertyChanged(nameof(UnitForegroundBrush));
        }
    }

    public IBrush ForegroundBrush => new SolidColorBrush(_foreground);
    public IBrush UnitForegroundBrush => new SolidColorBrush(_unitForeground);

    public string Label {
        get {
            if ( Sensor == null ) return Format(0);

            double raw = ValueType switch {
                PanelItemSensorValueType.Value => Sensor.Value ?? 0,
                PanelItemSensorValueType.Min => Sensor.Min ?? 0,
                PanelItemSensorValueType.Max => Sensor.Max ?? 0,
                _ => 0,
            };

            if ( ShowUnit && Unit != null ) {
                raw = App.ServiceProvider!
                         .GetRequiredService<UnitService>().Convert(raw, Sensor.Unit, Unit);
            }

            return Format(raw);
        }
    }

    public string LabelWithUnit => ShowUnit ? $"{Label} {UnitSymbol}" : Label;

    public override PanelItem Clone() {
        var clone = new PanelItemValue {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,

            Sensor = Sensor,
            Unit = Unit,
            NumDecimalPlaces = NumDecimalPlaces,
            ValueType = ValueType,

            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            UnitForeground = UnitForeground,
            ShowUnit = ShowUnit,
        };

        return clone;
    }

    public override PanelItemValueDto ToDto() {
        return new PanelItemValueDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Type = Type,

            Sensor = Sensor == null ? null : new SensorDto { Source = Sensor.Source, SourceId = Sensor.SourceId },
            Unit = Unit == null
                ? null
                : App.ServiceProvider!.GetRequiredService<UnitService>().GetUnitWithQuantityName(Unit),
            NumDecimalPlaces = NumDecimalPlaces,
            ValueType = ValueType,

            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            UnitForeground = UnitForeground,
            ShowUnit = ShowUnit,
        };
    }

    protected override void SensorValueChanged(float? value) {
        base.SensorValueChanged(value);
        RefreshLabels();
    }

    protected override void UnitChanged(Enum? unit) {
        base.UnitChanged(unit);
        RefreshLabels();
    }

    private void RefreshLabels() {
        this.RaisePropertyChanged(nameof(Label));
        this.RaisePropertyChanged(nameof(LabelWithUnit));
    }
}
