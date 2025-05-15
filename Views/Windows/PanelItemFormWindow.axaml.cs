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
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Services.InfoExtractors;
using DravusSensorPanel.Views.PanelItemsInfo;
using DynamicData;

namespace DravusSensorPanel.Views.Windows;

public partial class PanelItemFormWindow : WindowViewModel {
    private readonly IEnumerable<InfoExtractor>? _infoExtractors;
    private readonly Grid _grid;

    private int? _itemTypeSelectedIndex;
    private Sensor? _selectedSensor;
    private PanelItemInfo? _panelControl;
    private bool _editMode;

    public PanelItem PanelItem { get; set; }
    public ObservableCollection<NumberSensor> NumberSensors { get; } = new();
    public ObservableCollection<ObjectSensor> ObjectSensors { get; } = new();

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

    public NumberSensor? SelectedNumberSensor {
        get => SelectedSensor as NumberSensor;
        set => SelectedSensor = value;
    }

    public ObjectSensor? SelectedObjectSensor {
        get => SelectedSensor as ObjectSensor;
        set => SelectedSensor = value;
    }

    // Empty constructor to preview works on IDE
    public PanelItemFormWindow() : this(null) {
    }

    public PanelItemFormWindow(
        IEnumerable<InfoExtractor>? extractors,
        PanelItem? panelItem = null) {
        EditMode = panelItem != null;
        DataContext = this;

        InitializeComponent();

        _grid = this.FindControl<Grid>("Grid")!;

        Closed += OnWindowClosed;

        _infoExtractors = extractors;

        if ( !EditMode ) {
            ItemTypeSelectedIndex = 0;
        }

        if ( _infoExtractors != null ) {
            List<Sensor> sensorsList = _infoExtractors
                                       .SelectMany(e => e.Start())
                                       .Order()
                                       .ToList();

            NumberSensors.AddRange(sensorsList.Where(s => s is NumberSensor).Cast<NumberSensor>());
            ObjectSensors.AddRange(sensorsList.Where(s => s is ObjectSensor).Cast<ObjectSensor>());

            if ( panelItem is PanelItemSensor panelItemSensor ) {
                SelectedSensor = panelItemSensor.Sensor;
            }
        }

        if ( EditMode ) {
            LoadToEdit(panelItem!);
        }

        InfoExtractor.ByPassNoUse = true;
    }

    public void LoadToEdit(PanelItem panelItem) {
        ItemTypeSelectedIndex = panelItem.Type switch {
            SensorPanelItemType.SensorValue => 0,
            SensorPanelItemType.SensorObject => 1,
            SensorPanelItemType.SensorChart => 2,
            SensorPanelItemType.Label => 3,
            SensorPanelItemType.Image => 4,
        };

        PanelItem = panelItem;

        OnPropertyChanged(nameof(PanelItem));
    }

    private void OnWindowClosed(object? sender, EventArgs e) {
        _infoExtractors?.ToList().ForEach(i => i.Dispose());
        InfoExtractor.ByPassNoUse = false;
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

        PanelItem? oldPanelItem = PanelItem;

        PanelItem = item switch {
            0 => new PanelItemValue {
                Id = id, Sensor = SelectedSensor, FontSize = defaultFontSize, Width = 100, NumDecimalPlaces = 0,
                Foreground = defaultColor,
                UnitForeground = defaultColor, ShowUnit = true, FontFamily = defaultFont,
            },
            1 => new PanelItemObjectSensor {
                Id = id, Sensor = SelectedObjectSensor, FontSize = defaultFontSize, Width = 100,
                Foreground = defaultColor, FontFamily = defaultFont,
            },
            2 => new PanelItemChart {
                Id = id, Sensor = SelectedNumberSensor, Width = 100, Height = 100, Stroke = defaultColor,
                ShowXAxis = false,
                ShowYAxis = true,
                MinStep = 2, LineSmoothness = 1,
                Fill = Color.FromArgb(0, 0, 0, 0),
            },
            3 => new PanelItemLabel
                { Id = id, FontSize = defaultFontSize, Foreground = defaultColor, FontFamily = defaultFont },
            4 => new PanelItemImage { Id = id, Width = 100, Height = 100 },
            _ => throw new NotImplementedException(),
        };

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if ( oldPanelItem != null ) {
            PanelItem.X = oldPanelItem.X;
            PanelItem.Y = oldPanelItem.Y;
            PanelItem.ZIndex = oldPanelItem.ZIndex;


            if ( PanelItem is IPanelItemHorizontalSizeable panelItemHorizontalSizeable &&
                 oldPanelItem is IPanelItemHorizontalSizeable oldPanelItemHorizontalSizeable ) {
                panelItemHorizontalSizeable.Width = oldPanelItemHorizontalSizeable.Width;
            }

            if ( PanelItem is IPanelItemVerticalSizeable panelItemVerticalSizeable &&
                 oldPanelItem is IPanelItemVerticalSizeable oldPanelItemVerticalSizeable ) {
                panelItemVerticalSizeable.Height = oldPanelItemVerticalSizeable.Height;
            }
        }

        if ( PanelItem is PanelItemValue ) {
            _panelControl = new PanelItemInfoSensorValue(EditMode);
            _panelControl.Bind(PanelItemInfoSensorValue.SensorsProperty, new Binding(nameof(NumberSensors)));
            _panelControl.Bind(PanelItemInfoSensorValue.SelectedSensorProperty,
                new Binding(nameof(SelectedNumberSensor), BindingMode.TwoWay));
            _panelControl.Bind(PanelItemInfoSensorValue.PanelItemProperty, new Binding(nameof(PanelItem)));
        }
        else if ( PanelItem is PanelItemObjectSensor ) {
            _panelControl = new PanelItemInfoSensorObject(EditMode);
            _panelControl.Bind(PanelItemInfoSensorObject.SensorsProperty, new Binding(nameof(ObjectSensors)));
            _panelControl.Bind(PanelItemInfoSensorObject.SelectedSensorProperty,
                new Binding(nameof(SelectedObjectSensor), BindingMode.TwoWay));
            _panelControl.Bind(PanelItemInfoSensorObject.PanelItemProperty, new Binding(nameof(PanelItem)));
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
            _panelControl.Bind(PanelItemInfoSensorChart.SensorsProperty, new Binding(nameof(NumberSensors)));
            _panelControl.Bind(PanelItemInfoSensorChart.SelectedSensorProperty,
                new Binding(nameof(SelectedNumberSensor), BindingMode.TwoWay));
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
        OnPropertyChanged(nameof(SelectedNumberSensor));
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
            SensorPanelItemType.SensorObject => MountNameForItem(item as PanelItemSensor, "text"),
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
