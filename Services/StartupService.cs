using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;

namespace DravusSensorPanel.Services;

/// <summary>
///     Service to manage automatic start-up of the application on each supported OS.
///     Windows: Scheduled Task (elevated); Linux: XDG autostart; macOS: LaunchAgents.
/// </summary>
public class StartupService {
    private static readonly string AppName = Assembly.GetEntryAssembly()?
                                                 .GetCustomAttribute<AssemblyProductAttribute>()?
                                                 .Product
                                             ?? Assembly.GetEntryAssembly()?.GetName().Name!;

    private static readonly string ExecutablePath = GetExecutablePath();

    private static string GetExecutablePath() {
        string? path = Environment.ProcessPath;

        return !string.IsNullOrEmpty(path) ? path : Process.GetCurrentProcess().MainModule!.FileName!;
    }

    public void Enable() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ) {
            EnableWindows();
        }
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ) {
            EnableLinux();
        }
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ) {
            EnableMac();
        }
        else {
            throw new PlatformNotSupportedException("Platform not supported for StartupService.");
        }
    }

    public void Disable() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ) {
            DisableWindows();
        }
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ) {
            DisableLinux();
        }
        else if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ) {
            DisableMac();
        }
        else {
            throw new PlatformNotSupportedException("Platform not supported for StartupService.");
        }
    }

    public bool IsEnabled() {
        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ) {
            return IsEnabledWindows();
        }

        if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ) {
            return IsEnabledLinux();
        }

        if ( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ) {
            return IsEnabledMac();
        }

        throw new PlatformNotSupportedException("Platform not supported for StartupService.");
    }

#region Windows (Scheduled Task, elevated)

    private const string TaskName = "DravusSensorPanel_AutoStart";

    private void EnableWindows() {
        using var ts = new TaskService();

        TaskDefinition td = ts.NewTask();
        td.RegistrationInfo.Description = $"Automatically starts {AppName} on user logon (elevated)";

        td.Principal.UserId = WindowsIdentity.GetCurrent().Name;
        td.Principal.LogonType = TaskLogonType.InteractiveToken;
        td.Principal.RunLevel = TaskRunLevel.Highest;

        td.Triggers.Add(new LogonTrigger());

        td.Actions.Add(new ExecAction(
            ExecutablePath,
            null,
            Path.GetDirectoryName(ExecutablePath)));

        ts.RootFolder.RegisterTaskDefinition(
            TaskName,
            td,
            TaskCreation.CreateOrUpdate,
            null, // use current security context
            null,
            TaskLogonType.InteractiveToken);
    }

    private void DisableWindows() {
        using var ts = new TaskService();
        ts.RootFolder.DeleteTask(TaskName, false);
    }

    private bool IsEnabledWindows() {
        using var ts = new TaskService();
        return ts.GetTask(TaskName) is not null;
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
            $"Comment=Start {AppName} on initialization\n";
        File.WriteAllText(path, content);
    }

    private void DisableLinux() {
        string path = GetDesktopFilePath();
        if ( File.Exists(path) ) {
            File.Delete(path);
        }
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
        if ( File.Exists(path) ) {
            File.Delete(path);
        }
    }

    private bool IsEnabledMac() {
        return File.Exists(GetPlistPath());
    }

#endregion
}
