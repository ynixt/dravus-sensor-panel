using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public sealed class PanelItemLabel : PanelItem, IPanelItemEditableText {
    private string _label = string.Empty;
    private Color _foreground = Colors.White;
    private SolidColorBrush? _cachedBrush;
    private int _fontSize = 14;
    private FontFamily _fontFamily = FontFamily.Default;

    public override SensorPanelItemType Type => SensorPanelItemType.Label;

    public IBrush ForegroundBrush => _cachedBrush ??= new SolidColorBrush(_foreground);

    public int FontSize {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    public FontFamily FontFamily {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

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

    public override PanelItemLabelDto ToDto() {
        return new PanelItemLabelDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Type = Type,

            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Label = Label,
        };
    }
}
