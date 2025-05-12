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
}
