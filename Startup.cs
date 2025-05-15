using System;
using System.Collections.Generic;
using System.Linq;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Units;
using DravusSensorPanel.Repositories;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;
using DravusSensorPanel.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using UnitsNet;
using EditPanelWindow = DravusSensorPanel.Views.Windows.EditPanelWindow;
using MainWindow = DravusSensorPanel.Views.Windows.MainWindow;
using PanelItemFormWindow = DravusSensorPanel.Views.Windows.PanelItemFormWindow;

namespace DravusSensorPanel;

public static class Startup {
    public static IServiceProvider ConfigureServices() {
        var services = new ServiceCollection();

        services.AddSingleton<SensorRepository>();
        services.AddSingleton<UnitRepository>();

        services.AddSingleton<SensorPanelService>();
        services.AddSingleton<SensorPanelFileService>();
        services.AddSingleton<UnitService>();
        services.AddSingleton<ImageService>();
        services.AddSingleton<FileDialogService>();
        services.AddSingleton<UtilService>();
        services.AddSingleton<SensorPanelImportService>();

        services.AddSingleton<IInfoExtractor, LibreHardwareExtractor>();
        services.AddTransient<IInfoExtractor, RtssHardwareExtractor>();
        services.AddTransient<IInfoExtractor, SystemExtractor>();

        services.AddSingleton<Dictionary<string, Unit>>(_ => RtssHardwareExtractor.UnitsByName);
        services.AddSingleton<Dictionary<string, Unit>>(_ => SystemExtractor.UnitsByName);
        services.AddSingleton<Dictionary<string, Unit>>(_ => {
            var units = new Dictionary<string, Unit>();
            foreach ( UnitInfo unitInfo in Quantity
                                           .Infos
                                           .SelectMany(q => q.UnitInfos) ) {
                units[UnitUnitsNet.GetIdFromEnum(unitInfo.Value)] = new UnitUnitsNet(unitInfo.Value);
            }

            return units;
        });

        AddWindows(services);

        return services.BuildServiceProvider();
    }

    private static void AddWindows(ServiceCollection services) {
        services.AddTransient<SplashScreenWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<EditPanelWindow>();
        services.AddTransient<PanelItemFormWindow>();
        services.AddTransient<PanelSettingsWindow>();
        services.AddTransient<AboutWindow>();

        services.AddTransient<Func<SplashScreenWindow>>(sp => sp.GetRequiredService<SplashScreenWindow>);
        services.AddTransient<Func<MainWindow>>(sp => sp.GetRequiredService<MainWindow>);
        services.AddTransient<Func<EditPanelWindow>>(sp => sp.GetRequiredService<EditPanelWindow>);
        services.AddTransient<Func<PanelSettingsWindow>>(sp => sp.GetRequiredService<PanelSettingsWindow>);
        services.AddTransient<Func<AboutWindow>>(sp => sp.GetRequiredService<AboutWindow>);

        services.AddTransient<Func<PanelItem?, PanelItemFormWindow>>(sp =>
            panelItem => {
                if ( panelItem is null ) {
                    return sp.GetRequiredService<PanelItemFormWindow>();
                }

                return ActivatorUtilities.CreateInstance<PanelItemFormWindow>(sp, panelItem);
            });
    }
}
