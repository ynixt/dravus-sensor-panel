using System;
using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;

namespace DravusSensorPanel.Models;

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

public interface IPanelItemTextAlignment {
    public TextAlignment TextAlignment { get; set; }
}

public interface IPanelItemText {
    // TODO: FontStyle (normal, italic, oblic)
    // TODO: FontWeight
    // TODO: Border
    string Label { get; }
    int FontSize { get; set; }
    FontFamily FontFamily { get; set; }
    Color Foreground { get; set; }

    IBrush ForegroundBrush { get; }
}

public interface IPanelItemEditableText : IPanelItemText {
    new string Label { get; set; }
}

public abstract class PanelItem : SuperReactiveObject, IDisposable {
    private string _description = string.Empty;
    private int _zIndex;
    private int _x;
    private int _y;
    private int _sort;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int ZIndex {
        get => _zIndex;
        set => SetField(ref _zIndex, value);
    }

    public int X {
        get => _x;
        set {
            if ( !SetField(ref _x, value) ) return;
            XChanged(_x);
        }
    }

    public int Y {
        get => _y;
        set {
            if ( !SetField(ref _y, value) ) return;
            YChanged(_y);
        }
    }


    public int Sort {
        get => _sort;
        set {
            if ( !SetField(ref _sort, value) ) return;
            YChanged(_sort);
        }
    }

    public abstract SensorPanelItemType Type { get; }

    public string Description {
        get => _description;
        set => SetField(ref _description, value);
    }

    public virtual void Reload() {
    }

    public virtual void Dispose() {
    }

    public abstract PanelItem Clone();

    public abstract PanelItemDto ToDto();

    protected virtual void XChanged(int newX) {
    }

    protected virtual void YChanged(int newY) {
    }
}
