using Avalonia.Media;

namespace TabuKA.Entities;

public static class RoundResultExtensions
{
    public static string GetDisplayText(this RoundResult result)
    {
        return result switch
        {
            RoundResult.Correct => "✅ Doğru",
            RoundResult.Passed => "⏭️ Pas",
            RoundResult.Taboo => "🚫 Tabu",
            RoundResult.TimeOut => "⏰ Süre Doldu",
            RoundResult.Skipped => "⏭️ Atlandı",
            _ => "⏳ Bekliyor"
        };
    }

    public static SolidColorBrush GetColor(this RoundResult result)
    {
        return result switch
        {
            RoundResult.Correct => new SolidColorBrush(Color.Parse("#4CAF50")),
            RoundResult.Passed => new SolidColorBrush(Color.Parse("#FF9800")),
            RoundResult.Taboo => new SolidColorBrush(Color.Parse("#F44336")),
            RoundResult.TimeOut => new SolidColorBrush(Color.Parse("#9E9E9E")),
            RoundResult.Skipped => new SolidColorBrush(Color.Parse("#2196F3")),
            _ => new SolidColorBrush(Color.Parse("#757575"))
        };
    }
}