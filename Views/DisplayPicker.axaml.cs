using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using DynamicData;

namespace DravusSensorPanel.Views;

public partial class DisplayPicker : UserControl {
    public ObservableCollection<Screen> Displays { get; } = new();

    public static readonly StyledProperty<Screen> DisplaySelectedProperty =
        AvaloniaProperty.Register<DisplayPicker, Screen>(nameof(DisplaySelected));

    public Screen DisplaySelected {
        get => GetValue(DisplaySelectedProperty)!;
        set => SetValue(DisplaySelectedProperty, value);
    }

    public DisplayPicker() {
        LoadAllDisplays();
        InitializeComponent();
    }

    private void LoadAllDisplays() {
        Window window = (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!
            .MainWindow!;

        Displays.AddRange(window.Screens.All);
    }
}
