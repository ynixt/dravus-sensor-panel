using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DynamicData;

namespace DravusSensorPanel.Views;

public partial class FontPicker : UserControl {
    public ObservableCollection<FontFamily> Fonts { get; } = new();

    public static readonly StyledProperty<FontFamily> FontSelectedProperty =
        AvaloniaProperty.Register<FontPicker, FontFamily>(nameof(FontSelected));

    public FontFamily FontSelected {
        get => GetValue(FontSelectedProperty)!;
        set => SetValue(FontSelectedProperty, value);
    }

    public FontPicker() {
        LoadAllFonts();
        InitializeComponent();
    }

    private void LoadAllFonts() {
        Fonts.AddRange(FontManager.Current
                                  .SystemFonts
                                  .OrderBy(font => font.Name)
        );
    }
}
