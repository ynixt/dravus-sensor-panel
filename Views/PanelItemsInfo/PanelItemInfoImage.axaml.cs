using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoImage : PanelItemInfo {
    public static readonly StyledProperty<PanelItemImage?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoImage, PanelItemImage?>(nameof(PanelItem));

    public PanelItemImage? PanelItem {
        get => GetValue(PanelItemProperty);
        set {
            SetValue(PanelItemProperty, value);
            OnPropertyChanged(nameof(PanelItemImage));
        }
    }

    // Empty constructor to preview works on IDE
    public PanelItemInfoImage() : this(false) {
    }

    public PanelItemInfoImage(bool editMode) : base(editMode) {
        InitializeComponent();

        DetachedFromVisualTree += OnDetached;
    }

    public override bool IsValid() {
        if ( PanelItem == null ) {
            return false;
        }

        return true; // TODO
    }

    private async void ChooseImageClick(object? sender, RoutedEventArgs e) {
        if ( PanelItem == null ) return;

        string? sourcePath =
            await App.ServiceProvider!.GetRequiredService<FileDialogService>().OpenImageFileDialog(this);

        if ( sourcePath == null ) return;

        string exeDir = AppContext.BaseDirectory;
        string imagesDir = Path.Combine(exeDir, "images");
        Directory.CreateDirectory(imagesDir);
        string originalExt = Path.GetExtension(sourcePath);

        string destPath = Path.Combine(imagesDir, Guid.NewGuid().ToString());

        ImageInfo imageCopied = App.ServiceProvider!.GetRequiredService<ImageService>()
                                   .CopyImage(sourcePath, destPath, originalExt);
        Dispatcher.UIThread.Post(() => {
            PanelItem.ImagePath = Path.Combine("./", "images", imageCopied.Name).Replace('\\', '/');
            PanelItem.Width = imageCopied.Width;
            PanelItem.Height = imageCopied.Height;

            Console.WriteLine(PanelItem.ImagePath);

            OnPropertyChanged(nameof(PanelItem));
        });
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        PanelItem?.Dispose();
    }
}
