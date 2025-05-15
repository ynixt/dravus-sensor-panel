using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace DravusSensorPanel.Services;

public class FileDialogService {
    public async Task<string?> OpenImageFileDialog(Visual visual) {
        var topLevel = TopLevel.GetTopLevel(visual);
        var options = new FilePickerOpenOptions {
            Title = "Choose a image",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Images") {
                    Patterns = [
                        "*.bmp",
                        "*.gif",
                        "*.jpg",
                        "*.jpeg",
                        "*.pbm",
                        "*.png",
                        "*.tif",
                        "*.tiff",
                        "*.tga",
                        "*.webp",
                        "*.qoi",
                    ],
                },
            ],
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if ( files == null || files.Count == 0 ) {
            return null;
        }

        IStorageFile file = files.First();
        Uri sourceUri = file.Path;
        return sourceUri.LocalPath ?? sourceUri.AbsolutePath;
    }

    public async Task<string?> OpenDravusFileDialog(Visual visual) {
        var topLevel = TopLevel.GetTopLevel(visual);
        var options = new FilePickerOpenOptions {
            Title = "Choose the Dravus sensor panel file",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Dravus Sensor Panel") {
                    Patterns = [
                        "*.dravus",
                    ],
                },
            ],
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if ( files == null || files.Count == 0 ) {
            return null;
        }

        IStorageFile file = files.First();
        Uri sourceUri = file.Path;
        return sourceUri.LocalPath ?? sourceUri.AbsolutePath;
    }

    public async Task<string?> SaveDravusFileDialog(Visual visual) {
        var topLevel = TopLevel.GetTopLevel(visual);
        var options = new FilePickerSaveOptions {
            Title = "Export Dravus sensor panel",
            FileTypeChoices = [
                new FilePickerFileType("Dravus Sensor Panel") {
                    Patterns = ["*.dravus"],
                    AppleUniformTypeIdentifiers = new[] { "com.ynixt.dravus-sensor-panel" },
                    MimeTypes = ["application/vnd.ynixt.dravus-sensor-panel+zip"],
                },
            ],
            DefaultExtension = ".dravus",
        };

        IStorageFile file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if ( file == null ) {
            return null;
        }

        Uri sourceUri = file.Path;
        return sourceUri.LocalPath ?? sourceUri.AbsolutePath;
    }
}
