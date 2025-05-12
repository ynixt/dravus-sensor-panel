using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace DravusSensorPanel.Views;

public abstract class WindowViewModel : Window, INotifyPropertyChanged {
    public TRet RaiseAndSetIfChanged<TRet>(
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string? propertyName = null) {
        if ( EqualityComparer<TRet>.Default.Equals(backingField, newValue) ) {
            return newValue;
        }

        backingField = newValue;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return newValue;
    }

    protected void OnPropertyChanged(string name) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
}
