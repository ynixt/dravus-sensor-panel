using Avalonia.Media;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public sealed class PanelItemLabel : PanelItem, IPanelItemEditableText, IPanelItemTextAlignment {
    private string _rawLabel = string.Empty;
    private Color _foreground = Colors.White;
    private SolidColorBrush? _cachedBrush;
    private int _fontSize = 14;
    private FontFamily _fontFamily = FontFamily.Default;
    private TextAlignment _textAlignment = TextAlignment.Center;
    private string _caseStyle = PanelItemTextTransform.NormalCaseStyle;
    private string? _replaceRegexPattern;
    private string? _replaceRegexReplacement;

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

    public string RawLabel {
        get => _rawLabel;
        set {
            if ( !SetField(ref _rawLabel, value) ) return;
            RefreshLabel();
        }
    }

    public string Label {
        get => PanelItemTextTransform.Apply(_rawLabel, CaseStyle, ReplaceRegexPattern, ReplaceRegexReplacement);
        set => RawLabel = value;
    }

    public string CaseStyle {
        get => _caseStyle;
        set {
            if ( value == null && _caseStyle != null ) return;

            string normalized = PanelItemTextTransform.NormalizeCaseStyle(value);
            if ( !SetField(ref _caseStyle, normalized) ) return;
            RefreshLabel();
        }
    }

    public string? ReplaceRegexPattern {
        get => _replaceRegexPattern;
        set {
            if ( !SetField(ref _replaceRegexPattern, value) ) return;
            RefreshLabel();
        }
    }

    public string? ReplaceRegexReplacement {
        get => _replaceRegexReplacement;
        set {
            if ( !SetField(ref _replaceRegexReplacement, value) ) return;
            RefreshLabel();
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
            Transparency = Transparency,
            Description = Description,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            RawLabel = RawLabel,
            TextAlignment = TextAlignment,
            CaseStyle = CaseStyle,
            ReplaceRegexPattern = ReplaceRegexPattern,
            ReplaceRegexReplacement = ReplaceRegexReplacement,
            Sort = Sort,
        };
    }

    public override PanelItemLabelDto ToDto() {
        return new PanelItemLabelDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Transparency = Transparency,
            Description = Description,
            Type = Type,
            Sort = Sort,
            FontSize = FontSize,
            FontFamily = FontFamily,
            Foreground = Foreground,
            Label = RawLabel,
            TextAlignment = TextAlignment,
            CaseStyle = CaseStyle,
            ReplaceRegexPattern = ReplaceRegexPattern,
            ReplaceRegexReplacement = ReplaceRegexReplacement,
        };
    }

    private void RefreshLabel() {
        this.RaisePropertyChanged(nameof(Label));
    }
}
