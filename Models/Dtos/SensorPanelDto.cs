using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using DynamicData;

namespace DravusSensorPanel.Models.Dtos;

public class SensorPanelDto {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool HideBar { get; set; }
    public bool Maximized { get; set; }
    public int DisplayIndex { get; set; }
    public Color Background { get; set; }

    public List<PanelItemDto> Items { get; set; }

    public SensorPanel ToModel() {
        Window window = ( Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime )!
            .MainWindow!;

        Screen display;
        if ( DisplayIndex > window.Screens.All.Count ) {
            display = window.Screens.Primary ?? window.Screens.All[0];
        }
        else {
            display = window.Screens.All[DisplayIndex];
        }

        var sensorPanel = new SensorPanel {
            X = X, Y = Y, Width = Width, Height = Height, HideBar = HideBar, Maximized = Maximized, Display = display,
            Background = Background
        };

        sensorPanel.Items.AddRange(Items.Select(item => item.ToModel()));

        return sensorPanel;
    }
}
