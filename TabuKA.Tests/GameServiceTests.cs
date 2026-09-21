using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TabuKA.Data;
using TabuKA.Entities;
using TabuKA.Services;
using Xunit;

namespace TabuKA.Tests;

public class GameServiceTests
{
    private sealed class TestContextFactory : IDbContextFactory<TabuKADbContext>
    {
        private readonly DbContextOptions<TabuKADbContext> _options;

        public TestContextFactory(DbContextOptions<TabuKADbContext> options)
        {
            _options = options;
        }

        public TabuKADbContext CreateDbContext() => new(_options);
    }

    private sealed class FakeDatabaseService : IDatabaseService
    {
        private readonly List<Word> _words;

        public FakeDatabaseService(List<Word> words)
        {
            _words = words;
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task<bool> IsDatabaseInitializedAsync() => Task.FromResult(true);
        public Task BackupAsync(string filePath) => Task.CompletedTask;
        public Task RestoreAsync(string filePath) => Task.CompletedTask;
        public Task ResetAsync() => Task.CompletedTask;
        public Task SeedWordsAsync(List<WordImportDto> words) => Task.CompletedTask;
        public Task<int> GetWordCountAsync() => Task.FromResult(_words.Count);
        public Task<int> GetCategoryCountAsync() => Task.FromResult(_words.Select(w => w.CategoryId).Distinct().Count());
        public Task<List<Category>> GetCategoriesAsync() => Task.FromResult(new List<Category>());
        public Task<List<Word>> GetWordsByCategoryAsync(int categoryId) =>
            Task.FromResult(_words.Where(w => w.CategoryId == categoryId).ToList());
        public Task<List<Word>> GetRandomWordsAsync(int count, List<int>? categoryIds = null)
        {
            var query = _words.AsQueryable();
            if (categoryIds is { Count: > 0 })
            {
                query = query.Where(w => categoryIds.Contains(w.CategoryId));
            }
            return Task.FromResult(query.Take(count).ToList());
        }
        public Task<List<Word>> SearchWordsAsync(string? searchText, int? categoryId) => Task.FromResult(_words);
        public Task ExportToJsonAsync(string filePath) => Task.CompletedTask;
        public Task ImportFromJsonAsync(string filePath) => Task.CompletedTask;
        public Task<int> SeedFromSeedDataAsync() => Task.FromResult(0);
        public Task UpdateGameSettingsAsync(GameSettings settings) => Task.CompletedTask;
        public Task AddWordAsync(Word word) => Task.CompletedTask;
        public Task UpdateWordAsync(Word word) => Task.CompletedTask;
        public Task DeleteWordAsync(int wordId) => Task.CompletedTask;
        public Task AddCategoryAsync(string name, string? description) => Task.CompletedTask;
        public Task DeleteCategoryAsync(int categoryId) => Task.CompletedTask;
    }

    private static (IDbContextFactory<TabuKADbContext> factory, IGameService service, GameSettings settings) Setup(
        List<Word>? words = null, int scoreToWin = 0)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TabuKADbContext>()
            .UseSqlite(connection)
            .Options;

        using (var ctx = new TabuKADbContext(options))
        {
            ctx.Database.EnsureCreated();
        }

        var factory = new TestContextFactory(options);
        var settings = new GameSettings
        {
            Name = "Test",
            RoundTimeSeconds = 60,
            PassLimit = 3,
            TeamCount = 0,
            ScoreToWin = scoreToWin,
            EnableAutoTabooCheck = false
        };

        using (var ctx = factory.CreateDbContext())
        {
            ctx.GameSettings.Add(settings);
            ctx.Categories.Add(new Category
            {
                Name = "Test Kategorisi",
                Description = "Test",
                IsActive = true,
                SortOrder = 1
            });
            ctx.SaveChanges();
        }

        var dbService = new FakeDatabaseService(words ?? new List<Word>());
        var service = new GameService(factory, NullLogger<GameService>.Instance, dbService);
        return (factory, service, settings);
    }

    private static List<Word> BuildWords(int count)
    {
        return Enumerable.Range(1, count).Select(i => new Word
        {
            MainWord = $"Kelime{i}",
            ForbiddenWords = new List<string> { "yasak1", "yasak2" },
            CategoryId = 1,
            IsActive = true
        }).ToList();
    }

    [Fact]
    public async Task CreateGameAsync_RegistersTeamsInSessionData()
    {
        var (factory, service, settings) = Setup();
        settings.TeamCount = 2;

        var teams = new List<Team>
        {
            new Team { Name = "Kırmızı", SortOrder = 1 },
            new Team { Name = "Mavi", SortOrder = 2 }
        };

        var session = await service.CreateGameAsync(settings, teams);

        Assert.NotEqual(0, session.Id);
        Assert.Equal(2, session.GameData.TeamIds.Count);

        using var ctx = factory.CreateDbContext();
        var stored = await ctx.GameSessions.Include(g => g.GameSettings).FirstAsync(g => g.Id == session.Id);
        Assert.Equal(2, stored.GameData.TeamIds.Count);
        Assert.Equal(GameStatus.InProgress, stored.Status);
    }

    [Fact]
    public async Task StartRoundAsync_ReturnsWordFromUnusedPool()
    {
        var (_, service, settings) = Setup(BuildWords(5));
        settings.TeamCount = 2;

        var teams = new List<Team>
        {
            new Team { Name = "A", SortOrder = 1 },
            new Team { Name = "B", SortOrder = 2 }
        };

        var session = await service.CreateGameAsync(settings, teams);
        var round = await service.StartRoundAsync(session.Id, session.GameData.TeamIds[0]);

        Assert.NotNull(round);
        Assert.NotNull(round!.Word);

        var reloaded = await service.GetGameSessionAsync(session.Id);
        Assert.Contains(round.Word.Id, reloaded!.GameData.UsedWordIds);
    }

    [Fact]
    public async Task StartRoundAsync_ReturnsNull_WhenNoWordsLeft()
    {
        var (_, service, settings) = Setup();
        settings.TeamCount = 1;

        var teams = new List<Team> { new Team { Name = "Tek", SortOrder = 1 } };
        var session = await service.CreateGameAsync(settings, teams);

        var round = await service.StartRoundAsync(session.Id, session.GameData.TeamIds[0]);
        Assert.Null(round);
    }

    [Fact]
    public async Task SubmitAnswerAsync_Correct_AddsPositiveScore()
    {
        var (_, service, settings) = Setup(BuildWords(5));
        settings.TeamCount = 2;

        var teams = new List<Team> { new Team { Name = "Kırmızı", SortOrder = 1 }, new Team { Name = "Mavi", SortOrder = 2 } };
        var session = await service.CreateGameAsync(settings, teams);
        var teamId = session.GameData.TeamIds[0];

        var round = await service.StartRoundAsync(session.Id, teamId);
        Assert.NotNull(round);

        await service.SubmitAnswerAsync(round!.Id, RoundResult.Correct);

        var result = await service.GetGameSessionAsync(session.Id);
        Assert.NotNull(result);
        Assert.True(result.GameData.TeamScores[teamId] > 0);
    }

    [Fact]
    public async Task SubmitAnswerAsync_AdvancesTeamAndRound()
    {
        var (_, service, settings) = Setup(BuildWords(10));
        settings.TeamCount = 2;

        var teams = new List<Team> { new Team { Name = "A", SortOrder = 1 }, new Team { Name = "B", SortOrder = 2 } };
        var session = await service.CreateGameAsync(settings, teams);
        Assert.NotEmpty(session.GameData.TeamIds);

        var round = await service.StartRoundAsync(session.Id, session.GameData.TeamIds[0]);
        await service.SubmitAnswerAsync(round!.Id, RoundResult.Passed);

        var updated = await service.GetGameSessionAsync(session.Id);
        Assert.Equal(2, updated!.CurrentRound);
        Assert.Equal(1, updated.CurrentTeamIndex);
    }

    [Fact]
    public async Task SubmitAnswerAsync_FinishesGame_WhenScoreToWinReached()
    {
        var (_, service, settings) = Setup(BuildWords(5), scoreToWin: 1);
        settings.TeamCount = 1;
        settings.RoundTimeSeconds = 60;

        var teams = new List<Team> { new Team { Name = "Şampiyon", SortOrder = 1 } };
        var session = await service.CreateGameAsync(settings, teams);

        var round = await service.StartRoundAsync(session.Id, session.GameData.TeamIds[0]);
        round!.TimeUsedSeconds = 0;
        await service.SubmitAnswerAsync(round.Id, RoundResult.Correct);

        var updated = await service.GetGameSessionAsync(session.Id);
        Assert.Equal(GameStatus.Finished, updated!.Status);
        Assert.Equal(session.GameData.TeamIds[0].ToString(), updated.WinnerTeamId);
    }

    [Fact]
    public async Task CheckTabooWordAsync_DetectsForbiddenSubstringCaseInsensitive()
    {
        var (_, service, _) = Setup(BuildWords(1));

        var isTaboo = await service.CheckTabooWordAsync("bugün YASAK1 kelimesini kullandım", new List<string> { "yasak1" });
        Assert.True(isTaboo);

        var clean = await service.CheckTabooWordAsync("normal bir anlatım", new List<string> { "yasak1" });
        Assert.False(clean);
    }

    [Fact]
    public async Task EndGameAsync_DeterminesHighestScoringTeamAsWinner()
    {
        var (factory, service, settings) = Setup(BuildWords(5));
        settings.TeamCount = 2;

        var teams = new List<Team> { new Team { Name = "A", SortOrder = 1 }, new Team { Name = "B", SortOrder = 2 } };
        var session = await service.CreateGameAsync(settings, teams);

        var t1 = session.GameData.TeamIds[0];
        var t2 = session.GameData.TeamIds[1];

        using (var ctx = factory.CreateDbContext())
        {
            var team1 = await ctx.Teams.FindAsync(t1);
            var team2 = await ctx.Teams.FindAsync(t2);
            team1!.Score = 5;
            team2!.Score = 8;
            await ctx.SaveChangesAsync();
        }

        await service.EndGameAsync(session.Id);

        using var checkCtx = factory.CreateDbContext();
        var finished = await checkCtx.GameSessions.FirstAsync(g => g.Id == session.Id);
        Assert.Equal(GameStatus.Finished, finished.Status);
        Assert.Equal(t2.ToString(), finished.WinnerTeamId);
    }
}