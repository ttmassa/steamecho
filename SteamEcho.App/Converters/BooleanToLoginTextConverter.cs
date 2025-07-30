using System.Globalization;
using System.Windows.Data;

namespace SteamEcho.App.Converters;

public class BooleanToLoginTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isLoggedIn = value is bool b && b;
        return isLoggedIn ? "Logout from your Steam account: " : "Login to your Steam account: ";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
