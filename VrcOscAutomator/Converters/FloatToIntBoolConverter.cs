using System.Globalization;
using System.Windows.Data;

namespace VrcOscAutomator.Converters;

/// <summary>
/// RadioButtonでfloat値をON/OFF選択するコンバーター。
/// ConverterParameterに比較対象のint値 (0 or 1) を渡す。
/// </summary>
[ValueConversion(typeof(float), typeof(bool))]
public sealed class FloatToIntBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f && parameter is string s && int.TryParse(s, out int target))
            return (int)f == target;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string s && int.TryParse(s, out int target))
            return (float)target;
        return Binding.DoNothing;
    }
}
