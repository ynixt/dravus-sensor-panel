using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoSensorValue : PanelItemInfo {
    public static readonly StyledProperty<IEnumerable<Sensor>> SensorsProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, IEnumerable<Sensor>>(nameof(Sensors));

    public static readonly StyledProperty<Sensor?> SelectedSensorProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, Sensor?>(nameof(SelectedSensor));

    public static readonly StyledProperty<PanelItemValue?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, PanelItemValue?>(nameof(PanelItem));

    private int _valueTypeIndex;
    private PropertyChangedEventHandler? _handler;
    private PanelItemValue? _handlerUsedIn;
    private IDisposable? _panelItemDisposable;

    public ObservableCollection<Enum> PossibleUnits { get; } = new();

    public int ValueTypeIndex {
        get => _valueTypeIndex;
        set {
            _valueTypeIndex = value;

            if ( PanelItem != null ) {
                PanelItem.ValueType = ( PanelItemSensorValueType ) _valueTypeIndex;
            }
        }
    }

    public IEnumerable<Sensor> Sensors {
        get => GetValue(SensorsProperty);
        set => SetValue(SensorsProperty, value);
    }

    public Sensor? SelectedSensor {
        get => GetValue(SelectedSensorProperty);
        set => SetValue(SelectedSensorProperty, value);
    }

    public PanelItemValue? PanelItem {
        get => GetValue(PanelItemProperty);
        set => SetValue(PanelItemProperty, value);
    }

    public PanelItemInfoSensorValue(bool editMode = false) : base(editMode) {
        InitializeComponent();

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    public override bool IsValid() {
        if ( PanelItem == null ) {
            return false;
        }

        return PanelItem.Label.Trim().Length > 0 && PanelItem.Sensor != null;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) {
        if ( _handler != null && _handlerUsedIn != null ) _handlerUsedIn.PropertyChanged -= _handler;

        _handler = PanelItemPropertyChanged;
        _handlerUsedIn = PanelItem!;
        _handlerUsedIn.PropertyChanged += _handler;

        LoadUnits();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        _panelItemDisposable?.Dispose();
        PanelItem?.Dispose();
        if ( _handler != null && _handlerUsedIn != null ) _handlerUsedIn.PropertyChanged -= _handler;
    }

    private void PanelItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if ( e.PropertyName == nameof(PanelItem.ValueType) ) {
            ValueTypeIndex = ( int ) PanelItem!.ValueType;
        }

        if ( e.PropertyName == nameof(PanelItem.Sensor) ) {
            LoadUnits();
        }
    }

    private void LoadUnits() {
        Enum? oldUnit = PanelItem?.Unit;
        PossibleUnits.Clear();

        if ( PanelItem?.Sensor != null ) {
            PossibleUnits.AddRange(
                App.ServiceProvider!.GetRequiredService<UnitService>().GetPossibleUnits(PanelItem.Sensor.Unit)
            );

            PanelItem.Unit = null;
            PanelItem.Unit = EditMode ? oldUnit : PanelItem.Sensor.Unit;
        }
    }
}
