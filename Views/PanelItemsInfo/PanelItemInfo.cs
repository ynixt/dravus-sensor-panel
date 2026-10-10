using System.Collections.Generic;
using DravusSensorPanel.Models;

namespace DravusSensorPanel.Views.PanelItemsInfo;

public abstract class PanelItemInfo : UserControlViewModel {
    public bool EditMode { get; }
    public IReadOnlyList<string> CaseStyles => PanelItemTextTransform.CaseStyles;

    public abstract bool IsValid();

    protected PanelItemInfo() : this(false) {
    }

    protected PanelItemInfo(bool editMode) {
        EditMode = editMode;
    }

    protected bool IsRegexPatternValid(string? regexPattern) {
        return PanelItemTextTransform.IsValidRegexPattern(regexPattern);
    }
}
