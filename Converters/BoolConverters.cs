using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Media;
using TabuKA.ViewModels;
using TabuKA.Entities;

namespace TabuKA.Converters;

public class BoolToCategoryBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = Avalonia.Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        if (isDark)
        {
            return isSelected 
                ? new SolidColorBrush(Color.Parse("#381A4C")) 
                : new SolidColorBrush(Color.Parse("#1A162F"));
        }
        return isSelected 
            ? new SolidColorBrush(Color.Parse("#F3E8FF")) 
            : new SolidColorBrush(Color.Parse("#FFFFFF"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToCategoryBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = value is true;
        bool isDark = Avalonia.Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        if (isDark)
        {
            return isSelected 
                ? new SolidColorBrush(Color.Parse("#C084FC")) 
                : new SolidColorBrush(Color.Parse("#2F2752"));
        }
        return isSelected 
            ? new SolidColorBrush(Color.Parse("#A855F7")) 
            : new SolidColorBrush(Color.Parse("#D4D8EE"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "🎙️ Açık (Kilitli)" : "⏸️ Kapalı";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true 
            ? new SolidColorBrush(Color.Parse("#34D399")) 
            : new SolidColorBrush(Color.Parse("#9CA3AF"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RoundResultToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            RoundResult.Correct => "✅ Doğru",
            RoundResult.Passed => "⏭️ Pas",
            RoundResult.Taboo => "🚫 Tabu",
            RoundResult.TimeOut => "⏰ Süre Doldu",
            RoundResult.Skipped => "⏭️ Atlandı",
            _ => "⏳ Bekliyor"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RoundResultToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            RoundResult.Correct => new SolidColorBrush(Color.Parse("#4CAF50")),
            RoundResult.Passed => new SolidColorBrush(Color.Parse("#FF9800")),
            RoundResult.Taboo => new SolidColorBrush(Color.Parse("#F44336")),
            RoundResult.TimeOut => new SolidColorBrush(Color.Parse("#9E9E9E")),
            RoundResult.Skipped => new SolidColorBrush(Color.Parse("#2196F3")),
            _ => new SolidColorBrush(Color.Parse("#757575"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.ToString(@"mm\:ss");
        }
        if (value is TimeSpan ts2)
        {
            return ts2.ToString(@"mm\:ss");
        }
        return "00:00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class GameStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            GameStatus.NotStarted => "Henüz başlamadı",
            GameStatus.InProgress => "Devam ediyor",
            GameStatus.Paused => "Duraklatıldı",
            GameStatus.Finished => "Tamamlandı",
            GameStatus.Cancelled => "İptal edildi",
            _ => "Bilinmiyor"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RoundNumberConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GamePlayViewModel vm)
        {
            var currentRound = vm.CurrentGame?.CurrentRound ?? 1;
            var totalRounds = vm.CurrentGame?.GameSettings.TeamCount * (vm.CurrentGame?.GameSettings.ScoreToWin > 0 ? vm.CurrentGame.GameSettings.ScoreToWin : 10) ?? 10;
            return $"Tur: {currentRound} / {totalRounds}";
        }
        return "Tur: 1 / 10";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var message = value as string ?? "";
        if (message.Contains("Hata") || message.Contains("Başarısız"))
            return new SolidColorBrush(Color.Parse("#FFEBEE"));
        if (message.Contains("Başarıyla") || message.Contains("Tamamlandı"))
            return new SolidColorBrush(Color.Parse("#E8F5E9"));
        if (message.Contains("Yedek") || message.Contains("Dışa") || message.Contains("İçe"))
            return new SolidColorBrush(Color.Parse("#E3F2FD"));
        return new SolidColorBrush(Color.Parse("#FFF3E0"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class StatusToBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var message = value as string ?? "";
        if (message.Contains("Hata") || message.Contains("Başarısız"))
            return new SolidColorBrush(Color.Parse("#EF5350"));
        if (message.Contains("Başarıyla") || message.Contains("Tamamlandı"))
            return new SolidColorBrush(Color.Parse("#66BB6A"));
        if (message.Contains("Yedek") || message.Contains("Dışa") || message.Contains("İçe"))
            return new SolidColorBrush(Color.Parse("#42A5F5"));
        return new SolidColorBrush(Color.Parse("#FFA726"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ModelStatusBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isInstalled = value is true;
        return isInstalled
            ? new SolidColorBrush(Color.Parse("#1A10B981"))
            : new SolidColorBrush(Color.Parse("#1AF59E0B"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ModelStatusBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isInstalled = value is true;
        return isInstalled
            ? new SolidColorBrush(Color.Parse("#10B981"))
            : new SolidColorBrush(Color.Parse("#F59E0B"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ModelStatusFgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isInstalled = value is true;
        return isInstalled
            ? new SolidColorBrush(Color.Parse("#10B981"))
            : new SolidColorBrush(Color.Parse("#F59E0B"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}