using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractors;

namespace DravusSensorPanel.Views.Windows;

public partial class SplashScreenWindow : WindowViewModel {
    private int _loadingState;
    private Bitmap _loadingImage;
    private readonly IDisposable? _loadingDisposable;
    private readonly List<Bitmap> _loadingImages = new();

    private Func<MainWindow>? mainWindowFactory = null;

    public Bitmap LoadingImage {
        get => _loadingImage;
        set => RaiseAndSetIfChanged(ref _loadingImage, value);
    }

    public SplashScreenWindow() : this(null, null, null) {
    }

    public SplashScreenWindow(
        IEnumerable<InfoExtractor>? infoExtractors,
        Func<MainWindow>? mainWindowFactory,
        SensorPanelService? sensorPanelService) {
        DataContext = this;
        InitializeComponent();

        LoadAllLoadingImages();
        LoadingImage = _loadingImages[0];

        Closed += (_, __) => {
            _loadingDisposable?.Dispose();
            _loadingImages.ForEach(i => i.Dispose());
        };

        _loadingDisposable = Observable.Interval(TimeSpan.FromMilliseconds(300)).Subscribe(_ => {
            _loadingState = ( _loadingState + 1 ) % _loadingImages.Count;
            Dispatcher.UIThread.Post(() => { LoadingImage = _loadingImages[_loadingState]; });
        });

        if ( mainWindowFactory == null || infoExtractors == null || sensorPanelService == null ) {
            return;
        }

        new Thread(o => {
            DateTime now = DateTime.Now;
            foreach ( InfoExtractor infoExtractor in infoExtractors ) {
                infoExtractor.Start();
                infoExtractor.Dispose();
            }

            int elapsed = ( DateTime.Now - now ).Milliseconds;
            int remaining = 3000 - elapsed;
            Console.WriteLine($"Loaded in {elapsed}ms. Will wait {remaining}ms.");

            if ( remaining > 0 ) Thread.Sleep(remaining);

            Dispatcher.UIThread.Post(() => {
                sensorPanelService.LoadSensorPanel();

                MainWindow mainWindow = mainWindowFactory.Invoke();
                mainWindow.Show();
                Close();
            });
        }).Start();
    }

    private string GetLoadingImageUrl(int i) {
        return "avares://DravusSensorPanel/Assets/loading/loading-" + i + ".png";
    }

    private void LoadAllLoadingImages() {
        for ( int i = 0; i < 3; i++ ) {
            _loadingImages.Add(new Bitmap(AssetLoader.Open(new Uri(GetLoadingImageUrl(i)))));
        }
    }
}
