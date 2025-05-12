using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace DravusSensorPanel.Converters;

public class BooleanNegationConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is bool b ? !b : AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is bool b ? !b : AvaloniaProperty.UnsetValue;
    }
}
