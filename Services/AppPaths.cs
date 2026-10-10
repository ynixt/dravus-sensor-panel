using System;
using System.IO;

namespace DravusSensorPanel.Services;

/// <summary>
///     Central place for every file/directory the app reads or writes.
///     Windows: next to the executable (keeps the old behaviour).
///     Linux/macOS: user data directory ($XDG_CONFIG_HOME/DravusSensorPanel or ~/.config/DravusSensorPanel),
///     because the executable directory is usually read-only (or a temporary extraction dir for single-file
///     builds) and the current working directory is not reliable (desktop launchers, autostart...).
/// </summary>
public static class AppPaths {
    private const string AppFolderName = "DravusSensorPanel";
    private const string SensorPanelFileName = "sensorpanel.yaml";
    private const string ImagesFolderName = "images";

    private static readonly Lazy<string> DataDirectoryLazy = new(CreateDataDirectory);

    public static string DataDirectory => DataDirectoryLazy.Value;

    public static string SensorPanelFile => Path.Combine(DataDirectory, SensorPanelFileName);

    public static string ImagesDirectory => Path.Combine(DataDirectory, ImagesFolderName);

    /// <summary>
    ///     Paths saved inside sensorpanel.yaml are relative (e.g. "./images/abc.png") so the panel can be exported.
    ///     This resolves them against <see cref="DataDirectory" /> instead of the current working directory.
    /// </summary>
    public static string Resolve(string path) {
        if ( string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) ) {
            return path;
        }

        return Path.GetFullPath(Path.Combine(DataDirectory, path));
    }

    private static string CreateDataDirectory() {
        if ( OperatingSystem.IsWindows() ) {
            return AppContext.BaseDirectory;
        }

        string configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if ( string.IsNullOrWhiteSpace(configHome) ) {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        string dir = Path.Combine(configHome, AppFolderName);
        Directory.CreateDirectory(dir);

        MigrateLegacyData(dir);

        return dir;
    }

    /// <summary>
    ///     Older Linux/macOS builds saved the panel next to the executable or in the working directory.
    ///     Copy it to the new location once, so nobody loses their panel after updating.
    /// </summary>
    private static void MigrateLegacyData(string dataDir) {
        if ( File.Exists(Path.Combine(dataDir, SensorPanelFileName)) ) return;

        foreach ( string legacyDir in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory } ) {
            try {
                string legacyPanel = Path.Combine(legacyDir, SensorPanelFileName);
                if ( !File.Exists(legacyPanel) ) continue;

                File.Copy(legacyPanel, Path.Combine(dataDir, SensorPanelFileName));

                string legacyImages = Path.Combine(legacyDir, ImagesFolderName);
                if ( Directory.Exists(legacyImages) ) {
                    string newImages = Path.Combine(dataDir, ImagesFolderName);
                    Directory.CreateDirectory(newImages);

                    foreach ( string image in Directory.GetFiles(legacyImages) ) {
                        File.Copy(image, Path.Combine(newImages, Path.GetFileName(image)), true);
                    }
                }

                Console.WriteLine($"Migrated sensor panel from {legacyDir} to {dataDir}");
                return;
            }
            catch ( Exception e ) {
                Console.WriteLine($"Failed to migrate sensor panel from {legacyDir}: {e.Message}");
            }
        }
    }
}
