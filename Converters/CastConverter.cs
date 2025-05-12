using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DravusSensorPanel.Converters;

public class CastConverter<T> : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is T t ? t : default!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value!;
    }
}
