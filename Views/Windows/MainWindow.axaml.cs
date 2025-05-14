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
using Avalonia.Platform;
using Avalonia.Threading;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DravusSensorPanel.Services.InfoExtractor;
using DynamicData;
using LiveChartsCore.SkiaSharpView.Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using ReactiveUI;
using Control = Avalonia.Controls.Control;
using MouseButton = Avalonia.Input.MouseButton;

namespace DravusSensorPanel.Views.Windows;

public partial class MainWindow : WindowViewModel {
    private readonly IEnumerable<IInfoExtractor>? _infoExtractors;
    private readonly Func<EditPanelWindow>? _editPanelWindowFactory;
    private readonly Func<AboutWindow>? _aboutWindowFactory;
    private readonly Func<PanelItem?, PanelItemFormWindow>? _panelItemFormWindowFactory;
    private readonly UtilService? _utilService;

    private readonly Dictionary<string, Border> _controlsById = new();
    private readonly SensorPanelService? _sensorPanelService;
    private readonly List<Border> _selectedControls = [];

    private Window? _editWindowOpen;
    private IDisposable? _subscriptionDisposable;
    private List<IDisposable>? _windowPropertiesDisposables;
    private PanelItem? _itemDragging;
    private Point? _lastMousePosition;
    private bool _dontCloseApp = false;

    // Empty constructor to preview works on IDE
    public MainWindow() : this(null, null, null, null, null, null) {
    }

    public MainWindow(
        Func<EditPanelWindow>? editPanelWindowFactory,
        SensorPanelService? sensorPanelService,
        IEnumerable<IInfoExtractor>? infoExtractors,
        Func<PanelItem?, PanelItemFormWindow>? panelItemFormWindowFactory,
        UtilService? utilService,
        Func<AboutWindow>? aboutWindowFactory) {
        DataContext = this;

        InitializeComponent();

        _editPanelWindowFactory = editPanelWindowFactory;
        _sensorPanelService = sensorPanelService;
        _infoExtractors = infoExtractors;
        _panelItemFormWindowFactory = panelItemFormWindowFactory;
        _utilService = utilService;
        _aboutWindowFactory = aboutWindowFactory;

        Closed += OnWindowClosed;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        if ( _sensorPanelService != null && _infoExtractors != null ) {
            TrackSensorsValue();

            _sensorPanelService.SensorPanel.Items.CollectionChanged += OnCollectionChanged;

            ChangeWindowPropertiesUsingPanel(_sensorPanelService!.SensorPanel);

            foreach ( PanelItem item in _sensorPanelService.SensorPanel.Items ) {
                item.Reload();

                AddToCanvas(item);
            }

            if ( _sensorPanelService.SensorPanel.Items.Count == 0 ) {
                Dispatcher.UIThread.Post(() => { ShowHelpPopup(); });
            }
        }
    }

    public void CloseWithoutKillApp() {
        _dontCloseApp = true;
        Close();
    }

    private void ChangeWindowPropertiesUsingPanel(SensorPanel sensorPanel) {
        const int throttleSeconds = 500;

        _windowPropertiesDisposables?.ForEach(d => d.Dispose());

        Width = sensorPanel.Width;
        Height = sensorPanel.Height;
        ApplyPanelPosition(sensorPanel, sensorPanel.X, sensorPanel.Y);
        WindowState = sensorPanel.Maximized ? WindowState.Maximized : WindowState.Normal;
        ChangeBar(sensorPanel.HideBar);
        Background = sensorPanel.BackgroundBrush;

        _windowPropertiesDisposables = [
            sensorPanel.WhenAnyValue(sp => sp.BackgroundBrush)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newBackground => { Background = newBackground; }),
            sensorPanel.WhenAnyValue(sp => sp.Width)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newWidth => { Width = newWidth; }),
            sensorPanel.WhenAnyValue(sp => sp.Height)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newHeight => { Height = newHeight; }),
            sensorPanel.WhenAnyValue(sp => sp.X)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newX => { ApplyPanelPosition(sensorPanel, newX, sensorPanel.Y); }),
            sensorPanel.WhenAnyValue(sp => sp.Y)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newY => { ApplyPanelPosition(sensorPanel, sensorPanel.X, newY); }),
            sensorPanel.WhenAnyValue(sp => sp.Display)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(newY => { ApplyPanelPosition(sensorPanel, sensorPanel.X, sensorPanel.Y); }),
            sensorPanel.WhenAnyValue(sp => sp.HideBar)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Subscribe(ChangeBar),
            Observable.FromEventPattern<EventHandler<SizeChangedEventArgs>, SizeChangedEventArgs>(
                          h => SizeChanged += h,
                          h => SizeChanged -= h
                      )
                      .Throttle(TimeSpan.FromMilliseconds(throttleSeconds))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Select(e => e.EventArgs)
                      .Subscribe(OnMainWindowSizeChanged),
            Observable.FromEventPattern<EventHandler<PixelPointEventArgs>, PixelPointEventArgs>(
                          h => PositionChanged += h,
                          h => PositionChanged -= h
                      )
                      .Throttle(TimeSpan.FromMilliseconds(throttleSeconds))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Select(e => e.EventArgs)
                      .Subscribe(OnMainWindowPositionChange),
            this.GetObservable(WindowStateProperty)
                .Throttle(TimeSpan.FromMilliseconds(throttleSeconds))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnMainWindowStateChanged),
        ];
    }

    private void ChangeBar(bool shouldHide) {
        ExtendClientAreaToDecorationsHint = shouldHide;
        ExtendClientAreaChromeHints = shouldHide ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
    }

    private void ApplyPanelPosition(SensorPanel sensorPanel, int x, int y) {
        PixelRect workingArea = sensorPanel.Display.WorkingArea;

        Position = new PixelPoint(workingArea.TopLeft.X + x, workingArea.TopLeft.Y + y);
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

        if ( _dontCloseApp ) return;

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop?.Shutdown();
    }

    private void OnMainWindowSizeChanged(SizeChangedEventArgs e) {
        if ( _sensorPanelService is { SensorPanel.HideBar: false } ) {
            bool changed = false;

            if ( _sensorPanelService.SensorPanel.Width != ( int ) e.NewSize.Width ) {
                _sensorPanelService.SensorPanel.Width = ( int ) e.NewSize.Width;
                changed = true;
            }

            if ( _sensorPanelService.SensorPanel.Height != ( int ) e.NewSize.Height ) {
                _sensorPanelService.SensorPanel.Height = ( int ) e.NewSize.Height;
                changed = true;
            }

            if ( changed ) {
                _sensorPanelService.SavePanel();
            }
        }
    }

    private void OnMainWindowStateChanged(WindowState windowState) {
        if ( _sensorPanelService != null ) {
            bool isMaximized = WindowState == WindowState.Maximized;
            if ( _sensorPanelService.SensorPanel.Maximized != isMaximized ) {
                _sensorPanelService.SensorPanel.Maximized = isMaximized;

                _sensorPanelService.SavePanel();
            }
        }
    }

    private void OnMainWindowPositionChange(PixelPointEventArgs e) {
        if ( _sensorPanelService != null ) {
            bool changed = false;
            SensorPanel? sensorPanel = _sensorPanelService.SensorPanel;

            PixelRect workingArea = sensorPanel.Display.WorkingArea;
            int x = e.Point.X - workingArea.TopLeft.X;
            int y = e.Point.Y - workingArea.TopLeft.Y;

            if ( sensorPanel.X != x ) {
                sensorPanel.X = x;
                changed = true;
            }

            if ( sensorPanel.Y != y ) {
                sensorPanel.Y = y;
                changed = true;
            }

            // TODO: improve this. Currently has a weird behaviour when changing the screen using mouse drag.
            if ( x < 0 || x >= sensorPanel.Display.Bounds.Width || y >= _sensorPanelService.SensorPanel.Display.Bounds.Height ) {
                int displayIndex = Screens.All.IndexOf(sensorPanel.Display);

                if ( x < 0 ) {
                    if ( displayIndex >= 1 ) {
                        sensorPanel.Display = Screens.All[displayIndex - 1];
                    }
                }
                else {
                    Screen? correctDisplay = Screens.All.FirstOrDefault(s => x >= s.Bounds.TopLeft.X && x < s.Bounds.TopRight.X);
                    if ( correctDisplay != null ) {
                        sensorPanel.Display = correctDisplay;
                    }
                }

                _sensorPanelService.ChangeSensorPanelXY(0, 0);
            }

            if ( changed ) {
                _sensorPanelService.SavePanel();
            }
        }
    }

    private void UnselectControls() {
        if ( _selectedControls.Count > 0 ) {
            foreach ( Border selectedControl in _selectedControls ) {
                selectedControl.BorderThickness = new Thickness(0);
            }

            _selectedControls.Clear();
        }
    }

    private void SelectItem(PanelItem item, bool alsoMarkAllOthers = false) {
        Border border = _controlsById[item.Id];

        SelectControl(border, alsoMarkAllOthers);
    }

    private void SelectControl(Border? border, bool alsoMarkAllOthers = false) {
        if ( border != null ) {
            border.BorderThickness = new Thickness(2);
            border.BorderBrush = Brushes.Red;
            _selectedControls.Add(border);
        }

        if ( alsoMarkAllOthers ) {
            foreach ( Border value in _controlsById.Values ) {
                if ( value == border ) continue;

                value.BorderThickness = new Thickness(2);
                value.BorderBrush = Brushes.Yellow;
                _selectedControls.Add(value);
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) {
        if ( _itemDragging == null && e.Key == Key.LeftCtrl ) {
            SelectControl(null, true);
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e) {
        if ( _itemDragging == null && e.Key == Key.LeftCtrl ) {
            UnselectControls();
        }
    }

    private void PanelContainer_OnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if ( _sensorPanelService == null ) return;

        PointerPoint pointerPoint = e.GetCurrentPoint(this);

        if ( pointerPoint.Properties.IsLeftButtonPressed ) {
            if ( e.KeyModifiers == KeyModifiers.None &&
                 _sensorPanelService.SensorPanel is { Maximized: false, HideBar: true } ) {
                BeginMoveDrag(e);
            }
            else if ( e.KeyModifiers == KeyModifiers.Control ) {
                Point p = e.GetPosition(CanvasPanel);
                var hit = CanvasPanel.InputHitTest(p) // devolve IInputElement?
                    as Control;
                if ( hit is not null && hit != CanvasPanel ) {
                    _itemDragging = GetPanelItemFromControlName(hit);

                    if ( _itemDragging != null ) {
                        UnselectControls();
                        SelectItem(_itemDragging, true);
                    }
                }
            }
        }
    }

    private void PanelContainer_OnPointerMoved(object? sender, PointerEventArgs e) {
        PointerPoint currentPoint = e.GetCurrentPoint(this);

        if ( _lastMousePosition != null ) {
            if ( currentPoint.Properties.IsLeftButtonPressed && e.KeyModifiers == KeyModifiers.Control && _itemDragging != null ) {
                Point diff = currentPoint.Position - _lastMousePosition.Value;

                int diffX = ( int ) diff.X;
                int diffY = ( int ) diff.Y;

                if ( diffX != 0 || diffY != 0 ) {
                    _itemDragging.X += diffX;
                    _itemDragging.Y += diffY;
                }
            }
        }

        _lastMousePosition = currentPoint.Position;
    }

    private void PanelContainer_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        UnselectControls();

        if ( _itemDragging != null && _sensorPanelService != null ) {
            _sensorPanelService.SavePanel();
        }

        _itemDragging = null;
        _lastMousePosition = null;

        if ( e.InitialPressMouseButton == MouseButton.Right && sender is Control target ) {
            List<MenuItem> menuItems = [];

            Point p = e.GetPosition(CanvasPanel);
            var hit = CanvasPanel.InputHitTest(p)
                as Control;
            if ( hit is not null && hit != CanvasPanel ) {
                PanelItem? item = GetPanelItemFromControlName(hit);

                if ( item != null ) {
                    SelectItem(item);

                    menuItems.Add(new MenuItem {
                        Header = "Edit Item",
                        Command = ReactiveCommand.Create(() => { OpenEditPanelItem(item); }),
                    });

                    menuItems.Add(new MenuItem {
                        Header = "Remove Item",
                        Command = ReactiveCommand.Create(() => { OpenRemovePanelItem(item); }),
                    });

                    menuItems.Add(new MenuItem {
                        Header = "-",
                    });
                }
            }

            menuItems.Add(new() {
                Header = "Edit Panel",
                Command = ReactiveCommand.Create(() => {
                    UnselectControls();
                    OpenEditPanel();
                }),
            });

            menuItems.Add(new MenuItem {
                Header = "-",
            });

            menuItems.Add(new() {
                Header = "About",
                Command = ReactiveCommand.Create(() => {
                    UnselectControls();

                    AboutWindow? aboutWindow = _aboutWindowFactory?.Invoke();
                    aboutWindow?.ShowDialog(this);
                }),
            });

            menuItems.Add(new() {
                Header = "Help",
                Command = ReactiveCommand.Create(() => {
                    UnselectControls();
                    ShowHelpPopup();
                }),
            });

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
                ( _editWindowOpen as EditPanelWindow )!.MainWindow = this;
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
                UnselectControls();
            }
        }
        else {
            _editWindowOpen.Activate();
        }
    }

    private async void OpenRemovePanelItem(PanelItem item) {
        IMsBox<ButtonResult> confirmationBox = MessageBoxManager
            .GetMessageBoxStandard(
                "Confirmation",
                $"Do you confirm the deletion of {item.Description}?",
                ButtonEnum.YesNo);

        ButtonResult result = await confirmationBox.ShowAsPopupAsync(this);

        if ( result == ButtonResult.Yes ) {
            _sensorPanelService?.RemoveItem(item);
        }
        else {
            UnselectControls();
        }
    }

#region Draw items on canvas

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        if ( e.NewItems != null ) {
            foreach ( PanelItem item in e.NewItems ) {
                AddToCanvas(item);
            }
        }

        if ( e.OldItems != null ) {
            foreach ( PanelItem item in e.OldItems ) {
                if ( _controlsById.TryGetValue(item.Id, out Border? control) ) {
                    CanvasPanel.Children.Remove(control);
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
                Child = stackPanel,
            };

            CanvasPanel.Children.Add(border);

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
                control.Measure(CanvasPanel.Bounds.Size);
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

#endregion

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

    private async void ShowHelpPopup() {
        IMsBox<string>? popup = MessageBoxManager
            .GetMessageBoxCustom(
                new MessageBoxCustomParams {
                    ButtonDefinitions = new List<ButtonDefinition> {
                        new ButtonDefinition { Name = "Ok", IsDefault = true },
                        new ButtonDefinition { Name = "GitHub", },
                    },
                    ContentTitle = "Help",
                    ContentMessage = """
                                     Click with the right mouse button on panel to add/edit/remove new items.
                                     You can also drag items using ctrl.
                                     Needing more help? Go to our github :)
                                     """,
                });

        string choise = await popup.ShowAsPopupAsync(this);

        if ( choise == "GitHub" ) {
            _utilService.OpenGithub(this);
        }
    }
}
