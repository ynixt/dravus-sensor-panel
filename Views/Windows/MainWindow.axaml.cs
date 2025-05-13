using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;
using LiveChartsCore.SkiaSharpView.Avalonia;
using ReactiveUI;
using Control = Avalonia.Controls.Control;

namespace DravusSensorPanel.Views.Windows;

public partial class MainWindow : WindowViewModel {
    private readonly IEnumerable<IInfoExtractor>? _infoExtractors;
    private readonly Func<EditPanelWindow>? _editPanelWindowFactory;
    private readonly Func<PanelItem?, PanelItemFormWindow>? _panelItemFormWindowFactory;
    private readonly Dictionary<string, Border> _controlsById = new();

    public bool Collapsed { get; set; } = false;

    private Window? _editWindowOpen;
    private readonly Canvas _canvasPanel;
    private readonly SensorPanelService? _sensorPanelService;
    private IDisposable? _subscriptionDisposable;
    private List<IDisposable>? _windowPropertiesDisposables;

    private Border? _selectedControl;

    // Empty constructor to preview works on IDE
    public MainWindow() : this(null, null, null, null) {
    }

    public MainWindow(
        Func<EditPanelWindow>? editPanelWindowFactory,
        SensorPanelService? sensorPanelService,
        IEnumerable<IInfoExtractor>? infoExtractors,
        Func<PanelItem?, PanelItemFormWindow>? panelItemFormWindowFactory) {
        DataContext = this;

        InitializeComponent();

        _editPanelWindowFactory = editPanelWindowFactory;
        _sensorPanelService = sensorPanelService;
        _infoExtractors = infoExtractors;
        _panelItemFormWindowFactory = panelItemFormWindowFactory;

        _canvasPanel = this.FindControl<Canvas>("CanvasPanel")!;

        Closed += OnWindowClosed;

        if ( _sensorPanelService != null && _infoExtractors != null ) {
            TrackSensorsValue();

            _sensorPanelService.SensorPanel.Items.CollectionChanged += OnCollectionChanged;

            ChangeWindowPropertiesUsingPanel(_sensorPanelService!.SensorPanel);

            foreach ( PanelItem item in _sensorPanelService.SensorPanel.Items ) {
                item.Reload();

                AddToCanvas(item);
            }
        }
    }

    private void ChangeWindowPropertiesUsingPanel(SensorPanel sensorPanel) {
        _windowPropertiesDisposables?.ForEach(d => d.Dispose());
        _windowPropertiesDisposables = [
            sensorPanel.WhenAnyValue(sp => sp.Width).Subscribe(newWidth => { Width = newWidth; }),
            sensorPanel.WhenAnyValue(sp => sp.Height).Subscribe(newHeight => { Height = newHeight; }),
            sensorPanel.WhenAnyValue(sp => sp.X).Subscribe(newX => { Position = new PixelPoint(newX, sensorPanel.Y); }),
            sensorPanel.WhenAnyValue(sp => sp.Y).Subscribe(newY => { Position = new PixelPoint(sensorPanel.Y, newY); }),
        ];

        Width = sensorPanel.Width;
        Height = sensorPanel.Height;
        Position = new PixelPoint(sensorPanel.Y, sensorPanel.Y);
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
        _windowPropertiesDisposables?.ForEach(d => d.Dispose());
        _windowPropertiesDisposables = null;
        _infoExtractors?.ToList().ForEach(i => i.Dispose());

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop?.Shutdown();
    }

    private void UnselectControl() {
        if ( _selectedControl != null ) {
            _selectedControl.BorderThickness = new Thickness(0);
            _selectedControl = null;
        }
    }

    private void PanelContainer_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        UnselectControl();

        if ( e.InitialPressMouseButton == MouseButton.Right && sender is Control target ) {
            List<MenuItem> menuItems = [
                new() {
                    Header = "Edit Panel",
                    Command = ReactiveCommand.Create(OpenEditPanel),
                },
            ];

            Point p = e.GetPosition(_canvasPanel);
            var hit = _canvasPanel.InputHitTest(p) // devolve IInputElement?
                as Control;
            if ( hit is not null && hit != _canvasPanel ) {
                PanelItem? item = GetPanelItemFromControlName(hit);

                if ( item != null ) {
                    Border border = _controlsById[item.Id];

                    border.BorderThickness = new Thickness(2);
                    _selectedControl = border;

                    menuItems.Add(new MenuItem {
                        Header = "Edit Item",
                        Command = ReactiveCommand.Create(() => OpenEditPanelItem(item)),
                    });

                    menuItems.Add(new MenuItem {
                        Header = "Remove Item",
                        Command = ReactiveCommand.Create(() => RemovePanelItem(item)),
                    });
                }
            }

            var menu = new ContextMenu {
                ItemsSource = menuItems,
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

    private async void OpenEditPanelItem(PanelItem selectedItem) {
        if ( _editWindowOpen == null ) {
            _editWindowOpen = _panelItemFormWindowFactory?.Invoke(selectedItem);

            PanelItem originalItem = selectedItem;
            PanelItem clone = selectedItem.Clone();

            if ( _editWindowOpen != null ) {
                var item = await _editWindowOpen.ShowDialog<PanelItem?>(this);

                if ( item != null ) {
                    _sensorPanelService?.EditItem(item, clone);
                }
                else {
                    _sensorPanelService?.RemoveItem(originalItem, false, false);
                    _sensorPanelService?.AddNewItem(clone, false);
                }

                _editWindowOpen = null;
                UnselectControl();
            }
        }
        else {
            _editWindowOpen.Activate();
        }
    }

    private void RemovePanelItem(PanelItem item) {
        _sensorPanelService?.RemoveItem(item);
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        if ( e.NewItems != null ) {
            foreach ( PanelItem item in e.NewItems ) {
                AddToCanvas(item);
            }
        }

        if ( e.OldItems != null ) {
            foreach ( PanelItem item in e.OldItems ) {
                if ( _controlsById.TryGetValue(item.Id, out Border? control) ) {
                    _canvasPanel.Children.Remove(control);
                }

                _controlsById.Remove(item.Id);
            }
        }

        TrackSensorsValue();
    }

    private void AddToCanvas(PanelItem item) {
        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
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
            control.Name = MountNameForControl(item, false);

            var border = new Border {
                DataContext = item,
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Red,
                Child = stackPanel,
            };

            _canvasPanel.Children.Add(border);

            border.Bind(ZIndexProperty, new Binding(nameof(item.ZIndex)));
            border.Bind(Canvas.LeftProperty, new Binding(nameof(item.X)));
            border.Bind(Canvas.TopProperty, new Binding(nameof(item.Y)));


            stackPanel.Children.Add(control);

            if ( item is PanelItemValue itemValue ) {
                stackPanel.Children.Add(CreateUnitLabel(itemValue));

                control.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            }

            _controlsById[item.Id] = border;

            if ( item is IPanelItemHorizontalSizeable ) {
                control.Bind(WidthProperty, new Binding(nameof(IPanelItemHorizontalSizeable.Width)));
            }

            if ( item is IPanelItemVerticalSizeable ) {
                control.Bind(HeightProperty, new Binding(nameof(IPanelItemVerticalSizeable.Height)));
            }

            if ( item is IPanelItemText ) {
                control.InvalidateMeasure();
                control.Measure(_canvasPanel.Bounds.Size);
            }
        }
    }

    private Label CreateUnitLabel(PanelItemValue item) {
        var label = new Label { DataContext = item };

        label.Bind(IsVisibleProperty, new Binding(nameof(item.ShowUnit)));
        label.Bind(ContentProperty, new Binding(nameof(item.UnitSymbol)));
        label.Bind(FontSizeProperty, new Binding(nameof(item.FontSize)));
        label.Bind(FontFamilyProperty, new Binding(nameof(item.FontFamily)));
        label.Bind(ForegroundProperty, new Binding(nameof(item.UnitForegroundBrush)));
        label.Name = MountNameForControl(item, true);

        return label;
    }

    private string MountNameForControl(PanelItem item, bool isForUnitLabel) {
        string name = "$-" + item.Id + "-$" + item.Description;

        if ( isForUnitLabel ) {
            name += "-Unit";
        }

        return name;
    }

    private PanelItem? GetPanelItemFromControlName(Control? control) {
        string? name = GetFirstControlName(control);
        if ( name == null ) return null;

        const string pattern = @"\$-(.*?)-\$";
        Match match = Regex.Match(name, pattern);

        if ( match.Success ) {
            string id = match.Groups[1].Value;

            return _sensorPanelService?.GetItemById(id);
        }

        return null;
    }

    private string? GetFirstControlName(StyledElement? control) {
        if ( control?.Parent == null ) return null;
        if ( control.Name != null ) return control.Name;

        return GetFirstControlName(control.Parent);
    }
}
