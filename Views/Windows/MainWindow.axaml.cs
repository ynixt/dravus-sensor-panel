using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;
using LiveChartsCore.SkiaSharpView.Avalonia;
using ReactiveUI;

namespace DravusSensorPanel.Views.Windows;

public partial class MainWindow : WindowViewModel {
    private readonly IEnumerable<IInfoExtractor>? _infoExtractors;
    private readonly Func<EditPanelWindow>? _editPanelWindowFactory;
    private readonly Dictionary<string, List<Control>> _controlsById = new();

    public bool Collapsed { get; set; } = false;

    private EditPanelWindow? _editWindowOpen;
    private readonly Canvas _canvasPanel;
    private readonly SensorPanelService? _sensorPanelService;
    private IDisposable? _subscriptionDisposable;

    public MainWindow() : this(null, null, null) {
    }

    public MainWindow(
        Func<EditPanelWindow>? editPanelWindowFactory,
        SensorPanelService? sensorPanelService,
        IEnumerable<IInfoExtractor>? infoExtractors) {
        DataContext = this;

        InitializeComponent();

        _editPanelWindowFactory = editPanelWindowFactory;
        _sensorPanelService = sensorPanelService;
        _infoExtractors = infoExtractors;
        _canvasPanel = this.FindControl<Canvas>("CanvasPanel")!;

        Closed += OnWindowClosed;

        if ( _sensorPanelService != null && _infoExtractors != null ) {
            TrackSensorsValue();

            _sensorPanelService.SensorPanel.Items.CollectionChanged += OnCollectionChanged;
        }
    }

    private void TrackSensorsValue() {
        _subscriptionDisposable?.Dispose();
        _subscriptionDisposable = Observable
                                  .Interval(TimeSpan.FromMilliseconds(1000))
                                  .ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => {
                                      _infoExtractors?.ToList().ForEach(e => e.Update());
                                  });
    }

    private void OnWindowClosed(object? sender, EventArgs e) {
        _subscriptionDisposable?.Dispose();
        _infoExtractors?.ToList().ForEach(i => i.Dispose());

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop?.Shutdown();
    }

    private void PanelContainer_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        if ( e.InitialPressMouseButton == MouseButton.Right && sender is Control target ) {
            var menu = new ContextMenu {
                ItemsSource = new[] {
                    new MenuItem {
                        Header = "Edit Panel",
                        Command = ReactiveCommand.Create(OpenEditPanel),
                    },
                },
            };

            menu.Open(target);
        }
    }

    private void OpenEditPanel() {
        if ( _editWindowOpen == null ) {
            _editWindowOpen = _editPanelWindowFactory?.Invoke();

            if ( _editWindowOpen != null ) {
                _editWindowOpen.Show();
                _editWindowOpen.Closed += (_, _) => _editWindowOpen = null;
            }
        }
        else {
            _editWindowOpen.Activate();
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        if ( e.NewItems != null ) {
            foreach ( PanelItem item in e.NewItems ) {
                AddToCanvas(item);
            }
        }

        if ( e.OldItems != null ) {
            foreach ( PanelItem item in e.OldItems ) {
                foreach ( ContentControl contentControl in _controlsById[item.Id] ) {
                    _canvasPanel.Children.Remove(contentControl);
                }

                _controlsById.Remove(item.Id);
            }
        }

        TrackSensorsValue();
    }

    private void AddToCanvas(PanelItem item) {
        List<Control> controls = [];
        Control? control = null;

        switch ( item ) {
            case IPanelItemText itemText:
                control = new Label { DataContext = itemText };

                control.Bind(ContentProperty, new Binding(nameof(itemText.Label)));
                control.Bind(FontSizeProperty, new Binding(nameof(itemText.FontSize)));
                control.Bind(FontFamilyProperty, new Binding(nameof(itemText.FontFamily)));
                control.Bind(ForegroundProperty, new Binding(nameof(itemText.ForegroundBrush)));
                break;
            case PanelItemImage itemImage:
                control = new Image { DataContext = itemImage, Stretch = Stretch.Uniform };

                control.Bind(Image.SourceProperty, new Binding(nameof(itemImage.ImageBitmap)));
                break;
            case PanelItemChart itemChart:
                control = new CartesianChart { DataContext = itemChart, AnimationsSpeed = TimeSpan.Zero };
                control.Bind(CartesianChart.YAxesProperty, new Binding(nameof(itemChart.YAxes)));
                control.Bind(CartesianChart.XAxesProperty, new Binding(nameof(itemChart.XAxes)));
                control.Bind(CartesianChart.SeriesProperty, new Binding(nameof(itemChart.Series)));
                break;
        }

        if ( control != null ) {
            Canvas.SetLeft(control, item.X);
            Canvas.SetTop(control, item.Y);

            control.ZIndex = item.ZIndex;

            _canvasPanel.Children.Add(control);

            controls.Add(control);

            if ( item is PanelItemValue itemValue ) {
                if ( itemValue.ShowUnit ) {
                    controls.Add(CreateUnitLabel(itemValue, control));
                }

                control.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            }

            _controlsById[item.Id] = controls;

            if ( item is IPanelItemHorizontalSizeable horizontalSizeable ) {
                control.Width = horizontalSizeable.Width;
            }

            if ( item is IPanelItemVerticalSizeable verticalSizeable ) {
                control.Height = verticalSizeable.Height;
            }

            if ( item is IPanelItemText ) {
                CalculateDesiredSize(controls);
            }
        }
    }

    private void CalculateDesiredSize(List<Control> controls) {
        Control firstControl = controls.First();

        firstControl.InvalidateMeasure();
        firstControl.Measure(_canvasPanel.Bounds.Size);

        Control previousControl = firstControl;

        for ( int i = 1; i < controls.Count; i++ ) {
            Control currentControl = controls[i];

            double previousControlWidth = previousControl.Width is 0 or double.NaN
                ? previousControl.DesiredSize.Width
                : previousControl.Width;

            Canvas.SetLeft(currentControl, Canvas.GetLeft(previousControl) + previousControlWidth);
            Canvas.SetTop(currentControl, Canvas.GetTop(previousControl));
            currentControl.ZIndex = previousControl.ZIndex;
            previousControl = currentControl;
        }
    }

    private Label CreateUnitLabel(PanelItemValue item, Control valueControl) {
        var label = new Label { DataContext = item };

        label.Bind(ContentProperty, new Binding(nameof(item.UnitSymbol)));
        label.Bind(FontSizeProperty, new Binding(nameof(item.FontSize)));
        label.Bind(FontFamilyProperty, new Binding(nameof(item.FontFamily)));
        label.Bind(ForegroundProperty, new Binding(nameof(item.UnitForegroundBrush)));

        _canvasPanel.Children.Add(label);

        label.Measure(_canvasPanel.Bounds.Size);

        Canvas.SetLeft(label, item.X + valueControl.Width + 5);
        Canvas.SetTop(label, item.Y);

        return label;
    }
}
