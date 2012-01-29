using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RxURGViewer
{
    /// <summary>
    /// IEnumerable{Point}型のインスタンスをPointCollection型に変換するコンバータ
    /// </summary>
    public class PointCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            if (typeof(IEnumerable<Point>).IsAssignableFrom(value.GetType()) && targetType == typeof(PointCollection))
            {
                return new PointCollection((IEnumerable<Point>)value);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
