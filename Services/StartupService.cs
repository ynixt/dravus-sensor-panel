namespace DravusSensorPanel.Services;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;

/// <summary>
/// Service to manage the automatic startup of the application in the operating system.
/// Supports Windows (Registry), Linux (XDG .desktop) and macOS (LaunchAgents).
/// </summary>
public class StartupService {
    private static readonly string AppName = Assembly.GetEntryAssembly()?
                                                 .GetCustomAttribute<AssemblyProductAttribute>()?
                                                 .Product
                                             ?? Assembly.GetEntryAssembly()?.GetName().Name!;

    private static readonly string ExecutablePath = Assembly.GetEntryAssembly()?.Location
                                                    ?? Process.GetCurrentProcess().MainModule.FileName;

    public void Enable() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
            EnableWindows();
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) )
            EnableLinux();
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) )
            EnableMac();
        else
            throw new PlatformNotSupportedException("Platform not supported for StartupService.");
    }

    /// <summary>
    /// Desabilita a inicialização automática.
    /// </summary>
    public void Disable() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
            DisableWindows();
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) )
            DisableLinux();
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) )
            DisableMac();
        else
            throw new PlatformNotSupportedException("Platform not supported for StartupService.");
    }

    /// <summary>
    /// Retorna se a inicialização automática está habilitada.
    /// </summary>
    public bool IsEnabled() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
            return IsEnabledWindows();
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) )
            return IsEnabledLinux();
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) )
            return IsEnabledMac();
        throw new PlatformNotSupportedException("Platform not supported for StartupService.");
    }

#region Windows (Registry)

    private const string RegistryRunPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    private void EnableWindows() {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: true);
        key.SetValue(AppName, $"\"{ExecutablePath}\"");
    }

    private void DisableWindows() {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: true);
        key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private bool IsEnabledWindows() {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: false);
        return key.GetValue(AppName) != null;
    }

#endregion

#region Linux (XDG Autostart)

    private string GetDesktopFilePath() {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            ".config",
            "autostart");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{AppName}.desktop");
    }

    private void EnableLinux() {
        string path = GetDesktopFilePath();
        string content =
            $"[Desktop Entry]\n" +
            $"Type=Application\n" +
            $"Exec=\"{ExecutablePath}\"\n" +
            $"Hidden=false\n" +
            $"NoDisplay=false\n" +
            $"X-GNOME-Autostart-enabled=true\n" +
            $"Name={AppName}\n" +
            $"Comment=Start {AppName} on inicialization\n";
        File.WriteAllText(path, content);
    }

    private void DisableLinux() {
        string path = GetDesktopFilePath();
        if ( File.Exists(path) )
            File.Delete(path);
    }

    private bool IsEnabledLinux() {
        return File.Exists(GetDesktopFilePath());
    }

#endregion

#region macOS (LaunchAgents)

    private string GetPlistPath() {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Library",
            "LaunchAgents");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"com.{AppName}.plist");
    }

    private void EnableMac() {
        string path = GetPlistPath();
        string content =
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            $"<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
            $"<plist version=\"1.0\">\n" +
            $"<dict>\n" +
            $"  <key>Label</key>\n" +
            $"  <string>com.{AppName}</string>\n" +
            $"  <key>ProgramArguments</key>\n" +
            $"  <array>\n" +
            $"    <string>{ExecutablePath}</string>\n" +
            $"  </array>\n" +
            $"  <key>RunAtLoad</key>\n" +
            $"  <true/>\n" +
            $"</dict>\n" +
            $"</plist>\n";
        File.WriteAllText(path, content);
    }

    private void DisableMac() {
        string path = GetPlistPath();
        if ( File.Exists(path) )
            File.Delete(path);
    }

    private bool IsEnabledMac() {
        return File.Exists(GetPlistPath());
    }

#endregion
}
