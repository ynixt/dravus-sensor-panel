using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
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

public sealed class PanelItemChart : PanelItemSensor, IPanelItemSizeable {
    private int _width = 300;
    private int _height = 150;

    public override SensorPanelItemType Type => SensorPanelItemType.SensorChart;

    public double? YMinValue { get; set; }
    public double? YMaxValue { get; set; }
    public Color Stroke { get; set; } = Colors.Lime;
    public Color Fill { get; set; } = Colors.Transparent;
    public double LineSmoothness { get; set; } = 0.6;
    public double MinStep { get; set; } = 1;
    public bool ShowYAxis { get; set; }
    public bool ShowXAxis { get; set; }

    public ISeries[] Series { get; private set; } = [];

    public ICartesianAxis[] YAxes { get; private set; } = [];

    public ICartesianAxis[] XAxes { get; private set; } = [];

    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int Height {
        get => _height;
        set => SetField(ref _height, value);
    }

    public override PanelItem Clone() {
        var clone = new PanelItemChart {
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

    public override PanelItemChartDto ToDto() {
        return new PanelItemChartDto {
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
    }

    protected override void UnitChanged(Enum? unit) {
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
}
