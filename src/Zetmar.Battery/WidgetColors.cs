using System;
using System.Linq;
using System.Windows.Media;
namespace Zetmar.Battery;
internal static class WidgetColors
{
    public static string? Normalize(string? value)
    {
        if (value == null) return null;
        value = value.Trim();
        return value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit) ? value.ToUpperInvariant() : null;
    }
    public static SolidColorBrush? Brush(string? value) => Normalize(value) is {} hex ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)) : null;
    public static System.Windows.Media.Brush Contrasting(Color color)
    {
        static double Linear(byte channel) { double v = channel / 255d; return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        double luminance = .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        return luminance > .179 ? Brushes.Black : Brushes.White;
    }
}
