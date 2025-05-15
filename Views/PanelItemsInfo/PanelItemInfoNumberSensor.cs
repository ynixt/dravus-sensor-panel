using System;
using System.Reactive.Linq;
using DravusSensorPanel.Models;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public abstract class PanelItemInfoNumberSensor : PanelItemInfoSensor {
    public abstract int ValueTypeIndex { get; set; }

    // Empty constructor to preview works on IDE
    protected PanelItemInfoNumberSensor() : this(false) {
    }

    protected PanelItemInfoNumberSensor(bool editMode) : base(editMode) {
    }

    protected override void TrackPanelPropertiesChanged() {
        base.TrackPanelPropertiesChanged();

        _panelItemPropertiesDisposables!.Add(( GPanelItem as PanelItemNumberSensor ).WhenAnyValue(p => p.ValueType)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(s => {
                ValueTypeIndex =
                    ( int ) ( GPanelItem as PanelItemNumberSensor )!
                    .ValueType;
            }));
    }
}
