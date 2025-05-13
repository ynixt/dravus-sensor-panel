using System;
using Avalonia.Interactivity;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;

namespace DravusSensorPanel.Views.Windows;

public partial class PanelSettingsWindow : WindowViewModel {
    public SensorPanel SensorPanel { get; set; }

    // Empty constructor to preview works on IDE
    public PanelSettingsWindow() : this(null, null) {
    }

    public PanelSettingsWindow(
        Func<PanelItem?, PanelItemFormWindow>? panelItemFormWindowFactory,
        SensorPanelService? sensorPanelService) {
        DataContext = this;

        if ( sensorPanelService != null ) {
            SensorPanel = sensorPanelService.SensorPanel;
        }

        InitializeComponent();
    }

    public void OkClick(object sender, RoutedEventArgs args) {
        Close(SensorPanel);
    }

    public void CancelClick(object sender, RoutedEventArgs args) {
        Close(null);
    }
}
