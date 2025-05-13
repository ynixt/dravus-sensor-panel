using Avalonia.Media.Imaging;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models.Dtos;

namespace DravusSensorPanel.Models;

public sealed class PanelItemImage : PanelItem, IPanelItemSizeable {
    private string _imagePath = string.Empty;
    private Bitmap? _bitmap;
    private int _width;
    private int _height;

    public override SensorPanelItemType Type => SensorPanelItemType.Image;

    public int Width {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int Height {
        get => _height;
        set => SetField(ref _height, value);
    }

    public string ImagePath {
        get => _imagePath;
        set {
            if ( !SetField(ref _imagePath, value) ) return;
            LoadBitmap();
        }
    }

    public Bitmap? ImageBitmap {
        get => _bitmap;
        private set => SetField(ref _bitmap, value);
    }

    public override void Reload() {
        base.Reload();
        LoadBitmap();
    }

    public override void Dispose() {
        base.Dispose();
        ImageBitmap?.Dispose();
        ImageBitmap = null;
    }

    public override PanelItem Clone() {
        var clone = new PanelItemImage {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Width = Width,
            Height = Height,
            ImagePath = ImagePath,
        };

        return clone;
    }

    public override PanelItemImageDto ToDto() {
        return new PanelItemImageDto {
            Id = Id,
            X = X,
            Y = Y,
            ZIndex = ZIndex,
            Description = Description,
            Type = Type,

            Width = Width,
            Height = Height,
            ImagePath = ImagePath,
        };
    }

    private void LoadBitmap() {
        ImageBitmap?.Dispose();
        ImageBitmap = string.IsNullOrWhiteSpace(_imagePath) ? null : new Bitmap(_imagePath);
    }
}
