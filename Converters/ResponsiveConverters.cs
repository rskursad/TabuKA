using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using TabuKA.Services;

namespace TabuKA.Converters;

/// <summary>
/// Boyut sınıfına göre farklı değerler seçer.
/// <para>
/// <c>ConverterParameter</c> biçimi: <c>kompakt|orta|geniş</c> (bölmeleri <c>|</c>,
/// sayı bileşenleri <c>,</c>). Eksik bölme soldan sağa devralınır.
/// </para>
/// <list type="bullet">
///   <item><c>FontSize="28"</c> → <c>ConverterParameter="20|26|28"</c></item>
///   <item><c>Padding="24"</c> → <c>ConverterParameter="12,16|20,24|24,28"</c> (Thickness)</item>
///   <item><c>MaxWidth</c> → <c>ConverterParameter="10000|760|820"</c></item>
///   <item><c>Text="Kapalı|Açık|Kapalı"</c> → metin</item>
/// </list>
/// </summary>
public class ResponsiveValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var tokens = SplitToken(parameter);
        if (tokens.Length == 0)
        {
            return value;
        }

        var token = SelectToken(tokens);

        if (targetType == typeof(Thickness))
        {
            return ParseThickness(token);
        }

        if (targetType == typeof(double) || targetType == typeof(float))
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }

            return value;
        }

        if (targetType == typeof(int))
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }

            return value;
        }

        if (targetType is { IsEnum: true })
        {
            try
            {
                return Enum.Parse(targetType, token, ignoreCase: true);
            }
            catch (ArgumentException)
            {
                return value;
            }
        }

        return token;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string[] SplitToken(object? parameter)
    {
        if (parameter is not string s || string.IsNullOrWhiteSpace(s))
        {
            return Array.Empty<string>();
        }

        return s.Split('|', StringSplitOptions.TrimEntries);
    }

    private static string SelectToken(string[] tokens)
    {
        var index = (int)ResponsiveState.Instance.SizeClass;
        return tokens[Math.Min(index, tokens.Length - 1)];
    }

    private static Thickness ParseThickness(string token)    {
        var parts = token.Split(',', StringSplitOptions.TrimEntries);
        var values = new double[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
            {
                return default(Thickness);
            }
        }

        return values.Length switch
        {
            1 => new Thickness(values[0]),
            2 => new Thickness(values[0], values[1], values[0], values[1]),
            4 => new Thickness(values[0], values[1], values[2], values[3]),
            _ => new Thickness(values[0])
        };
    }
}

/// <summary>
/// Boyut sınıfına bağlı <c>bool</c> üretir (<c>IsVisible</c> / <c>IsEnabled</c> için).
/// <para><c>ConverterParameter</c>: <c>compact</c>, <c>medium</c>, <c>expanded</c>,
/// <c>!compact</c>, <c>!medium</c>, <c>!expanded</c> veya <c>compact|medium</c> biçimleri.</para>
/// </summary>
public class ResponsiveVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string raw || string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var negate = raw.StartsWith('!');
        var spec = negate ? raw[1..] : raw;

        var match = false;
        foreach (var part in spec.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (Matches(part))
            {
                match = true;
                break;
            }
        }

        return negate ? !match : match;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static bool Matches(string part) => part.ToLowerInvariant() switch
    {
        "compact" or "mobile" or "phone" => ResponsiveState.Instance.IsCompact,
        "medium" or "tablet" => ResponsiveState.Instance.IsMedium,
        "expanded" or "desktop" or "wide" => ResponsiveState.Instance.IsExpanded,
        "notcompact" => !ResponsiveState.Instance.IsCompact,
        "notmedium" => !ResponsiveState.Instance.IsMedium,
        "notexpanded" => !ResponsiveState.Instance.IsExpanded,
        _ => false
    };
}

/// <summary>
/// Güvenli alan boşluğunu verilen değere ekler (alt navigasyon çubuğu güvenliği için).
/// </summary>
public class SafeAreaBottomConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var baseValue = value is double d ? d : 0d;
        return baseValue + ResponsiveState.Instance.SafeAreaPaddingBottom;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
