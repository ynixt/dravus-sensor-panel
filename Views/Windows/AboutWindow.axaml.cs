using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;

namespace DravusSensorPanel.Views.Windows;

public partial class AboutWindow : WindowViewModel {
    private readonly UtilService? _utilService;

    public string Version { get; set; }

    public AboutWindow() : this(null) {
    }

    public AboutWindow(
        UtilService? utilService) {
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
