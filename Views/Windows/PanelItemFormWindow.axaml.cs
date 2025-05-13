using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services.InfoExtractor;
using DravusSensorPanel.Views.PanelItemsInfo;
using DynamicData;

namespace DravusSensorPanel.Views.Windows;

public partial class PanelItemFormWindow : WindowViewModel {
    private readonly IEnumerable<IInfoExtractor>? _infoExtractors;
    private readonly Grid _grid;

    private int? _itemTypeSelectedIndex;
    private Sensor? _selectedSensor;
    private PanelItemInfo? _panelControl;
    private bool _editMode;

    public PanelItem PanelItem { get; set; }
    public ObservableCollection<Sensor> Sensors { get; } = new();

    public bool EditMode {
        get => _editMode;
        set => RaiseAndSetIfChanged(ref _editMode, value);
    }

    public int? ItemTypeSelectedIndex {
        get => _itemTypeSelectedIndex;
        set {
            if ( _itemTypeSelectedIndex != value ) {
                _itemTypeSelectedIndex = value;
                SelectedItemChanged(_itemTypeSelectedIndex);
            }
        }
    }


    public Sensor? SelectedSensor {
        get => _selectedSensor;
        set {
            if ( _selectedSensor != value && value != null ) {
                _selectedSensor = value;
                SelectedSensorChanged(_selectedSensor);
            }
        }
    }

    // Empty constructor to preview works on IDE
    public PanelItemFormWindow() : this(null) {
    }

    public PanelItemFormWindow(
        IEnumerable<IInfoExtractor>? extractors,
        PanelItem? panelItem = null) {
        EditMode = panelItem != null;
        DataContext = this;

        InitializeComponent();

        _grid = this.FindControl<Grid>("Grid")!;

        Closed += OnWindowClosed;

        _infoExtractors = extractors;

        ItemTypeSelectedIndex = 0;

        if ( _infoExtractors != null ) {
            List<Sensor> sensorsList = _infoExtractors
                                       .SelectMany(e => e.Start())
                                       .Order()
                                       .ToList();

            Sensors.AddRange(sensorsList);

            if ( panelItem is not PanelItemSensor panelItemSensor ) {
                SelectedSensor = sensorsList[0];
            }
            else {
                SelectedSensor = panelItemSensor.Sensor;
            }
        }

        if ( EditMode ) {
            Dispatcher.UIThread.Post(() => { LoadToEdit(panelItem!); });
        }
    }

    public void LoadToEdit(PanelItem panelItem) {
        ItemTypeSelectedIndex = panelItem.Type switch {
            SensorPanelItemType.SensorValue => 0,
            SensorPanelItemType.SensorChart => 1,
            SensorPanelItemType.Label => 2,
            SensorPanelItemType.Image => 3,
        };

        PanelItem = panelItem;

        OnPropertyChanged(nameof(PanelItem));
    }

    private void OnWindowClosed(object? sender, EventArgs e) {
        _infoExtractors?.ToList().ForEach(i => i.Dispose());
    }

    public void OkClick(object sender, RoutedEventArgs args) {
        if ( _panelControl?.IsValid() == true ) {
            Close(PanelItem);
        }
    }

    public void CancelClick(object sender, RoutedEventArgs args) {
        Close(null);
    }

    private void SelectedItemChanged(int? item) {
        OnPropertyChanged(nameof(ItemTypeSelectedIndex));

        string id = Guid.NewGuid().ToString();

        Color defaultColor = Application.Current!.ActualThemeVariant == ThemeVariant.Dark
            ? Color.FromArgb(255, 255, 255, 255)
            : Color.FromArgb(255, 0, 0, 0);

        int defaultFontSize = 20;

        FontFamily defaultFont = FontManager.Current.DefaultFontFamily;

        RemovePanelControlFromGrid();

        PanelItem = item switch {
            0 => new PanelItemValue {
                Id = id, Sensor = SelectedSensor, FontSize = defaultFontSize, Width = 100, NumDecimalPlaces = 0,
                Foreground = defaultColor,
                UnitForeground = defaultColor, ShowUnit = true, FontFamily = defaultFont,
            },
            1 => new PanelItemChart {
                Id = id, Sensor = SelectedSensor, Width = 100, Height = 100, Stroke = defaultColor, ShowXAxis = false,
                ShowYAxis = true,
                MinStep = 2, LineSmoothness = 1,
                Fill = Color.FromArgb(0, 0, 0, 0),
            },
            2 => new PanelItemLabel
                { Id = id, FontSize = defaultFontSize, Foreground = defaultColor, FontFamily = defaultFont },
            3 => new PanelItemImage { Id = id, Width = 100, Height = 100 },
            _ => throw new NotImplementedException(),
        };

        if ( PanelItem is PanelItemValue ) {
            _panelControl = new PanelItemInfoSensorValue(EditMode);
            _panelControl.Bind(PanelItemInfoSensorValue.SensorsProperty, new Binding(nameof(Sensors)));
            _panelControl.Bind(PanelItemInfoSensorValue.SelectedSensorProperty,
                new Binding(nameof(SelectedSensor), BindingMode.TwoWay));
            _panelControl.Bind(PanelItemInfoSensorValue.PanelItemProperty, new Binding(nameof(PanelItem)));
        }
        else if ( PanelItem is PanelItemLabel ) {
            _panelControl = new PanelItemInfoLabel(EditMode);
            _panelControl.Bind(PanelItemInfoLabel.PanelItemProperty, new Binding(nameof(PanelItem)));
        }
        else if ( PanelItem is PanelItemImage ) {
            _panelControl = new PanelItemInfoImage(EditMode);
            _panelControl.Bind(PanelItemInfoImage.PanelItemProperty, new Binding(nameof(PanelItem)));
        }
        else if ( PanelItem is PanelItemChart ) {
            _panelControl = new PanelItemInfoSensorChart(EditMode);
            _panelControl.Bind(PanelItemInfoSensorChart.SensorsProperty, new Binding(nameof(Sensors)));
            _panelControl.Bind(PanelItemInfoSensorChart.SelectedSensorProperty,
                new Binding(nameof(SelectedSensor), BindingMode.TwoWay));
            _panelControl.Bind(PanelItemInfoSensorChart.PanelItemProperty, new Binding(nameof(PanelItem)));
        }

        if ( _panelControl != null ) {
            Grid.SetRow(_panelControl, 1);
            _grid.Children.Add(_panelControl);

            PanelItem.Description = MountNameForItem(PanelItem);
            OnPropertyChanged(nameof(PanelItem));
        }
    }

    private void SelectedSensorChanged(Sensor? sensor) {
        if ( sensor != null ) {
            if ( PanelItem is PanelItemSensor sensorPanelItem ) {
                sensorPanelItem.Sensor = sensor;
                PanelItem.Description = MountNameForItem(PanelItem);
                sensorPanelItem.Unit = sensor.Unit;
            }
        }

        OnPropertyChanged(nameof(SelectedSensor));
        OnPropertyChanged(nameof(PanelItem));
    }

    private void RemovePanelControlFromGrid() {
        if ( _panelControl != null ) {
            _grid.Children.Remove(_panelControl);
        }

        _panelControl = null;
    }

    private string MountNameForItem(PanelItem? item) {
        return item?.Type switch {
            SensorPanelItemType.SensorValue => MountNameForItem(item as PanelItemSensor, "value"),
            SensorPanelItemType.SensorChart => MountNameForItem(item as PanelItemSensor, "chart"),
            SensorPanelItemType.Label => "label",
            SensorPanelItemType.Image => "image",
            _ => "",
        };
    }

    private string MountNameForItem(PanelItemSensor? item, string suffix) {
        Sensor? sensor = item?.Sensor;

        if ( sensor != null ) {
            return sensor.Hardware + " - " + sensor.Name + " - " + suffix;
        }

        return "";
    }
}
