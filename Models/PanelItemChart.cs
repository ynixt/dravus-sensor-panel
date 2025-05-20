using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SkiaSharp;

namespace DravusSensorPanel.Models;

public enum ChartType {
    Lines,
    HorizontalBar,
    HorizontalBars,
}

public sealed class PanelItemChart : PanelItemNumberSensor, IPanelItemSizeable {
    private int _width = 300;
    private int _height = 150;
    private Color _background = Colors.Transparent;
    private SolidColorBrush? _cachedBackgroundBrush;
    private double? _yMinValue;
    private double? _yMaxValue;
    private double? _xMinValue;
    private double? _xMaxValue;
    private bool _showYAxis;
    private bool _showXAxis;

    public override SensorPanelItemType Type => SensorPanelItemType.SensorChart;

    public double? YMinValue {
        get => _yMinValue;
        set => SetField(ref _yMinValue, value);
    }

    public double? YMaxValue {
        get => _yMaxValue;
        set => SetField(ref _yMaxValue, value);
    }

    public double? XMinValue {
        get => _xMinValue;
        set => SetField(ref _xMinValue, value);
    }

    public double? XMaxValue {
        get => _xMaxValue;
        set => SetField(ref _xMaxValue, value);
    }

    public Color Stroke { get; set; } = Colors.Lime;
    public Color Fill { get; set; } = Colors.Transparent;
    public double LineSmoothness { get; set; } = 0.6;
    public double MinStep { get; set; } = 1;

    public bool ShowYAxis {
        get => _showYAxis;
        set => SetField(ref _showYAxis, value);
    }

    public bool ShowXAxis {
        get => _showXAxis;
        set => SetField(ref _showXAxis, value);
    }

    public ChartType ChartType { get; set; } = ChartType.Lines;
    public IBrush BackgroundBrush => _cachedBackgroundBrush ??= new SolidColorBrush(_background);

    public ISeries[] Series { get; private set; } = [];

    public ICartesianAxis[] YAxes { get; private set; } = [
        new Axis {
            IsVisible = false,
            ShowSeparatorLines = false,
        },
    ];

    public ICartesianAxis[] XAxes { get; private set; } = [
        new Axis {
            IsVisible = false,
            ShowSeparatorLines = false,
        },
    ];

    public Color Background {
        get => _background;
        set {
            if ( !SetField(ref _background, value) ) return;
            _cachedBackgroundBrush = new SolidColorBrush(value);
            this.RaisePropertyChanged(nameof(BackgroundBrush));
        }
    }

    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int Height {
        get => _height;
        set => SetField(ref _height, value);
    }

    public NumberSensor? NumberSensor {
        get => Sensor as NumberSensor;
        set => Sensor = value;
    }

    public override PanelItem Clone() {
        var clone = new PanelItemChart {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Sort = Sort,

            Sensor = Sensor,
            Unit = Unit,
            NumDecimalPlaces = NumDecimalPlaces,
            ValueType = ValueType,

            Width = Width,
            Height = Height,
            YMinValue = YMinValue,
            YMaxValue = YMaxValue,
            XMinValue = XMinValue,
            XMaxValue = XMaxValue,
            Background = Background,
            Stroke = Stroke,
            Fill = Fill,
            LineSmoothness = LineSmoothness,
            MinStep = MinStep,
            ShowYAxis = ShowYAxis,
            ShowXAxis = ShowXAxis,
            ChartType = ChartType,
        };

        clone.UpdateChart();

        return clone;
    }

    public override PanelItemChartDto ToDto() {
        return new PanelItemChartDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Type = Type,
            Sort = Sort,

            Sensor = Sensor == null ? null : new SensorDto { Source = Sensor.Source, SourceId = Sensor.SourceId },
            Unit = Unit?.ToDto(),
            NumDecimalPlaces = NumDecimalPlaces,
            ValueType = ValueType,

            Width = Width,
            Height = Height,
            YMinValue = YMinValue,
            YMaxValue = YMaxValue,
            XMinValue = XMinValue,
            XMaxValue = XMaxValue,
            Background = Background,
            Stroke = Stroke,
            Fill = Fill,
            LineSmoothness = LineSmoothness,
            MinStep = MinStep,
            ShowYAxis = ShowYAxis,
            ShowXAxis = ShowXAxis,
            ChartType = ChartType,
        };
    }

    protected override void UnitChanged(Unit? unit) {
        base.UnitChanged(unit);
        UpdateChart();
    }

    protected override void SensorValueChanged(float? value) {
        base.SensorValueChanged(value);
        UpdateChart();
    }

    private void UpdateChart() {
        BuildSeries();
        BuildAxes();
    }

    private void BuildSeries() {
        const int limit = 100;
        if ( Sensor == null || NumberSensor == null ) {
            Series = [];
            return;
        }

        IEnumerable<DateValue> source = ValueType switch {
            PanelItemSensorValueType.Value => NumberSensor.Values,
            PanelItemSensorValueType.Min => NumberSensor.Mins,
            PanelItemSensorValueType.Max => NumberSensor!.Maxs,
            _ => Enumerable.Empty<DateValue>(),
        };

        bool convert = Unit != null;
        UnitService? unitSvc = convert
            ? App.ServiceProvider!
                 .GetRequiredService<UnitService>()
            : null;
        Unit sensorUnit = NumberSensor.Unit;
        Unit? targetUnit = Unit;

        var points = new List<DateTimePoint>(limit);
        DateTime? last = null;

        foreach ( DateValue dv in source ) {
            if ( dv.Value == null ) continue;
            if ( last != null && ( dv.DateTime - last.Value ).TotalSeconds < MinStep ) continue;
            last = dv.DateTime;

            double val = dv.Value.Value;
            if ( convert && unitSvc != null ) {
                val = unitSvc.Convert(val, sensorUnit, targetUnit!);
            }

            val = Math.Round(val, NumDecimalPlaces, MidpointRounding.ToEven);

            points.Add(new DateTimePoint(dv.DateTime, val));
            if ( points.Count > limit ) points.RemoveAt(0);
        }

        if ( ChartType is ChartType.HorizontalBar or ChartType.HorizontalBars ) {
            if ( ChartType is ChartType.HorizontalBar ) {
                Series = [
                    new RowSeries<double> {
                        Stroke =
                            Stroke.A == 0
                                ? null
                                : new SolidColorPaint(new SKColor(Stroke.R, Stroke.G, Stroke.B, Stroke.A)),
                        Fill = Fill.A == 0 ? null : new SolidColorPaint(new SKColor(Fill.R, Fill.G, Fill.B, Fill.A)),
                        Values = points.Count > 0 && points[^1].Value != null ? [points[^1].Value!.Value] : null,
                    },
                ];
            }
            else {
                Series = [
                    new RowSeries<DateTimePoint> {
                        Stroke =
                            Stroke.A == 0
                                ? null
                                : new SolidColorPaint(new SKColor(Stroke.R, Stroke.G, Stroke.B, Stroke.A)),
                        Fill = Fill.A == 0 ? null : new SolidColorPaint(new SKColor(Fill.R, Fill.G, Fill.B, Fill.A)),
                        Values = points,
                    },
                ];
            }
        }
        else {
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
        }

        this.RaisePropertyChanged(nameof(Series));
    }

    private void BuildAxes() {
        bool isHorizontal = ChartType is ChartType.HorizontalBar or ChartType.HorizontalBars;

        double? minXLimit;
        double? maxXLimit;
        double? minYLimit;
        double? maxYLimit;

        string DefaultLabeler(double x) {
            return x.ToString(CultureInfo.CurrentCulture);
        }

        Func<double, string> tickerLabeler = ticks => new DateTime(( long ) ticks).ToString("HH:mm:ss");
        Func<double, string> valueLabeler = v => $"{v.ToString(CultureInfo.CurrentCulture)} {UnitSymbol}";

        Func<double, string> labelerY;
        Func<double, string> labelerX;

        if ( ChartType is ChartType.HorizontalBar or ChartType.HorizontalBars ) {
            minXLimit = XMinValue;
            maxXLimit = XMaxValue;
            minYLimit = null;
            maxYLimit = null;
            labelerX = ChartType is ChartType.HorizontalBar ? x => "" : valueLabeler;
            labelerY = ChartType is ChartType.HorizontalBar ? valueLabeler : tickerLabeler;
        }
        else {
            minXLimit = null;
            maxXLimit = null;
            minYLimit = YMinValue;
            maxYLimit = YMaxValue;
            labelerX = tickerLabeler;
            labelerY = valueLabeler;
        }

        XAxes = [
            new Axis {
                MinLimit = minXLimit,
                MaxLimit = maxXLimit,
                Labeler = labelerX,
                IsVisible = ShowXAxis,
                ShowSeparatorLines = false,
            },
        ];
        this.RaisePropertyChanged(nameof(XAxes));

        YAxes = [
            new Axis {
                MinLimit = minYLimit,
                MaxLimit = maxYLimit,
                Labeler = labelerY,
                ShowSeparatorLines = false,
                IsVisible = ShowYAxis,
            },
        ];
        this.RaisePropertyChanged(nameof(YAxes));
    }
}
