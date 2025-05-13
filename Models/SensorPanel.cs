using System.Collections.ObjectModel;
using System.Linq;
using DravusSensorPanel.Models.Dtos;
using DynamicData;

namespace DravusSensorPanel.Models;

public class SensorPanel : SuperReactiveObject {
    private int _x;
    private int _y;
    private int _width = 400;
    private int _height = 400;

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

    public SensorPanelDto ToDto() {
        return new SensorPanelDto
            { Items = Items.Select(item => item.ToDto()).ToList(), X = X, Y = Y, Width = Width, Height = Height };
    }

    public SensorPanel Clone() {
        return new SensorPanel { Items = Items, X = X, Y = Y, Width = Width, Height = Height };
    }

    public void CopyFrom(SensorPanel sensorPanel, bool includeItems = false) {
        X = sensorPanel.X;
        Y = sensorPanel.Y;
        Width = sensorPanel.Width;
        Height = sensorPanel.Height;

        if ( includeItems ) {
            Items.Clear();
            Items.AddRange(sensorPanel.Items);
        }
    }
}
