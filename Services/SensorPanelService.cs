using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Sensors;
using DravusSensorPanel.Views.Windows;

namespace DravusSensorPanel.Services;

public class SensorPanelService {
    private SensorPanel? _sensorPanel;

    public SensorPanel SensorPanel {
        get => _sensorPanel!;
        private set => _sensorPanel = value;
    }


    private readonly SensorPanelFileService _sensorPanelFileService;

    public SensorPanelService(SensorPanelFileService sensorPanelFileService) {
        _sensorPanelFileService = sensorPanelFileService;
    }

    public PanelItem? GetItemById(string id) {
        return _sensorPanel?.Items.FirstOrDefault(item => item.Id == id);
    }

    public void LoadSensorPanel(
        string filePath = SensorPanelFileService.DefaultSensorPanelPath,
        bool ignorePersonalFields = false) {
        Screen display = GetPrimaryDisplay() ?? GetAllDisplays()[0];

        SensorPanel = _sensorPanelFileService.Load(filePath) ?? new SensorPanel { Display = display };

        if ( ignorePersonalFields ) {
            SensorPanel.Display = display;
            SensorPanel.X = 0;
            SensorPanel.Y = 0;
        }

        ChangeSensorPanelXY(SensorPanel.X, SensorPanel.Y);

        foreach (PanelItem item in SensorPanel.Items) {
            if ( item is PanelItemSensor { Sensor: not null } itemSensor ) {
                itemSensor.Sensor.InUse = true;
            }
        }
    }

    public void ChangeSensorPanelXY(int x, int y) {
        int panelW = SensorPanel.Width;
        int panelH = SensorPanel.Height;

        SensorPanel.X = Math.Clamp(
            x,
            0,
            SensorPanel.Display.WorkingArea.Width - panelW
        );

        SensorPanel.Y = Math.Clamp(
            y,
            0,
            SensorPanel.Display.WorkingArea.Height - panelH
        );
    }

    public void SavePanel(string filePath = SensorPanelFileService.DefaultSensorPanelPath) {
        _sensorPanelFileService.Save(_sensorPanel!, filePath);
    }

    public void SortItems() {
        MainWindow.IgnoreCollectionChanged = true;
        List<PanelItem> sorted = SensorPanel.Items.OrderBy(item => item.Sort).ToList();
        SensorPanel.Items.Clear();
        foreach ( PanelItem item in sorted ) {
            SensorPanel.Items.Add(item);
        }

        MainWindow.IgnoreCollectionChanged = false;
    }

    public void AddNewItem(PanelItem item, bool persist = true) {
        SensorPanel.Items.Add(item);

        if ( item is PanelItemSensor { Sensor: not null } itemSensor ) {
            itemSensor.Sensor.InUse = true;
        }

        item.Reload();

        if ( persist ) {
            if ( SensorPanel.Items.Count != 0 && item.Sort == 0 ) {
                item.Sort = SensorPanel.Items.Count;
            }

            SortItems();
            SavePanel();
        }
    }

    private bool CheckIfSensorIsInUse(Sensor sensor) {
        return SensorPanel.Items.FirstOrDefault(s => s is PanelItemSensor pis && pis.Sensor?.Id == sensor.Id) != null;
    }

    public void EditItem(PanelItem item, PanelItem oldItem, bool persist = true) {
        if ( item.Type == oldItem.Type ) {
            item.Reload();
        }
        else {
            RemoveItem(oldItem, false);
            AddNewItem(item);
        }

        if ( oldItem is PanelItemSensor { Sensor: not null } oldItemSensor ) {
            oldItemSensor.Sensor.InUse = CheckIfSensorIsInUse(oldItemSensor.Sensor);
        }

        if ( item is PanelItemSensor { Sensor: not null } itemSensor ) {
            itemSensor.Sensor.InUse = CheckIfSensorIsInUse(itemSensor.Sensor);
        }

        if ( persist ) {
            SavePanel();

            if ( item is PanelItemImage itemImage && oldItem is PanelItemImage oldItemImage ) {
                if ( itemImage.ImagePath != oldItemImage.ImagePath ) {
                    DeleteImageFromItem(oldItemImage);
                }
            }
        }
    }

    public void RemoveItem(PanelItem item, bool persist = true, bool removeImage = true) {
        // Item can be a clone
        PanelItem? panelFound = SensorPanel.Items.FirstOrDefault(it => it.Id == item.Id);

        if ( panelFound != null ) SensorPanel.Items.Remove(panelFound);

        if ( item is PanelItemSensor { Sensor: not null } itemSensor ) {
            itemSensor.Sensor.InUse = CheckIfSensorIsInUse(itemSensor.Sensor);
        }

        if ( persist ) {
            SortItems();
            SavePanel();
        }

        if ( removeImage ) {
            DeleteImageFromItem(item);
        }
    }

    private void DeleteImageFromItem(PanelItem item) {
        if ( item is PanelItemImage panelItemImage ) {
            if ( File.Exists(panelItemImage.ImagePath) ) {
                File.Delete(panelItemImage.ImagePath);
            }
        }
    }

    private IReadOnlyList<Screen> GetAllDisplays() {
        Window window = ( Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime )!
            .MainWindow!;

        return window.Screens.All;
    }

    public Screen? GetPrimaryDisplay() {
        Window window = ( Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime )!
            .MainWindow!;

        return window.Screens.Primary;
    }
}
