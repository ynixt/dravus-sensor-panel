using Avalonia.Interactivity;
using DravusSensorPanel.Services;

namespace DravusSensorPanel.Views.Windows;

public partial class AboutWindow : WindowViewModel {
    private readonly UtilService? _utilService;

    public string Version { get; set; }

    public AboutWindow() : this(null) {
    }

    public AboutWindow(UtilService? utilService) {
        DataContext = this;

        Version = utilService?.GetAppVersion() ?? string.Empty;

        _utilService = utilService;

        InitializeComponent();
    }

    private void OpenGithubClick(object? sender, RoutedEventArgs e) {
        _utilService?.OpenGithub(this);
    }

    private void OpenLicenseClick(object? sender, RoutedEventArgs e) {
        _utilService?.OpenLicense(this);
    }
}
