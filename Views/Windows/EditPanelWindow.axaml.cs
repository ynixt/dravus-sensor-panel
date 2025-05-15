using System;
using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractors;

namespace DravusSensorPanel.Views.Windows;

public partial class EditPanelWindow : WindowViewModel {
    public ObservableCollection<PanelItem> PanelItems { get; }

    private PanelItem? _selectedItem;
    private readonly Func<PanelItem?, PanelItemFormWindow>? _panelItemFormWindowFactory;
    private readonly Func<PanelSettingsWindow>? _panelSettingsWindowFactory;
    private readonly Func<SplashScreenWindow>? _splashScreenWindowFactory;
    private readonly SensorPanelService? _sensorPanelService;
    private readonly SensorPanelImportService? _sensorPanelImportService;

    public MainWindow? MainWindow { get; set; }

    public PanelItem? SelectedItem {
        get => _selectedItem;
        set {
            if ( !SetField(ref _selectedItem, value) ) return;

            MainWindow?.UnselectControls();
            if ( SelectedItem != null ) {
                MainWindow?.SelectItem(SelectedItem);
            }
        }
    }

    // Empty constructor to preview works on IDE
    public EditPanelWindow() : this(null, null, null, null, null) {
    }

    public EditPanelWindow(
        Func<PanelItem?, PanelItemFormWindow>? panelItemFormWindowFactory,
        SensorPanelService? sensorPanelService,
        Func<PanelSettingsWindow>? panelSettingsWindowFactory,
        SensorPanelImportService? sensorPanelImportService,
        Func<SplashScreenWindow>? splashScreenWindowFactory) {
        DataContext = this;
        _panelItemFormWindowFactory = panelItemFormWindowFactory;
        _sensorPanelService = sensorPanelService;
        _panelSettingsWindowFactory = panelSettingsWindowFactory;
        _sensorPanelImportService = sensorPanelImportService;
        _splashScreenWindowFactory = splashScreenWindowFactory;

        PanelItems = sensorPanelService?.SensorPanel.Items ?? [];
        Closed += OnWindowClosed;

        InitializeComponent();
    }

    public async void NewItemClick(object sender, RoutedEventArgs args) {
        if ( _panelItemFormWindowFactory != null ) {
            PanelItemFormWindow window = _panelItemFormWindowFactory(null);

            var item = await window.ShowDialog<PanelItem?>(this);

            if ( item != null ) {
                _sensorPanelService?.AddNewItem(item);
            }
        }
    }

    public async void ModifyItemClick(object sender, RoutedEventArgs args) {
        if ( _panelItemFormWindowFactory != null && SelectedItem != null ) {
            PanelItem originalItem = SelectedItem;
            PanelItem clone = SelectedItem.Clone();
            PanelItemFormWindow window = _panelItemFormWindowFactory(originalItem);

            var item = await window.ShowDialog<PanelItem?>(this);

            if ( item != null ) {
                _sensorPanelService?.EditItem(item, clone);
            }
            else {
                _sensorPanelService?.RemoveItem(originalItem, false, false);
                _sensorPanelService?.AddNewItem(clone, false);
                _sensorPanelService?.SortItems();
            }
        }
    }

    public void CloneItemClick(object sender, RoutedEventArgs args) {
        if ( _panelItemFormWindowFactory != null && SelectedItem != null ) {
            PanelItem clone = SelectedItem.ToDto().ToModel();
            clone.Id = Guid.NewGuid().ToString("N");
            _sensorPanelService?.AddNewItem(clone);
        }
    }

    public void DeleteItemClick(object sender, RoutedEventArgs args) {
        if ( SelectedItem != null ) {
            _sensorPanelService?.RemoveItem(SelectedItem);
        }
    }

    public async void PanelSettingsClick(object sender, RoutedEventArgs args) {
        if ( _panelSettingsWindowFactory != null ) {
            PanelSettingsWindow window = _panelSettingsWindowFactory();

            SensorPanel originalPanel = _sensorPanelService!.SensorPanel.Clone();
            var modifiedPanel = await window.ShowDialog<SensorPanel?>(this);

            if ( modifiedPanel == null ) {
                _sensorPanelService!.SensorPanel.CopyFrom(originalPanel);
            }
            else {
                _sensorPanelService!.SavePanel();
            }
        }
    }

    private async void ImportClick(object? sender, RoutedEventArgs e) {
        if ( !await _sensorPanelImportService!.ImportUsingDialog(this) ) return;

        MainWindow?.CloseWithoutKillApp();

        SplashScreenWindow? splashScreen = _splashScreenWindowFactory?.Invoke();
        splashScreen?.Show();

        Close();
    }

    private void ExportClick(object? sender, RoutedEventArgs e) {
        _sensorPanelImportService?.ExportUsingDialog(this);
    }

    private void OnWindowClosed(object? sender, EventArgs e) {
        MainWindow?.UnselectControls();
    }
}
