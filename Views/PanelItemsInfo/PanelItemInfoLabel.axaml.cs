using Avalonia;
using DravusSensorPanel.Models;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public partial class PanelItemInfoLabel : PanelItemInfo {
    public static readonly StyledProperty<PanelItemLabel?> PanelItemProperty =
        AvaloniaProperty.Register<PanelItemInfoLabel, PanelItemLabel?>(nameof(PanelItem));

    public PanelItemLabel? PanelItem {
        get => GetValue(PanelItemProperty);
        set {
            SetValue(PanelItemProperty, value);
            OnPropertyChanged(nameof(PanelItemLabel));
        }
    }

    // Empty constructor to preview works on IDE
    public PanelItemInfoLabel() : this(false) {
    }

    public PanelItemInfoLabel(bool editMode) : base(editMode) {
        InitializeComponent();

        DetachedFromVisualTree += OnDetached;
    }

    public override bool IsValid() {
        if ( PanelItem == null ) {
            return false;
        }

        return PanelItem.Label.Trim().Length > 0;
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) {
        PanelItem?.Dispose();
    }
}
