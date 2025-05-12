using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SkiaSharp;
using YamlDotNet.Serialization;

namespace DravusSensorPanel.Models;

#region Interfaces

public interface IPanelItemSizeable : IPanelItemHorizontalSizeable, IPanelItemVerticalSizeable {
    public new int Width { get; set; }
    public new int Height { get; set; }
}

public interface IPanelItemHorizontalSizeable {
    public int Width { get; set; }
}

public interface IPanelItemVerticalSizeable {
    public int Height { get; set; }
}

public interface IPanelItemText {
    string Label { get; }
    int FontSize { get; set; }
    FontFamily FontFamily { get; set; }
    Color Foreground { get; set; }

    [YamlIgnore] IBrush ForegroundBrush { get; }
}

public interface IPanelItemEditableText : IPanelItemText {
    new string Label { get; set; }
}

#endregion

#region Base class

public abstract class PanelItem : ReactiveObject, IDisposable {
    private string _description = string.Empty;

    [YamlMember(Order = 1)] public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [YamlMember(Order = 2)] public int X { get; set; }
    [YamlMember(Order = 3)] public int Y { get; set; }
    [YamlMember(Order = 4)] public int ZIndex { get; set; }

    [YamlIgnore] public abstract SensorPanelItemType Type { get; }

    [YamlMember(Order = 0)]
    public string Description {
        get => _description;
        set => SetField(ref _description, value);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if ( EqualityComparer<T>.Default.Equals(field, value) ) return false;
        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        return true;
    }

    public virtual void Dispose() {
    }

    public abstract PanelItem Clone();
}

#endregion

#region Label item

public sealed class PanelItemLabel : PanelItem, IPanelItemEditableText {
    private string _label = string.Empty;
    private Color _foreground = Colors.White;
    private SolidColorBrush? _cachedBrush;

    public override SensorPanelItemType Type => SensorPanelItemType.Label;

    [YamlMember] public int FontSize { get; set; } = 14;
    [YamlMember] public FontFamily FontFamily { get; set; } = FontFamily.Default;

    [YamlIgnore] public IBrush ForegroundBrush => _cachedBrush ??= new SolidColorBrush(_foreground);

    public string Label {
        get => _label;
        set => SetField(ref _label, value);
    }

    public Color Foreground {
        get => _foreground;
        set {
            if ( !SetField(ref _foreground, value) ) return;
            _cachedBrush = new SolidColorBrush(value);
            this.RaisePropertyChanged(nameof(ForegroundBrush));
        }
    }

    public override PanelItem Clone() {
        return new PanelItemLabel {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Label = Label,
        };
    }
}

#endregion

#region Sensor base

public sealed record SensorRef(string Source, string SourceId);

public abstract class PanelItemSensor : PanelItem {
    private Sensor? _sensor;
    private Enum? _unit;
    private int _numDecimalsPlace = 1;
    private PanelItemSensorValueType _valueType = PanelItemSensorValueType.Value;

    [YamlIgnore]
    public Sensor? Sensor {
        get => _sensor;
        set {
            if ( !SetField(ref _sensor, value) ) return;

            _sensorRef = value == null ? null : new SensorRef(value.Source, value.SourceId);

            SensorChanged(value);
        }
    }

    [YamlMember(Order = 20, Alias = "sensor")]
    public SensorRef? SensorPersist {
        get => _sensorRef;
        set {
            _sensorRef = value;
            // TODO
            Sensor = value == null
                ? null
                : App.ServiceProvider!
                     .GetRequiredService<SensorPanelService>()
                     .FindSensor(value.Source, value.SourceId);
        }
    }

    private SensorRef? _sensorRef;

    public Enum? Unit {
        get => _unit;
        set {
            if ( !SetField(ref _unit, value) ) return;
            this.RaisePropertyChanged(nameof(UnitSymbol));
            UnitChanged(value);
        }
    }

    public int NumDecimalPlaces {
        get => _numDecimalsPlace;
        set => SetField(ref _numDecimalsPlace, value);
    }

    public PanelItemSensorValueType ValueType {
        get => _valueType;
        set => SetField(ref _valueType, value);
    }

    [YamlIgnore]
    public string UnitSymbol => Unit == null
        ? string.Empty
        : App.ServiceProvider!
             .GetRequiredService<UnitService>().GetAbbreviation(Unit);

    public abstract void WatchSensorValueChange();

    protected virtual void SensorChanged(Sensor? _) {
    }

    protected virtual void UnitChanged(Enum? _) {
    }

    protected string Format(double value) {
        double rounded = Math.Round(value, NumDecimalPlaces, MidpointRounding.ToEven);
        return rounded.ToString($"F{NumDecimalPlaces}");
    }

    protected string Format() {
        return Format(Sensor?.Value ?? 0);
    }
}

#endregion

#region Value item

public sealed class PanelItemValue : PanelItemSensor, IPanelItemText {
    private bool _showUnit = true;
    private Color _foreground = Colors.White;
    private Color _unitForeground = Colors.Gray;

    private IDisposable? _subscription;

    public override SensorPanelItemType Type => SensorPanelItemType.SensorValue;

    [YamlMember] public int Width { get; set; } = 100;
    [YamlMember] public int FontSize { get; set; } = 14;
    [YamlMember] public FontFamily FontFamily { get; set; } = FontFamily.Default;

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

    [YamlIgnore] public IBrush ForegroundBrush => new SolidColorBrush(_foreground);
    [YamlIgnore] public IBrush UnitForegroundBrush => new SolidColorBrush(_unitForeground);

    [YamlIgnore]
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

    [YamlIgnore] public string LabelWithUnit => ShowUnit ? $"{Label} {UnitSymbol}" : Label;

    public override void WatchSensorValueChange() {
        _subscription?.Dispose();
        if ( Sensor != null ) {
            _subscription = Sensor.WhenAnyValue(s => s.Value).Subscribe(_ => RefreshLabels());
        }

        RefreshLabels();
    }

    protected override void UnitChanged(Enum? _) {
        RefreshLabels();
    }

    protected override void SensorChanged(Sensor? newSensor) {
        WatchSensorValueChange();
    }

    private void RefreshLabels() {
        this.RaisePropertyChanged(nameof(Label));
        this.RaisePropertyChanged(nameof(LabelWithUnit));
    }

    public override void Dispose() {
        base.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

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
}

#endregion

#region Chart item

public sealed class PanelItemChart : PanelItemSensor, IPanelItemSizeable {
    private IDisposable? _subscription;

    public override SensorPanelItemType Type => SensorPanelItemType.SensorChart;

    [YamlMember] public int Width { get; set; } = 300;
    [YamlMember] public int Height { get; set; } = 150;
    [YamlMember] public double? YMinValue { get; set; }
    [YamlMember] public double? YMaxValue { get; set; }
    [YamlMember] public Color Stroke { get; set; } = Colors.Lime;
    [YamlMember] public Color Fill { get; set; } = Colors.Transparent;
    [YamlMember] public double LineSmoothness { get; set; } = 0.6;
    [YamlMember] public double MinStep { get; set; } = 1;
    [YamlMember] public bool ShowYAxis { get; set; }
    [YamlMember] public bool ShowXAxis { get; set; }

    [YamlIgnore] public ISeries[] Series { get; private set; } = [];

    [YamlIgnore] public ICartesianAxis[] YAxes { get; private set; } = [];

    [YamlIgnore] public ICartesianAxis[] XAxes { get; private set; } = [];

    protected override void UnitChanged(Enum? _) {
        UpdateChart();
    }

    protected override void SensorChanged(Sensor? newSensor) {
        WatchSensorValueChange();
    }

    public override void WatchSensorValueChange() {
        _subscription?.Dispose();
        if ( Sensor != null ) {
            _subscription = Sensor.WhenAnyValue(s => s.Value).Subscribe(_ => UpdateChart());
        }

        UpdateChart();
    }

    public override void Dispose() {
        base.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

    private void UpdateChart() {
        BuildSeries();
        BuildAxes();
    }

    private void BuildSeries() {
        const int limit = 100;
        if ( Sensor == null ) {
            Series = [];
            return;
        }

        IEnumerable<DateValue> source = ValueType switch {
            PanelItemSensorValueType.Value => Sensor.Values,
            PanelItemSensorValueType.Min => Sensor.Mins,
            PanelItemSensorValueType.Max => Sensor.Maxs,
            _ => Enumerable.Empty<DateValue>(),
        };

        bool convert = Unit != null;
        UnitService? unitSvc = convert
            ? App.ServiceProvider!
                 .GetRequiredService<UnitService>()
            : null;
        Enum sensorUnit = Sensor.Unit;
        Enum? targetUnit = Unit;

        var points = new List<DateTimePoint>(limit);
        DateTime? last = null;

        foreach ( DateValue dv in source ) {
            if ( dv.Value == null ) continue;
            if ( last != null && ( dv.DateTime - last.Value ).TotalSeconds < MinStep ) continue;
            last = dv.DateTime;

            double val = dv.Value.Value;
            if ( convert && unitSvc != null ) {
                val = unitSvc.Convert(val, sensorUnit, targetUnit);
            }

            val = Math.Round(val, NumDecimalPlaces, MidpointRounding.ToEven);

            points.Add(new DateTimePoint(dv.DateTime, val));
            if ( points.Count > limit ) points.RemoveAt(0);
        }

        Series = [
            new LineSeries<DateTimePoint> {
                LineSmoothness = LineSmoothness,
                Stroke =
                    Stroke.A == 0 ? null : new SolidColorPaint(new SKColor(Stroke.R, Stroke.G, Stroke.B, Stroke.A)),
                Fill = Fill.A == 0 ? null : new SolidColorPaint(new SKColor(Fill.R, Fill.G, Fill.B, Fill.A)),
                GeometryFill = null,
                GeometryStroke = null,
                Values = points,
            },
        ];

        this.RaisePropertyChanged(nameof(Series));
    }

    private void BuildAxes() {
        XAxes = [
            new Axis {
                Labeler = ticks => new DateTime(( long ) ticks).ToString("HH:mm:ss"),
                IsVisible = ShowXAxis,
                ShowSeparatorLines = false,
            },
        ];
        this.RaisePropertyChanged(nameof(XAxes));

        YAxes = [
            new Axis {
                MinLimit = YMinValue,
                MaxLimit = YMaxValue,
                Labeler = v => $"{v} {UnitSymbol}",
                ShowSeparatorLines = false,
                IsVisible = ShowYAxis,
            },
        ];
        this.RaisePropertyChanged(nameof(YAxes));
    }

    public override PanelItem Clone() {
        var clone = new PanelItemChart {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,

            Sensor = Sensor, // reutiliza o mesmo sensor
            Unit = Unit,
            NumDecimalPlaces = NumDecimalPlaces,
            ValueType = ValueType,

            Width = Width,
            Height = Height,
            YMinValue = YMinValue,
            YMaxValue = YMaxValue,
            Stroke = Stroke,
            Fill = Fill,
            LineSmoothness = LineSmoothness,
            MinStep = MinStep,
            ShowYAxis = ShowYAxis,
            ShowXAxis = ShowXAxis,
        };

        clone.UpdateChart();

        return clone;
    }
}

#endregion

#region Image item

public sealed class PanelItemImage : PanelItem, IPanelItemSizeable {
    private string _imagePath = string.Empty;
    private Bitmap? _bitmap;
    private int _width;
    private int _height;

    public override SensorPanelItemType Type => SensorPanelItemType.Image;

    [YamlMember]
    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    [YamlMember]
    public int Height {
        get => _height;
        set => SetField(ref _height, value);
    }

    public string ImagePath {
        get => _imagePath;
        set {
            if ( !SetField(ref _imagePath, value) ) return;
            LoadBitmap();
        }
    }

    [YamlIgnore]
    public Bitmap? ImageBitmap {
        get => _bitmap;
        private set => SetField(ref _bitmap, value);
    }

    private void LoadBitmap() {
        ImageBitmap?.Dispose();
        ImageBitmap = string.IsNullOrWhiteSpace(_imagePath) ? null : new Bitmap(_imagePath);
    }

    public override void Dispose() {
        base.Dispose();
        ImageBitmap?.Dispose();
    }

    public override PanelItem Clone() {
        var clone = new PanelItemImage {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Width = Width,
            Height = Height,
            ImagePath = ImagePath,
        };

        return clone;
    }
}

#endregion

public enum SensorPanelItemType {
    Label,
    SensorValue,
    SensorChart,
    Image,
}
