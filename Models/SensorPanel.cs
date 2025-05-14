using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using DravusSensorPanel.Models.Dtos;
using DynamicData;
using ReactiveUI;

namespace DravusSensorPanel.Models;

public class SensorPanel : SuperReactiveObject {
    private int _x;
    private int _y;
    private int _width = 400;
    private int _height = 400;
    private bool _hideBar;
    private bool _maximized;
    private int _displayIndex;
    private Screen _display;
    private Color _background = Colors.Black;
    private SolidColorBrush? _cachedBackgroundBrush;

    public ObservableCollection<PanelItem> Items { get; private init; } = new();

    public int X {
        get => _x;
        set => SetField(ref _x, value);
    }

    public int Y {
        get => _y;
        set => SetField(ref _y, value);
    }

    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int Height {
        get => _height;
        set => SetField(ref _height, value);
    }

    public bool HideBar {
        get => _hideBar;
        set => SetField(ref _hideBar, value);
    }

    public bool Maximized {
        get => _maximized;
        set => SetField(ref _maximized, value);
    }

    public Screen Display {
        get => _display;
        set => SetField(ref _display, value);
    }

    public IBrush BackgroundBrush => _cachedBackgroundBrush ??= new SolidColorBrush(_background);

    public Color Background {
        get => _background;
        set {
            if ( !SetField(ref _background, value) ) return;
            _cachedBackgroundBrush = new SolidColorBrush(value);
            this.RaisePropertyChanged(nameof(BackgroundBrush));
        }
    }

    public SensorPanelDto ToDto() {
        Window window = ( Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime )!
            .MainWindow!;

        return new SensorPanelDto {
            Items = Items.Select(item => item.ToDto()).ToList(), X = X, Y = Y, Width = Width, Height = Height, HideBar = HideBar,
            Maximized = Maximized, DisplayIndex = window.Screens.All.IndexOf(Display), Background = Background
        };
    }

    public SensorPanel Clone() {
        return new SensorPanel {
            Items = Items, X = X, Y = Y, Width = Width, Height = Height, HideBar = HideBar, Maximized = Maximized,
            Display = Display, Background = Background
        };
    }

    public void CopyFrom(SensorPanel sensorPanel, bool includeItems = false) {
        Display = sensorPanel.Display;
        X = sensorPanel.X;
        Y = sensorPanel.Y;
        Width = sensorPanel.Width;
        Height = sensorPanel.Height;
        HideBar = sensorPanel.HideBar;
        Maximized = sensorPanel.Maximized;
        Background = sensorPanel.Background;

        if ( includeItems ) {
            Items.Clear();
            Items.AddRange(sensorPanel.Items);
        }
    }
}
