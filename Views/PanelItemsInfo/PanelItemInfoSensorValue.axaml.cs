using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Sensors;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoSensorValue : PanelItemInfoNumberSensor {
    public static readonly StyledProperty<IEnumerable<NumberSensor>> SensorsProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, IEnumerable<NumberSensor>>(nameof(Sensors));

    public static readonly StyledProperty<NumberSensor?> SelectedSensorProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, NumberSensor?>(nameof(SelectedSensor));

    public static readonly StyledProperty<PanelItemValue?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorValue, PanelItemValue?>(nameof(PanelItem));

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

    public IEnumerable<NumberSensor> Sensors {
        get => GetValue(SensorsProperty);
        set => SetValue(SensorsProperty, value);
    }

    public NumberSensor? SelectedSensor {
        get => GetValue(SelectedSensorProperty);
        set => SetValue(SelectedSensorProperty, value);
    }

    public PanelItemValue? PanelItem {
        get => GetValue(PanelItemProperty);
        set => SetValue(PanelItemProperty, value);
    }

    protected override PanelItemSensor GPanelItem => PanelItem;

    // Empty constructor to preview works on IDE
    public PanelItemInfoSensorValue() : this(false) {
    }

    public PanelItemInfoSensorValue(bool editMode) : base(editMode) {
        InitializeComponent();

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    public override bool IsValid() {
        if ( PanelItem == null ) {
            return false;
        }

        return PanelItem.Sensor != null && IsRegexPatternValid(PanelItem.ReplaceRegexPattern);
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
