namespace DravusSensorPanel.Views.PanelItemsInfo;

public abstract class PanelItemInfo : UserControlViewModel {
    public bool EditMode { get; }

    public abstract bool IsValid();

    protected PanelItemInfo() : this(false) {
    }

    protected PanelItemInfo(bool editMode) {
        EditMode = editMode;
    }
}
