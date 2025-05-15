using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Sensors;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoSensorObject : PanelItemInfoSensor {
    public static readonly StyledProperty<IEnumerable<ObjectSensor>> SensorsProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorObject, IEnumerable<ObjectSensor>>(nameof(Sensors));

    public static readonly StyledProperty<ObjectSensor?> SelectedSensorProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorObject, ObjectSensor?>(nameof(SelectedSensor));

    public static readonly StyledProperty<PanelItemObjectSensor?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoSensorObject, PanelItemObjectSensor?>(nameof(PanelItem));

    private IDisposable? _panelItemDisposable;

    public IEnumerable<ObjectSensor> Sensors {
        get => GetValue(SensorsProperty);
        set => SetValue(SensorsProperty, value);
    }

    public ObjectSensor? SelectedSensor {
        get => GetValue(SelectedSensorProperty);
        set => SetValue(SelectedSensorProperty, value);
    }

    public PanelItemObjectSensor? PanelItem {
        get => GetValue(PanelItemProperty);
        set => SetValue(PanelItemProperty, value);
    }

    protected override PanelItemSensor GPanelItem => PanelItem;

    // Empty constructor to preview works on IDE
    public PanelItemInfoSensorObject() : this(false) {
    }

    public PanelItemInfoSensorObject(bool editMode) : base(editMode) {
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
