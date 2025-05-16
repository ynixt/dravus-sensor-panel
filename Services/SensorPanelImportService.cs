using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using DravusSensorPanel.Models;

namespace DravusSensorPanel.Services;

public class SensorPanelImportService {
    private readonly SensorPanelService _sensorPanelService;
    private readonly UtilService _utilService;
    private readonly FileDialogService _fileDialogService;

    public SensorPanelImportService(
        SensorPanelService sensorPanelService,
        UtilService utilService,
        FileDialogService fileDialogService) {
        _sensorPanelService = sensorPanelService;
        _utilService = utilService;
        _fileDialogService = fileDialogService;
    }

    public async Task<bool> ImportUsingDialog(Visual visual) {
        string? path = await _fileDialogService.OpenDravusFileDialog(visual);

        if ( path != null ) {
            Import(path);

            return true;
        }

        return false;
    }

    public async void ExportUsingDialog(Visual visual) {
        string? path = await _fileDialogService.SaveDravusFileDialog(visual);

        if ( path != null ) {
            Export(path);
        }
    }

    private void Export(string dravusFilePath) {
        string tempDir = Path.Combine(Path.GetTempPath(), $"dravus_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try {
            //---------------- sensorpanel.yaml -------------------
            string panelPath = Path.Combine(tempDir, "sensorpanel.yaml");
            _sensorPanelService.SavePanel(panelPath);

            //---------------- imagesl -------------------
            string imagesDir = Path.Combine(tempDir, "images");
            Directory.CreateDirectory(imagesDir);

            IEnumerable<string> imagePaths = _sensorPanelService.SensorPanel.Items
                                                                .OfType<PanelItemImage>()
                                                                .Select(i => i.ImagePath)
                                                                .Distinct()
                                                                .Where(File.Exists);

            foreach ( string img in imagePaths ) {
                string dest = Path.Combine(imagesDir, Path.GetFileName(img));
                File.Copy(img, dest, true);
            }

            //---------------- manifest.yaml -------------------
            string manifestPath = Path.Combine(tempDir, "manifest.yaml");
            File.WriteAllText(manifestPath,
                $"app_version: {_utilService.GetAppVersion()}");

            //---------------- ZIP (.dravus) --------------------
            if ( File.Exists(dravusFilePath) ) {
                File.Delete(dravusFilePath);
            }

            ZipFile.CreateFromDirectory(
                tempDir,
                dravusFilePath,
                CompressionLevel.Optimal,
                false);
        }
        finally {
            if ( Directory.Exists(tempDir) ) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    public void Import(string dravusFilePath) {
        if ( !File.Exists(dravusFilePath) ) {
            throw new FileNotFoundException("File .dravus not found.", dravusFilePath);
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"dravus_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try {
            ZipFile.ExtractToDirectory(dravusFilePath, tempDir);

            //---------------- images --------------------------
            string extractedImagesDir = Path.Combine(tempDir, "images");
            string exeImagesDir = Path.Combine(AppContext.BaseDirectory, "images");

            if ( Directory.Exists(exeImagesDir) ) {
                Directory.Delete(exeImagesDir, true);
            }

            if ( Directory.Exists(extractedImagesDir) ) {
                DirectoryCopy(extractedImagesDir, exeImagesDir, true);
            }

            //---------------- sensorpanel.yaml ----------------
            string panelPath = Path.Combine(tempDir, "sensorpanel.yaml");
            _sensorPanelService.LoadSensorPanel(panelPath, true);
            _sensorPanelService.SavePanel();
        }
        finally {
            if ( Directory.Exists(tempDir) ) {
                Directory.Delete(tempDir, true);
            }
        }
    }

#region Helpers

    private static void DirectoryCopy(string sourceDir, string destDir, bool recursive) {
        var dir = new DirectoryInfo(sourceDir);

        if ( !dir.Exists ) {
            throw new DirectoryNotFoundException($"Directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destDir);

        foreach ( FileInfo file in dir.GetFiles() ) {
            string target = Path.Combine(destDir, file.Name);
            file.CopyTo(target, true);
        }

        if ( recursive ) {
            foreach ( DirectoryInfo subDir in dir.GetDirectories() ) {
                string newDest = Path.Combine(destDir, subDir.Name);
                DirectoryCopy(subDir.FullName, newDest, recursive);
            }
        }
    }

#endregion
}
