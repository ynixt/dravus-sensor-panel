using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Services;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public abstract class PanelItemInfoSensor : PanelItemInfo {
    public ObservableCollection<Unit> PossibleUnits { get; } = new();

    protected abstract PanelItemSensor? GPanelItem { get; }

    protected List<IDisposable>? _panelItemPropertiesDisposables;
    private IDisposable? _panelItemDisposable;

    // Empty constructor to preview works on IDE
    public PanelItemInfoSensor() : this(false) {
    }

    public PanelItemInfoSensor(bool editMode) : base(editMode) {
    }

    protected virtual void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) {
        _panelItemDisposable?.Dispose();
    }

    protected virtual void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        _panelItemPropertiesDisposables?.ForEach(p => p.Dispose());
    }

    protected virtual void TrackPanelPropertiesChanged() {
        _panelItemPropertiesDisposables?.ForEach(p => p.Dispose());

        _panelItemPropertiesDisposables = [
            GPanelItem.WhenAnyValue(p => p.Sensor)
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(s => { LoadUnits(); }),
        ];
    }

    protected void LoadUnits() {
        Unit? oldUnit = GPanelItem?.Unit;
        PossibleUnits.Clear();

        if ( GPanelItem?.Sensor != null ) {
            GPanelItem.Unit = null;
            PossibleUnits.AddRange(
                App.ServiceProvider!.GetRequiredService<UnitService>().GetPossibleUnits(GPanelItem.Sensor.Unit)
            );

            if ( oldUnit != null ) {
                GPanelItem.Unit = oldUnit;
            }
            else {
                GPanelItem.Unit = GPanelItem.Sensor.Unit;
            }
        }
    }
}
