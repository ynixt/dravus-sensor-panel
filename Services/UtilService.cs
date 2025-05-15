using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace DravusSensorPanel.Services;

public class UtilService {
    public async Task<bool> OpenSite(Visual visual, string url) {
        ILauncher launcher = TopLevel.GetTopLevel(visual)!.Launcher;
        return await launcher.LaunchUriAsync(new Uri(url));
    }

    public async Task<bool> OpenGithub(Visual visual) {
        const string url = "https://github.com/ynixt/dravus-sensor-panel";

        return await OpenSite(visual, url);
    }

    public async Task<bool> OpenLicense(Visual visual) {
        const string url = "https://github.com/ynixt/dravus-sensor-panel/blob/master/LICENSE";

        return await OpenSite(visual, url);
    }

    public string GetAppVersion() {
        var asm = Assembly.GetEntryAssembly();
        return asm?.GetName().Version?.ToString() ??
               asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? "unknown version";
    }
}
