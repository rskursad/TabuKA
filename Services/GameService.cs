using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabuKA.Entities;
using TabuKA.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TabuKA.Services;

public interface IGameService
{
    Task<GameSession> CreateGameAsync(GameSettings settings, List<Team> teams);
    Task<Round?> StartRoundAsync(int gameSessionId, int teamId);
    Task<RoundResult> SubmitAnswerAsync(int roundId, RoundResult result, bool advanceTeam = true);
    Task<Round?> PassWordAsync(int roundId);
    Task<GameSession?> GetGameSessionAsync(int gameSessionId);
    Task<List<GameSession>> GetGameHistoryAsync(int limit = 10);
    Task<bool> CheckTabooWordAsync(string spokenText, List<string> forbiddenWords);
    Task EndGameAsync(int gameSessionId);
    Task<GameSession?> NextTurnAsync(int gameSessionId);
    Task<bool> CanPassAsync(int gameSessionId, int teamId);
}

public class GameService : IGameService
{
    private readonly IDbContextFactory<TabuKADbContext> _dbContextFactory;
    private readonly ILogger<GameService> _logger;
    private readonly IDatabaseService _databaseService;

    public GameService(IDbContextFactory<TabuKADbContext> dbContextFactory, ILogger<GameService> logger, IDatabaseService databaseService)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task<GameSession> CreateGameAsync(GameSettings settings, List<Team> teams)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var teamCount = teams.Count > 0 ? teams.Count : settings.TeamCount;

        var dbSettings = await context.GameSettings.FirstOrDefaultAsync(s => s.Id == settings.Id);
        if (dbSettings != null)
        {
            dbSettings.TeamCount = teamCount;
            dbSettings.SelectedCategoryIdsJson = settings.SelectedCategoryIdsJson;
            dbSettings.RoundTimeSeconds = settings.RoundTimeSeconds;
            dbSettings.PassLimit = settings.PassLimit;
            dbSettings.ScoreToWin = settings.ScoreToWin;
            dbSettings.EnableAutoTabooCheck = settings.EnableAutoTabooCheck;
            await context.SaveChangesAsync();
        }
        else
        {
            context.GameSettings.Add(settings);
            await context.SaveChangesAsync();
        }

        var gameSession = new GameSession
        {
            GameSettingsId = settings.Id,
            StartedAt = DateTime.UtcNow,
            Status = GameStatus.InProgress,
            CurrentTeamIndex = 0,
            CurrentRound = 1,
            TotalRounds = settings.ScoreToWin > 0 ? settings.ScoreToWin * teamCount : 0
        };

        var gameData = new GameSessionData();

        foreach (var team in teams)
        {
            var gameTeam = new Team
            {
                Name = string.IsNullOrWhiteSpace(team.Name) ? $"Takım {teams.IndexOf(team) + 1}" : team.Name,
                Score = 0,
                IsActive = true,
                SortOrder = team.SortOrder
            };
            context.Teams.Add(gameTeam);
            await context.SaveChangesAsync();

            gameData.TeamIds.Add(gameTeam.Id);
            gameData.TeamScores[gameTeam.Id] = 0;
            gameData.TeamPassUsed[gameTeam.Id] = 0;
        }

        gameSession.GameData = gameData;
        context.GameSessions.Add(gameSession);
        await context.SaveChangesAsync();

        _logger.LogInformation("Yeni oyun oluşturuldu: {GameId}, Takımlar: {TeamCount}", gameSession.Id, gameData.TeamIds.Count);

        return gameSession;
    }

    public async Task<Round?> StartRoundAsync(int gameSessionId, int teamId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var gameSession = await context.GameSessions
            .Include(g => g.GameSettings)
            .FirstOrDefaultAsync(g => g.Id == gameSessionId);

        if (gameSession == null || gameSession.Status != GameStatus.InProgress)
            return null;

        var usedWordIds = gameSession.GameData.UsedWordIds;
        var categoryIds = gameSession.GameSettings.SelectedCategoryIds.Count > 0
            ? gameSession.GameSettings.SelectedCategoryIds
            : null;

        var allAvailable = await _databaseService.GetRandomWordsAsync(
            Math.Max(200, usedWordIds.Count + 50), categoryIds);

        var word = allAvailable.FirstOrDefault(w => !usedWordIds.Contains(w.Id));

        if (word == null)
        {
            _logger.LogWarning("Kullanılabilir kelime kalmadı, oyun bitiriliyor");
            await EndGameAsync(gameSessionId);
            return null;
        }

        var wordEntity = await context.Words
            .Include(w => w.Category)
            .FirstOrDefaultAsync(w => w.Id == word.Id);

        if (wordEntity == null)
        {
            wordEntity = word;
            if (context.Entry(wordEntity).State == EntityState.Detached)
            {
                context.Attach(wordEntity);
            }
        }

        var round = new Round
        {
            GameSessionId = gameSessionId,
            TeamId = teamId,
            WordId = wordEntity.Id,
            Word = wordEntity,
            RoundNumber = gameSession.CurrentRound,
            StartedAt = DateTime.UtcNow,
            Result = RoundResult.Pending
        };

        context.Rounds.Add(round);
        await context.SaveChangesAsync();

        var gameData = gameSession.GameData;
        gameData.CurrentWordId = round.Word.Id;
        gameData.RoundStartTime = DateTime.UtcNow;
        gameData.CurrentTeamId = teamId;
        gameData.UsedWordIds.Add(round.Word.Id);
        gameSession.GameData = gameData;
        await context.SaveChangesAsync();

        _logger.LogInformation("Tur başlatıldı: {RoundId}, Kelime: {Word}", round.Id, round.Word.MainWord);
        return round;
    }

    public async Task<RoundResult> SubmitAnswerAsync(int roundId, RoundResult result, bool advanceTeam = true)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var round = await context.Rounds
            .Include(r => r.GameSession)
                .ThenInclude(g => g.GameSettings)
            .Include(r => r.Team)
            .Include(r => r.Word)
            .FirstOrDefaultAsync(r => r.Id == roundId);

        if (round == null)
            throw new ArgumentException("Tur bulunamadı");

        if (round.Result != RoundResult.Pending)
            return round.Result;

        round.Result = result;
        round.EndedAt = DateTime.UtcNow;
        round.TimeUsedSeconds = (int)(round.EndedAt.Value - round.StartedAt).TotalSeconds;

        var score = CalculateScore(result, round.TimeUsedSeconds, round.GameSession.GameSettings.RoundTimeSeconds);
        round.ScoreGained = score;
        round.Team.Score += score;

        var gameData = round.GameSession.GameData;

        if (result == RoundResult.Passed)
        {
            round.PassUsed = 1;
            round.Team.PassUsed++;
            gameData.TeamPassUsed[round.TeamId] = round.Team.PassUsed;
        }

        gameData.TeamScores[round.TeamId] = round.Team.Score;
        if (!gameData.TeamIds.Contains(round.TeamId))
        {
            gameData.TeamIds.Add(round.TeamId);
        }
        round.GameSession.GameData = gameData;

        if (advanceTeam)
        {
            var teamCount = Math.Max(1, round.GameSession.GameSettings.TeamCount);
            round.GameSession.CurrentTeamIndex = (round.GameSession.CurrentTeamIndex + 1) % teamCount;
            round.GameSession.CurrentRound++;
        }

        var winSettings = round.GameSession.GameSettings.ScoreToWin;
        var isGameWon = winSettings > 0 && round.Team.Score >= winSettings;

        await context.SaveChangesAsync();

        _logger.LogInformation("Tur sonucu: {Result}, Puan: {Score}", result, score);

        if (isGameWon)
        {
            await EndGameAsync(round.GameSessionId);
        }

        return result;
    }

    public async Task<Round?> PassWordAsync(int roundId)
    {
        var result = await SubmitAnswerAsync(roundId, RoundResult.Passed);
        
        using var context = _dbContextFactory.CreateDbContext();
        return await context.Rounds.FirstOrDefaultAsync(r => r.Id == roundId);
    }

    public async Task<GameSession?> GetGameSessionAsync(int gameSessionId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.GameSessions
            .Include(g => g.GameSettings)
            .Include(g => g.Rounds)
                .ThenInclude(r => r.Word)
            .Include(g => g.Rounds)
                .ThenInclude(r => r.Team)
            .FirstOrDefaultAsync(g => g.Id == gameSessionId);
    }

    public async Task<List<GameSession>> GetGameHistoryAsync(int limit = 10)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.GameSessions
            .Where(g => g.Status == GameStatus.Finished)
            .OrderByDescending(g => g.EndedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> CheckTabooWordAsync(string spokenText, List<string> forbiddenWords)
    {
        if (string.IsNullOrWhiteSpace(spokenText) || forbiddenWords == null || forbiddenWords.Count == 0)
            return false;

        var trCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        var spokenLower = spokenText.ToLower(trCulture).Trim();
        var spokenClean = StripDiacritics(spokenLower);

        foreach (var forbidden in forbiddenWords)
        {
            if (string.IsNullOrWhiteSpace(forbidden)) continue;

            var forbiddenLower = forbidden.ToLower(trCulture).Trim();
            var forbiddenClean = StripDiacritics(forbiddenLower);

            // 1. Direct Turkish culture containment (e.g. "kırmızı" in "kırmızı bir araba")
            if (spokenLower.Contains(forbiddenLower, StringComparison.CurrentCultureIgnoreCase))
            {
                _logger.LogInformation("Yasaklı kelime tespit edildi (tr): {Forbidden} in {Spoken}", forbidden, spokenText);
                return await Task.FromResult(true);
            }

            // 2. Diacritic-folded containment (e.g. "kirmizi" matches "kırmızı", "agac" matches "ağaç")
            if (spokenClean.Contains(forbiddenClean, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Yasaklı kelime tespit edildi (folded): {Forbidden} in {Spoken}", forbidden, spokenText);
                return await Task.FromResult(true);
            }

            // 3. Word token stem check (e.g. forbidden word "araba" -> narrator says "arabadan", "arabayı")
            var spokenTokens = spokenClean.Split(new[] { ' ', '.', ',', '!', '?', '-', ';', ':', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in spokenTokens)
            {
                if (token.Equals(forbiddenClean, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Yasaklı kelime tespit edildi (token): {Forbidden} in {Token}", forbidden, token);
                    return await Task.FromResult(true);
                }

                if (forbiddenClean.Length >= 4 && token.StartsWith(forbiddenClean, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Yasaklı kelime kök tespit edildi (stem): {Forbidden} in {Token}", forbidden, token);
                    return await Task.FromResult(true);
                }
            }
        }

        return await Task.FromResult(false);
    }

    private static string StripDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case 'ç': case 'Ç': sb.Append('c'); break;
                case 'ğ': case 'Ğ': sb.Append('g'); break;
                case 'ı': case 'I': case 'İ': sb.Append('i'); break;
                case 'ö': case 'Ö': sb.Append('o'); break;
                case 'ş': case 'Ş': sb.Append('s'); break;
                case 'ü': case 'Ü': sb.Append('u'); break;
                default: sb.Append(char.ToLowerInvariant(ch)); break;
            }
        }
        return sb.ToString();
    }

    public async Task EndGameAsync(int gameSessionId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        var gameSession = await context.GameSessions
            .Include(g => g.GameSettings)
            .FirstOrDefaultAsync(g => g.Id == gameSessionId);
        if (gameSession == null)
            return;

        var gameData = gameSession.GameData;
        var teamIds = gameData.TeamIds.Count > 0
            ? gameData.TeamIds
            : gameSession.Rounds.Select(r => r.TeamId).Distinct().ToList();

        var dbTeams = await context.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToListAsync();

        Team? winner = null;
        if (dbTeams.Count > 0)
        {
            winner = dbTeams.OrderByDescending(t => t.Score).FirstOrDefault();
        }
        else if (gameData.TeamScores.Count > 0)
        {
            var winnerId = gameData.TeamScores.OrderByDescending(kv => kv.Value).First().Key;
            winner = await context.Teams.FirstOrDefaultAsync(t => t.Id == winnerId);
        }

        gameSession.Status = GameStatus.Finished;
        gameSession.EndedAt = DateTime.UtcNow;
        if (winner != null)
        {
            gameSession.WinnerTeamId = winner.Id.ToString();
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Oyun sonlandırıldı: {GameId}, Kazanan: {Winner}", gameSessionId, winner?.Name);
    }

    public async Task<GameSession?> NextTurnAsync(int gameSessionId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var gameSession = await context.GameSessions
            .Include(g => g.GameSettings)
            .FirstOrDefaultAsync(g => g.Id == gameSessionId);

        if (gameSession == null || gameSession.Status != GameStatus.InProgress)
            return null;

        var teamCount = Math.Max(1, gameSession.GameSettings.TeamCount);
        var nextTeamIndex = (gameSession.CurrentTeamIndex + 1) % teamCount;
        gameSession.CurrentTeamIndex = nextTeamIndex;

        if (nextTeamIndex == 0)
        {
            gameSession.CurrentRound++;
        }

        var gameData = gameSession.GameData;
        if (gameData.TeamIds.Count > nextTeamIndex)
        {
            var nextTeamId = gameData.TeamIds[nextTeamIndex];
            gameData.TeamPassUsed[nextTeamId] = 0;
            var team = await context.Teams.FindAsync(nextTeamId);
            if (team != null)
            {
                team.PassUsed = 0;
            }
        }
        gameSession.GameData = gameData;

        await context.SaveChangesAsync();
        _logger.LogInformation("Yeni tur sırası: Oyun {GameId}, Takım İndeksi {TeamIndex}, Tur {Round}", gameSessionId, nextTeamIndex, gameSession.CurrentRound);
        return gameSession;
    }

    public async Task<bool> CanPassAsync(int gameSessionId, int teamId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var gameSession = await context.GameSessions
            .Include(g => g.GameSettings)
            .FirstOrDefaultAsync(g => g.Id == gameSessionId);

        if (gameSession == null) return false;
        var limit = gameSession.GameSettings.PassLimit;
        if (limit <= 0) return true; // Sınırsız pas

        var passUsed = 0;
        if (gameSession.GameData.TeamPassUsed.TryGetValue(teamId, out var used))
        {
            passUsed = used;
        }

        return passUsed < limit;
    }

    private int CalculateScore(RoundResult result, int timeUsed, int maxTime)
    {
        return result switch
        {
            RoundResult.Correct => 1,
            RoundResult.Passed => 0,
            RoundResult.Taboo => -1,
            RoundResult.TimeOut => 0,
            RoundResult.Skipped => 0,
            _ => 0
        };
    }
}