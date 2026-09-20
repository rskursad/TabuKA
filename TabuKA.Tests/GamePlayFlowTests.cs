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

public class GamePlayFlowTests
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

    internal static (IDbContextFactory<TabuKADbContext> factory, IGameService service, GameSettings settings) Setup(
        List<Word>? words = null, int scoreToWin = 0, int passLimit = 3)
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
            Name = "Turn Test",
            RoundTimeSeconds = 60,
            PassLimit = passLimit,
            TeamCount = 2,
            ScoreToWin = scoreToWin,
            EnableAutoTabooCheck = false
        };

        using (var ctx = factory.CreateDbContext())
        {
            ctx.GameSettings.Add(settings);
            ctx.Categories.Add(new Category
            {
                Name = "Test Kategori",
                Description = "Test",
                IsActive = true,
                SortOrder = 1
            });
            ctx.SaveChanges();
        }

        var dbService = new FakeDatabaseService(words ?? BuildWords(20));
        var service = new GameService(factory, NullLogger<GameService>.Instance, dbService);
        return (factory, service, settings);
    }

    internal static List<Word> BuildWords(int count)
    {
        return Enumerable.Range(1, count).Select(i => new Word
        {
            MainWord = $"Kelime{i}",
            ForbiddenWords = new List<string> { $"yasak{i}_1", $"yasak{i}_2" },
            CategoryId = 1,
            IsActive = true
        }).ToList();
    }

    [Fact]
    public async Task ConsecutiveWordsInSameTurn_KeepSameTeam()
    {
        var (_, service, settings) = Setup(BuildWords(10));
        var teams = new List<Team>
        {
            new Team { Name = "Takım 1", SortOrder = 1 },
            new Team { Name = "Takım 2", SortOrder = 2 }
        };

        var session = await service.CreateGameAsync(settings, teams);
        var teamId = session.GameData.TeamIds[0];

        // Word 1: Correct
        var r1 = await service.StartRoundAsync(session.Id, teamId);
        Assert.NotNull(r1);
        await service.SubmitAnswerAsync(r1!.Id, RoundResult.Correct, advanceTeam: false);

        // Word 2: Taboo
        var r2 = await service.StartRoundAsync(session.Id, teamId);
        Assert.NotNull(r2);
        await service.SubmitAnswerAsync(r2!.Id, RoundResult.Taboo, advanceTeam: false);

        // Word 3: Correct
        var r3 = await service.StartRoundAsync(session.Id, teamId);
        Assert.NotNull(r3);
        await service.SubmitAnswerAsync(r3!.Id, RoundResult.Correct, advanceTeam: false);

        var updated = await service.GetGameSessionAsync(session.Id);
        Assert.NotNull(updated);

        // Team must still be Takım 1 (index 0) during their turn
        Assert.Equal(0, updated!.CurrentTeamIndex);

        // Score: +1 -1 +1 = +1
        Assert.Equal(1, updated.GameData.TeamScores[teamId]);
    }

    [Fact]
    public async Task NextTurnAsync_AdvancesToNextTeamAndResetsTurnPasses()
    {
        var (_, service, settings) = Setup(BuildWords(10));
        var teams = new List<Team>
        {
            new Team { Name = "Takım A", SortOrder = 1 },
            new Team { Name = "Takım B", SortOrder = 2 }
        };

        var session = await service.CreateGameAsync(settings, teams);
        var t1 = session.GameData.TeamIds[0];

        // Takım A uses 2 passes
        var r1 = await service.StartRoundAsync(session.Id, t1);
        await service.SubmitAnswerAsync(r1!.Id, RoundResult.Passed, advanceTeam: false);

        var r2 = await service.StartRoundAsync(session.Id, t1);
        await service.SubmitAnswerAsync(r2!.Id, RoundResult.Passed, advanceTeam: false);

        // Now turn ends, advance to Takım B
        var nextSession = await service.NextTurnAsync(session.Id);
        Assert.NotNull(nextSession);
        Assert.Equal(1, nextSession!.CurrentTeamIndex);

        // Check if Takım B can pass
        var t2 = nextSession.GameData.TeamIds[1];
        var canPassT2 = await service.CanPassAsync(nextSession.Id, t2);
        Assert.True(canPassT2);
    }

    [Fact]
    public async Task CanPassAsync_ReturnsFalse_WhenLimitExceeded()
    {
        var (_, service, settings) = Setup(BuildWords(10), passLimit: 2);
        var teams = new List<Team>
        {
            new Team { Name = "Takım X", SortOrder = 1 }
        };

        var session = await service.CreateGameAsync(settings, teams);
        var t1 = session.GameData.TeamIds[0];

        // First pass
        var r1 = await service.StartRoundAsync(session.Id, t1);
        await service.SubmitAnswerAsync(r1!.Id, RoundResult.Passed, advanceTeam: false);
        Assert.True(await service.CanPassAsync(session.Id, t1));

        // Second pass (limit reached: 2 / 2)
        var r2 = await service.StartRoundAsync(session.Id, t1);
        await service.SubmitAnswerAsync(r2!.Id, RoundResult.Passed, advanceTeam: false);

        // Third pass should now be denied
        Assert.False(await service.CanPassAsync(session.Id, t1));
    }

    [Fact]
    public async Task CreateGameAsync_NewGameAfterFinishedGame_IsFreshAndInProgress()
    {
        var (_, service, settings) = Setup(BuildWords(10), scoreToWin: 1);
        var teams1 = new List<Team>
        {
            new Team { Name = "Takım 1", SortOrder = 1 },
            new Team { Name = "Takım 2", SortOrder = 2 }
        };

        // Game 1: finish it
        var session1 = await service.CreateGameAsync(settings, teams1);
        await service.EndGameAsync(session1.Id);
        var endedSession1 = await service.GetGameSessionAsync(session1.Id);
        Assert.NotNull(endedSession1);
        Assert.Equal(GameStatus.Finished, endedSession1!.Status);

        // Game 2: Start new game
        var teams2 = new List<Team>
        {
            new Team { Name = "Takım A", SortOrder = 1 },
            new Team { Name = "Takım B", SortOrder = 2 }
        };
        var session2 = await service.CreateGameAsync(settings, teams2);
        var freshSession2 = await service.GetGameSessionAsync(session2.Id);

        Assert.NotNull(freshSession2);
        Assert.Equal(GameStatus.InProgress, freshSession2!.Status);
        Assert.NotEqual(session1.Id, session2.Id);
        Assert.Equal(0, freshSession2.CurrentTeamIndex);
        Assert.Equal(1, freshSession2.CurrentRound);
    }

    [Fact]
    public async Task CreateGameAsync_PreservesEnableAutoTabooCheck_WhenTrue()
    {
        var (_, service, settings) = Setup(BuildWords(10));
        settings.EnableAutoTabooCheck = true;

        var teams = new List<Team>
        {
            new Team { Name = "Takım 1", SortOrder = 1 },
            new Team { Name = "Takım 2", SortOrder = 2 }
        };

        var session = await service.CreateGameAsync(settings, teams);
        var fetched = await service.GetGameSessionAsync(session.Id);

        Assert.NotNull(fetched);
        Assert.True(fetched!.GameSettings.EnableAutoTabooCheck);
    }
}
