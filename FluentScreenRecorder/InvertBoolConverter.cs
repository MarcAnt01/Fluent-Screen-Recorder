using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;


namespace FluentScreenRecorder
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Invert(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Invert(value);
        }

        private bool Invert(object value)
        {
            if (value is bool val)
                return !val;

            throw new ArgumentException("The incoming value must be a bool.");
        }
    }
}
