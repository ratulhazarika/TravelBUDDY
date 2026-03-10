using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI.Xaml.Data;

namespace EmployeeWeb.Desktop.Converters
{
    public class TimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("HH:mm");
            }

            if (value is DateTimeOffset dto)
            {
                return dto.ToString("HH:mm");
            }

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
