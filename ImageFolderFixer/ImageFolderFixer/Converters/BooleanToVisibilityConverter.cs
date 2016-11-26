using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImageFolderFixer.Converters
{
    public class BooleanToVisibilityConverter : ValueConverterMarkupExtension<BooleanToVisibilityConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var Value = (bool)value;
            if (Value)
                return System.Windows.Visibility.Visible;
            else
                return System.Windows.Visibility.Collapsed;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var Value = (System.Windows.Visibility)value;
            return Value == System.Windows.Visibility.Visible;
        }

        #endregion
    }
}