using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DravusSensorPanel.Views;

namespace DravusSensorPanel;

public class ViewLocator : IDataTemplate {
    public Control? Build(object? param) {
        if ( param is null ) {
            return null;
        }

        string name = param.GetType().FullName!;
        var type = Type.GetType(name);

        if ( type != null ) {
            return ( Control ) Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) {
        return data is WindowViewModel;
    }
}
