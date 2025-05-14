using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using DravusSensorPanel.Models;

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

    public void LoadCurrentSensorPanel() {
        SensorPanel = _sensorPanelFileService.Load() ?? new SensorPanel();

        ChangeSensorPanelXY(SensorPanel.X, SensorPanel.Y);
    }

    public void ChangeSensorPanelXY(int x, int y) {
        int panelW = (int)SensorPanel.Width;
        int panelH = (int)SensorPanel.Height;

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

    public void SavePanel() {
        _sensorPanelFileService.Save(_sensorPanel!);
    }

    public void AddNewItem(PanelItem item, bool persist = true) {
        SensorPanel.Items.Add(item);

        item.Reload();

        if ( persist ) {
            _sensorPanelFileService.Save(_sensorPanel!);
        }
    }

    public void EditItem(PanelItem item, PanelItem oldItem, bool persist = true) {
        if ( item.Type == oldItem.Type ) {
            item.Reload();
        }
        else {
            RemoveItem(oldItem, false);
            AddNewItem(item);
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

        if ( persist ) {
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
}
