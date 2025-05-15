using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;
using DravusSensorPanel.Views.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DravusSensorPanel;

public class App : Application {
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize() {
        ServiceProvider = Startup.ConfigureServices();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) {
            desktop.MainWindow = ServiceProvider.GetRequiredService<SplashScreenWindow>();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e) {
        foreach ( IInfoExtractor? infoExtractor in ServiceProvider.GetServices<IInfoExtractor>() ) {
            infoExtractor.Dispose();
        }

        foreach ( PanelItem sensorPanelItem in ServiceProvider.GetRequiredService<SensorPanelService>().SensorPanel
                                                              .Items ) {
            sensorPanelItem.Dispose();
        }
    }
}
