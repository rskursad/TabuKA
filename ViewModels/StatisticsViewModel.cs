using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabuKA.Entities;
using TabuKA.Services;
using Microsoft.EntityFrameworkCore;

namespace TabuKA.ViewModels;

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IGameService _gameService;
    private readonly INavigationService _navigationService;
    private readonly IDbContextFactory<TabuKA.Data.TabuKADbContext> _dbContextFactory;

    [ObservableProperty]
    public partial int TotalGamesPlayed { get; set; }

    [ObservableProperty]
    public partial int TotalRoundsPlayed { get; set; }

    [ObservableProperty]
    public partial int TotalWordsGuessed { get; set; }

    [ObservableProperty]
    public partial int TotalTabooViolations { get; set; }

    [ObservableProperty]
    public partial int TotalPassesUsed { get; set; }

    [ObservableProperty]
    public partial double AverageScorePerGame { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TeamStatItem> TeamStats { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<GameHistoryItem> GameHistory { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<CategoryStatItem> CategoryStats { get; set; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public StatisticsViewModel(
        IDatabaseService databaseService,
        IGameService gameService,
        INavigationService navigationService,
        IDbContextFactory<TabuKA.Data.TabuKADbContext> dbContextFactory)
    {
        _databaseService = databaseService;
        _gameService = gameService;
        _navigationService = navigationService;
        _dbContextFactory = dbContextFactory;

        _ = LoadStatisticsAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        IsLoading = true;
        try
        {
            using var context = _dbContextFactory.CreateDbContext();

            var finishedGames = await context.GameSessions
                .Where(g => g.Status == GameStatus.Finished)
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Team)
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Word)
                        .ThenInclude(w => w.Category)
                .OrderByDescending(g => g.EndedAt)
                .ToListAsync();

            TotalGamesPlayed = finishedGames.Count;
            TotalRoundsPlayed = finishedGames.SelectMany(g => g.Rounds).Count();
            TotalWordsGuessed = finishedGames.SelectMany(g => g.Rounds).Count(r => r.Result == RoundResult.Correct);
            TotalTabooViolations = finishedGames.SelectMany(g => g.Rounds).Count(r => r.Result == RoundResult.Taboo);
            TotalPassesUsed = finishedGames.SelectMany(g => g.Rounds).Count(r => r.Result == RoundResult.Passed);
            AverageScorePerGame = finishedGames.Count > 0 
                ? finishedGames.Average(g => g.Rounds.Sum(r => r.ScoreGained)) 
                : 0;

            var teamStats = finishedGames
                .SelectMany(g => g.Rounds)
                .GroupBy(r => r.TeamId)
                .Select(g => new TeamStatItem
                {
                    TeamName = g.First().Team?.Name ?? $"Takım {g.Key}",
                    GamesPlayed = g.Select(r => r.GameSessionId).Distinct().Count(),
                    TotalScore = g.Sum(r => r.ScoreGained),
                    CorrectAnswers = g.Count(r => r.Result == RoundResult.Correct),
                    TabooViolations = g.Count(r => r.Result == RoundResult.Taboo),
                    PassesUsed = g.Count(r => r.Result == RoundResult.Passed)
                })
                .OrderByDescending(t => t.TotalScore)
                .ToList();

            TeamStats.Clear();
            foreach (var stat in teamStats)
            {
                TeamStats.Add(stat);
            }

            var history = finishedGames.Take(20).Select(g => new GameHistoryItem
            {
                Date = g.EndedAt?.ToString("dd.MM.yyyy HH:mm") ?? "",
                Winner = g.WinnerTeamId != null && int.TryParse(g.WinnerTeamId, out var winnerId) 
                    ? g.Rounds.FirstOrDefault(r => r.TeamId == winnerId)?.Team?.Name ?? $"Takım {winnerId}" 
                    : "Belirsiz",
                TotalRounds = g.Rounds.Count,
                TotalScore = g.Rounds.Sum(r => r.ScoreGained),
                Duration = g.EndedAt.HasValue ? (g.EndedAt.Value - g.StartedAt).ToString(@"mm\:ss") : "--:--"
            }).ToList();

            GameHistory.Clear();
            foreach (var item in history)
            {
                GameHistory.Add(item);
            }

            var categoryStats = finishedGames
                .SelectMany(g => g.Rounds.Where(r => r.Word != null))
                .GroupBy(r => r.Word!.CategoryId)
                .Select(g => new CategoryStatItem
                {
                    CategoryName = g.First().Word?.Category?.Name ?? "Bilinmeyen",
                    TimesPlayed = g.Count(),
                    CorrectGuesses = g.Count(r => r.Result == RoundResult.Correct),
                    TabooCount = g.Count(r => r.Result == RoundResult.Taboo),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(r => r.Result == RoundResult.Correct) / g.Count() * 100 : 0
                })
                .OrderByDescending(c => c.TimesPlayed)
                .ToList();

            CategoryStats.Clear();
            foreach (var stat in categoryStats)
            {
                CategoryStats.Add(stat);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo(Services.ViewType.MainMenu);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadStatisticsAsync();
    }
}

public partial class TeamStatItem : ObservableObject
{
    [ObservableProperty]
    public partial string TeamName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int GamesPlayed { get; set; }

    [ObservableProperty]
    public partial int TotalScore { get; set; }

    [ObservableProperty]
    public partial int CorrectAnswers { get; set; }

    [ObservableProperty]
    public partial int TabooViolations { get; set; }

    [ObservableProperty]
    public partial int PassesUsed { get; set; }
}

public partial class GameHistoryItem : ObservableObject
{
    [ObservableProperty]
    public partial string Date { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Winner { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalRounds { get; set; }

    [ObservableProperty]
    public partial int TotalScore { get; set; }

    [ObservableProperty]
    public partial string Duration { get; set; } = string.Empty;
}

public partial class CategoryStatItem : ObservableObject
{
    [ObservableProperty]
    public partial string CategoryName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TimesPlayed { get; set; }

    [ObservableProperty]
    public partial int CorrectGuesses { get; set; }

    [ObservableProperty]
    public partial int TabooCount { get; set; }

    [ObservableProperty]
    public partial double SuccessRate { get; set; }
}