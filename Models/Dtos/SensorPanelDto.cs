using System.Collections.Generic;
using System.Linq;
using DynamicData;

namespace DravusSensorPanel.Models.Dtos;

public class SensorPanelDto {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PanelItemDto> Items { get; set; }

    public SensorPanel ToModel() {
        var sensorPanel = new SensorPanel { X = X, Y = Y, Width = Width, Height = Height };

        sensorPanel.Items.AddRange(Items.Select(item => item.ToModel()));

        return sensorPanel;
    }
}
