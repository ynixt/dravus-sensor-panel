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

    public PanelItemInfoLabel(bool editMode = false) : base(editMode) {
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
