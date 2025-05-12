using System;
using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DynamicData;

namespace DravusSensorPanel.Views.Windows;

public partial class EditPanelWindow : WindowViewModel {
    public ObservableCollection<PanelItem> PanelItems { get; } = new();

    private PanelItem? _selectedItem;
    private readonly Func<PanelItem?, PanelItemFormWindow>? _panelItemFormWindowFactory;
    private readonly SensorPanelService? _sensorPanelService;

    public PanelItem? SelectedItem {
        get => _selectedItem;
        set => RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public EditPanelWindow() : this(null, null) {
    }

    public EditPanelWindow(
        Func<PanelItem?, PanelItemFormWindow>? panelItemFormWindowFactory,
        SensorPanelService? sensorPanelService) {
        DataContext = this;
        _panelItemFormWindowFactory = panelItemFormWindowFactory;
        _sensorPanelService = sensorPanelService;

        InitializeComponent();

        if ( sensorPanelService?.SensorPanel.Items != null ) {
            PanelItems.AddRange(sensorPanelService.SensorPanel.Items);
        }
    }

    public void NewItemClick(object sender, RoutedEventArgs args) {
        if ( _panelItemFormWindowFactory != null ) {
            PanelItemFormWindow window = _panelItemFormWindowFactory(null);

            window.ShowDialog<PanelItem?>(this).ContinueWith(task => {
                PanelItem? item = task.Result;
                if ( item != null ) {
                    AddNewItem(item);
                }
            });
        }
    }

    public void ModifyItemClick(object sender, RoutedEventArgs args) {
        if ( _panelItemFormWindowFactory != null && SelectedItem != null ) {
            PanelItem originalItem = SelectedItem;
            PanelItem clone = SelectedItem.Clone();
            PanelItemFormWindow window = _panelItemFormWindowFactory(originalItem);

            window.ShowDialog<PanelItem?>(this).ContinueWith(task => {
                PanelItem? item = task.Result;
                if ( item != null ) {
                    if ( item is PanelItemSensor itemSensor ) {
                        itemSensor.WatchSensorValueChange();
                    }
                }
                else {
                    RemoveItem(originalItem);
                    AddNewItem(clone);
                }
            });
        }
    }

    public void DeleteItemClick(object sender, RoutedEventArgs args) {
        if ( SelectedItem != null ) {
            RemoveItem(SelectedItem);
        }
    }

    private void AddNewItem(PanelItem item) {
        Dispatcher.UIThread.Post(() => {
            PanelItems.Add(item);
            _sensorPanelService?.SensorPanel.Items.Add(item);

            if ( item is PanelItemSensor itemSensor ) {
                itemSensor.WatchSensorValueChange();
            }
        });
    }

    private void RemoveItem(PanelItem item) {
        Dispatcher.UIThread.Post(() => {
            _sensorPanelService?.SensorPanel.Items.Remove(item);
            PanelItems.Remove(item);
        });
    }
}
