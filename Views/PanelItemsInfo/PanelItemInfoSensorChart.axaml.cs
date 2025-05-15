using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Sensors;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoSensorChart : PanelItemInfoNumberSensor {
    public static readonly StyledProperty<IEnumerable<NumberSensor>> SensorsProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorChart, IEnumerable<NumberSensor>>(nameof(Sensors));

    private int _valueTypeIndex;
    private int _chartTypeIndex;
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

    public bool IsHorizontalChart => PanelItem?.ChartType is ChartType.HorizontalBar or ChartType.HorizontalBars;
    public bool IsVerticalChart => !IsHorizontalChart;

    public int CharTypeIndex {
        get => _chartTypeIndex;
        set {
            _chartTypeIndex = value;

            if ( PanelItem != null ) {
                PanelItem.ChartType = ( ChartType ) _chartTypeIndex;

                OnPropertyChanged(nameof(IsHorizontalChart));
                OnPropertyChanged(nameof(IsVerticalChart));

                if ( IsHorizontalChart ) {
                    PanelItem.YMaxValue = null;
                    PanelItem.YMaxValue = null;

                    PanelItem.ShowYAxis = PanelItem.ChartType is not ChartType.HorizontalBar;
                }
                else {
                    PanelItem.XMaxValue = null;
                    PanelItem.XMaxValue = null;
                    PanelItem.ShowYAxis = true;
                }
            }
        }
    }

    public IEnumerable<NumberSensor> Sensors {
        get => GetValue(SensorsProperty);
        set => SetValue(SensorsProperty, value);
    }

    public static readonly StyledProperty<NumberSensor?> SelectedSensorProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorChart, NumberSensor?>(nameof(SelectedSensor));

    public NumberSensor? SelectedSensor {
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

        _panelItemDisposable = this.GetObservable(PanelItemProperty).ObserveOn(RxApp.MainThreadScheduler)
                                   .Subscribe(_ => { TrackPanelPropertiesChanged(); });
    }

    protected override void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        base.OnDetached(sender, e);
        _panelItemDisposable?.Dispose();
        PanelItem?.Dispose();
    }
}
