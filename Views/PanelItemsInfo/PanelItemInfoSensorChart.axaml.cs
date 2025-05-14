using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoSensorChart : PanelItemInfoSensor {
    public static readonly StyledProperty<IEnumerable<Sensor>> SensorsProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorChart, IEnumerable<Sensor>>(nameof(Sensors));

    private int _valueTypeIndex;
    private IDisposable? _panelItemDisposable;

    public override int ValueTypeIndex {
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

    public static readonly StyledProperty<Sensor?> SelectedSensorProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorChart, Sensor?>(nameof(SelectedSensor));

    public Sensor? SelectedSensor {
        get => GetValue(SelectedSensorProperty);
        set => SetValue(SelectedSensorProperty, value);
    }

    public static readonly StyledProperty<PanelItemChart?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorChart, PanelItemChart?>(nameof(PanelItem));

    public PanelItemChart? PanelItem {
        get => GetValue(PanelItemProperty);
        set {
            SetValue(PanelItemProperty, value);
            OnPropertyChanged(nameof(PanelItemChart));
            OnPropertyChanged(nameof(PanelItemInfoSensorChart));
        }
    }

    protected override PanelItemSensor GPanelItem => PanelItem;

    // Empty constructor to preview works on IDE
    public PanelItemInfoSensorChart() : this(false) {
    }

    public PanelItemInfoSensorChart(bool editMode) : base(editMode) {
        InitializeComponent();

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    public override bool IsValid() {
        if ( PanelItem == null ) {
            return false;
        }

        return PanelItem.Sensor != null;
    }

    protected override void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) {
       base.OnAttached(sender, e);

        _panelItemDisposable = this.GetObservable(PanelItemProperty).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => {
            TrackPanelPropertiesChanged();
        });
    }

    protected override void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        base.OnDetached(sender, e);
        _panelItemDisposable?.Dispose();
        PanelItem?.Dispose();
    }
}
