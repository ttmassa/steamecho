using System.Globalization;
using System.Windows.Data;

namespace SteamEcho.App.Converters;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    UltraRare
}

public class AchievementRarityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            if (percentage >= 20)
            {
                return Rarity.Common;
            }
            else if (percentage >= 10)
            {
                return Rarity.Uncommon;
            }
            else if (percentage >= 5)
            {
                return Rarity.Rare;
            }
            else
            {
                return Rarity.UltraRare;
            }
        }
        return Rarity.Common;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}