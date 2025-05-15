using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DravusSensorPanel.Models.Dtos;

public abstract class PanelItemDto {
    public string Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int ZIndex { get; set; }
    public int Sort { get; set; }
    public SensorPanelItemType Type { get; set; }
    public string Description { get; set; }

    protected void CopyBaseToTarget(PanelItem target) {
        target.Id = Id;
        target.X = X;
        target.Y = Y;
        target.ZIndex = ZIndex;
        target.Description = Description;
        target.Sort = Sort;
    }

    public abstract PanelItem ToModel();
}

public abstract class PanelItemSensorDto : PanelItemDto {
    public SensorDto? Sensor { get; set; }
    public UnitDto? Unit { get; set; }

    protected void CopySensorBase(PanelItemSensor target) {
        CopyBaseToTarget(target);

        target.Sensor = Sensor == null
            ? null
            : App.ServiceProvider!
                 .GetRequiredService<SensorRepository>()
                 .FindSensor(Sensor.Source, Sensor.SourceId);

        target.Unit = target.Sensor == null || Unit == null
            ? null
            : App.ServiceProvider!.GetRequiredService<UnitRepository>().GetUnitById(Unit.Id);
    }
}

public abstract class PanelItemNumberSensorDto : PanelItemSensorDto {
    public int NumDecimalPlaces { get; set; }
    public PanelItemSensorValueType ValueType { get; set; }

    protected void CopySensorBase(PanelItemNumberSensor target) {
        base.CopySensorBase(target);
        CopyBaseToTarget(target);

        target.NumDecimalPlaces = NumDecimalPlaces;
        target.ValueType = ValueType;
        target.Sensor = Sensor == null
            ? null
            : App.ServiceProvider!
                 .GetRequiredService<SensorRepository>()
                 .FindSensor(Sensor.Source, Sensor.SourceId);

        target.Unit = target.Sensor == null || Unit == null
            ? null
            : App.ServiceProvider!.GetRequiredService<UnitRepository>().GetUnitById(Unit.Id);
    }
}

public class PanelItemLabelDto : PanelItemDto {
    public int FontSize { get; set; }
    public FontFamily FontFamily { get; set; }
    public Color Foreground { get; set; }
    public string Label { get; set; }
    public TextAlignment TextAlignment { get; set; }

    public override PanelItemLabel ToModel() {
        var model = new PanelItemLabel {
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Label = Label,
            TextAlignment = TextAlignment,
        };
        CopyBaseToTarget(model);
        return model;
    }
}

public class PanelItemValueDto : PanelItemNumberSensorDto {
    public int Width { get; set; }
    public int FontSize { get; set; }
    public FontFamily FontFamily { get; set; } = FontFamily.Default;
    public Color Foreground { get; set; }
    public Color UnitForeground { get; set; }
    public bool ShowUnit { get; set; }

    public override PanelItemValue ToModel() {
        var model = new PanelItemValue {
            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            UnitForeground = UnitForeground,
            ShowUnit = ShowUnit,
        };
        CopySensorBase(model);
        model.Reload();
        return model;
    }
}

public class PanelItemChartDto : PanelItemNumberSensorDto {
    public int Width { get; set; }
    public int Height { get; set; }
    public double? YMinValue { get; set; }
    public double? YMaxValue { get; set; }
    public Color Stroke { get; set; }
    public Color Fill { get; set; }
    public double LineSmoothness { get; set; }
    public double MinStep { get; set; }
    public bool ShowYAxis { get; set; }
    public bool ShowXAxis { get; set; }

    public override PanelItemChart ToModel() {
        var model = new PanelItemChart {
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
        CopySensorBase(model);
        model.Reload();
        return model;
    }
}

public class PanelItemObjectDto : PanelItemSensorDto {
    public int Width { get; set; }
    public int FontSize { get; set; }
    public FontFamily FontFamily { get; set; } = FontFamily.Default;
    public Color Foreground { get; set; }
    public string? Format { get; set; }
    public TextAlignment TextAlignment { get; set; }

    public override PanelItemObjectSensor ToModel() {
        var model = new PanelItemObjectSensor {
            Width = Width,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Format = Format,
            TextAlignment = TextAlignment,
        };
        CopySensorBase(model);
        model.Reload();
        return model;
    }
}

public class PanelItemImageDto : PanelItemDto {
    public int Width { get; set; }
    public int Height { get; set; }
    public string ImagePath { get; set; }

    public override PanelItemImage ToModel() {
        var model = new PanelItemImage {
            Width = Width,
            Height = Height,
            ImagePath = ImagePath,
        };
        CopyBaseToTarget(model);
        model.Reload();
        return model;
    }
}
