using System.Collections.ObjectModel;

namespace DravusSensorPanel.Models;

public class SensorPanel {
    public ObservableCollection<PanelItem> Items { get; } = new();
}
