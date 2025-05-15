using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DynamicData;

namespace DravusSensorPanel.Views;

public partial class TextAlignmentPicker : UserControl {
    public ObservableCollection<TextAlignment> TextAlignments { get; } = new();

    public static readonly StyledProperty<TextAlignment> TextAlignmentSelectedProperty =
        AvaloniaProperty.Register<TextAlignmentPicker, TextAlignment>(nameof(TextAlignmentSelected));

    public TextAlignment TextAlignmentSelected {
        get => GetValue(TextAlignmentSelectedProperty)!;
        set => SetValue(TextAlignmentSelectedProperty, value);
    }

    public TextAlignmentPicker() {
        LoadAllTextAlignments();
        InitializeComponent();
    }

    private void LoadAllTextAlignments() {
        TextAlignments.AddRange(Enum.GetValues(typeof(TextAlignment)).Cast<TextAlignment>());
    }
}
