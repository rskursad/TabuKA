using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TabuKA.Entities;
using TabuKA.Services;

namespace TabuKA.ViewModels;

public partial class GamePlayViewModel : ViewModelBase, IDisposable
{
    private readonly IGameService _gameService;
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly ISpeechRecognitionService _speechService;
    private readonly ISoundEffectService _soundEffectService;
    private readonly IDbContextFactory<TabuKA.Data.TabuKADbContext> _dbContextFactory;

    [ObservableProperty]
    public partial GameSession? CurrentGame { get; set; }

    [ObservableProperty]
    public partial Round? CurrentRound { get; set; }

    [ObservableProperty]
    public partial Word? CurrentWord { get; set; }

    [ObservableProperty]
    public partial Team? CurrentTeam { get; set; }

    [ObservableProperty]
    public partial int TimeRemaining { get; set; }

    [ObservableProperty]
    public partial bool IsUrgentTime { get; set; }

    [ObservableProperty]
    public partial bool IsRoundActive { get; set; }

    [ObservableProperty]
    public partial bool IsReadyIntermission { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial bool IsTurnSummary { get; set; }

    [ObservableProperty]
    public partial bool IsGameFinished { get; set; }

    [ObservableProperty]
    public partial string GameStatusText { get; set; } = "Oyun hazırlanıyor...";

    [ObservableProperty]
    public partial ObservableCollection<TeamScoreItem> TeamScoreList { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<RoundHistoryItem> RecentRounds { get; set; } = new();

    [ObservableProperty]
    public partial string WinnerText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WinnerDetailsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAutoTabooEnabled { get; set; }

    [ObservableProperty]
    public partial string LastRecognizedSpeech { get; set; } = string.Empty;

    public string SpeechDisplayStatus
    {
        get
        {
            if (!IsAutoTabooEnabled) return "Sesli tespit bu oyun oturumunda devre dışı";
            if (string.IsNullOrWhiteSpace(LastRecognizedSpeech)) return "🎙️ Yakından dinleniyor (Anlatıcı modu - Uzak sesler filtrelenir)";
            return LastRecognizedSpeech;
        }
    }

    partial void OnIsAutoTabooEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(SpeechDisplayStatus));
    }

    partial void OnLastRecognizedSpeechChanged(string value)
    {
        OnPropertyChanged(nameof(SpeechDisplayStatus));
    }

    [ObservableProperty]
    public partial bool TabooDetected { get; set; }

    [ObservableProperty]
    public partial int TurnCorrectCount { get; set; }

    [ObservableProperty]
    public partial int TurnTabooCount { get; set; }

    [ObservableProperty]
    public partial int TurnPassCount { get; set; }

    [ObservableProperty]
    public partial int TurnScoreDelta { get; set; }

    [ObservableProperty]
    public partial string PassStatusText { get; set; } = "Pas: 3 / 3";

    [ObservableProperty]
    public partial bool CanPass { get; set; } = true;

    [ObservableProperty]
    public partial bool IsScoreboardOpen { get; set; } = false;

    [ObservableProperty]
    public partial string TeamScoreSummaryText { get; set; } = string.Empty;

    private DispatcherTimer? _turnTimer;
    private bool _isDisposed;

    public GamePlayViewModel(
        IGameService gameService,
        IDatabaseService databaseService,
        ISettingsService settingsService,
        INavigationService navigationService,
        ISpeechRecognitionService speechService,
        ISoundEffectService soundEffectService,
        IDbContextFactory<TabuKA.Data.TabuKADbContext> dbContextFactory)
    {
        _gameService = gameService;
        _databaseService = databaseService;
        _settingsService = settingsService;
        _navigationService = navigationService;
        _speechService = speechService;
        _soundEffectService = soundEffectService;
        _dbContextFactory = dbContextFactory;

        _navigationService.NavigationRequested += OnNavigationRequested;
        _speechService.SpeechRecognized += OnSpeechRecognized;
        _speechService.ErrorOccurred += OnSpeechError;

        _turnTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _turnTimer.Tick += TurnTimer_Tick;
    }

    private void OnNavigationRequested(Services.ViewType viewType)
    {
        if (viewType == Services.ViewType.GamePlay && _navigationService.NavigationParameter is int gameSessionId)
        {
            _ = InitializeGameAsync(gameSessionId);
        }
        else if (viewType != Services.ViewType.GamePlay)
        {
            StopActiveTurn();
        }
    }

    public async Task InitializeGameAsync(int gameSessionId)
    {
        StopActiveTurn();

        CurrentGame = await _gameService.GetGameSessionAsync(gameSessionId);
        if (CurrentGame == null) return;

        CurrentRound = null;
        CurrentWord = null;
        TabooDetected = false;
        LastRecognizedSpeech = string.Empty;

        TurnCorrectCount = 0;
        TurnTabooCount = 0;
        TurnPassCount = 0;
        TurnScoreDelta = 0;

        WinnerText = string.Empty;
        WinnerDetailsText = string.Empty;

        IsAutoTabooEnabled = CurrentGame.GameSettings.EnableAutoTabooCheck;
        IsGameFinished = false;
        IsPaused = false;
        IsTurnSummary = false;
        IsRoundActive = false;
        IsReadyIntermission = true;
        IsScoreboardOpen = false;

        if (_turnTimer == null)
        {
            _turnTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _turnTimer.Tick += TurnTimer_Tick;
        }

        await SetupCurrentTeamDisplayAsync();
        UpdateTeamScores();
        UpdateRecentRounds();
    }

    public void StopActiveTurn()
    {
        _turnTimer?.Stop();
        _ = StopSpeechRecognitionAsync();
        IsRoundActive = false;
        IsPaused = false;
    }

    [RelayCommand]
    public void ToggleScoreboard()
    {
        IsScoreboardOpen = !IsScoreboardOpen;
    }

    private async Task SetupCurrentTeamDisplayAsync()
    {
        if (CurrentGame == null) return;

        var teams = await GetTeamsAsync();
        if (teams.Count == 0) return;

        var teamIndex = CurrentGame.CurrentTeamIndex % CurrentGame.GameSettings.TeamCount;
        CurrentTeam = teams[teamIndex];

        TimeRemaining = CurrentGame.GameSettings.RoundTimeSeconds;
        IsUrgentTime = false;
        GameStatusText = $"{CurrentTeam.Name} • {CurrentGame.CurrentRound}. Tur";

        UpdatePassStatus();
    }

    private void UpdatePassStatus()
    {
        if (CurrentGame == null || CurrentTeam == null) return;

        var limit = CurrentGame.GameSettings.PassLimit;
        if (limit <= 0)
        {
            PassStatusText = "Pas: Sınırsız";
            CanPass = true;
            return;
        }

        var used = 0;
        if (CurrentGame.GameData.TeamPassUsed.TryGetValue(CurrentTeam.Id, out var count))
        {
            used = count;
        }

        var remaining = Math.Max(0, limit - used);
        PassStatusText = $"Pas: {remaining} / {limit}";
        CanPass = remaining > 0;
    }

    [RelayCommand]
    public async Task StartTurnAsync()
    {
        try
        {
            if (CurrentGame == null || CurrentTeam == null) return;

            IsReadyIntermission = false;
            IsTurnSummary = false;
            IsPaused = false;
            IsRoundActive = true;

            TurnCorrectCount = 0;
            TurnTabooCount = 0;
            TurnPassCount = 0;
            TurnScoreDelta = 0;

            TimeRemaining = CurrentGame.GameSettings.RoundTimeSeconds;
            IsUrgentTime = false;

            await FetchNextWordCardAsync();

            if (IsAutoTabooEnabled)
            {
                try
                {
                    await StartSpeechRecognitionAsync();
                }
                catch (Exception speechEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ses tanıma başlatılamadı: {speechEx.Message}");
                }
            }

            _turnTimer?.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartTurnAsync hatası: {ex}");
            GameStatusText = $"Hata: {ex.Message}";
        }
    }

    private async Task FetchNextWordCardAsync()
    {
        try
        {
            if (CurrentGame == null || CurrentTeam == null) return;

            CurrentRound = await _gameService.StartRoundAsync(CurrentGame.Id, CurrentTeam.Id);
            if (CurrentRound == null)
            {
                // Kelime kalmadı, oyunu bitir
                await EndGameAsync();
                return;
            }

            CurrentWord = CurrentRound.Word;

            if (IsAutoTabooEnabled && CurrentWord != null)
            {
                try
                {
                    _speechService.SetForbiddenWords(CurrentWord.ForbiddenWords);
                }
                catch
                {
                    // Ignore speech word update error
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FetchNextWordCardAsync hatası: {ex}");
            GameStatusText = $"Kelime çekilemedi: {ex.Message}";
        }
    }

    private async void TurnTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsRoundActive || IsPaused) return;

        if (TimeRemaining > 1)
        {
            TimeRemaining--;
            IsUrgentTime = TimeRemaining <= 10;

            if (TimeRemaining <= 5)
            {
                _soundEffectService.PlayTick();
            }
        }
        else
        {
            TimeRemaining = 0;
            IsUrgentTime = false;
            _turnTimer?.Stop();
            _soundEffectService.PlayTimeUp();
            await HandleTurnTimeOutAsync();
        }
    }

    private async Task HandleTurnTimeOutAsync()
    {
        IsRoundActive = false;
        await StopSpeechRecognitionAsync();

        // O an ekrandaki kelimeyi zaman aşımı olarak kaydet (takımı ilerletmeden)
        if (CurrentRound != null)
        {
            await _gameService.SubmitAnswerAsync(CurrentRound.Id, RoundResult.TimeOut, advanceTeam: false);
        }

        CurrentGame = await _gameService.GetGameSessionAsync(CurrentGame!.Id);
        UpdateTeamScores();
        UpdateRecentRounds();

        TurnScoreDelta = TurnCorrectCount - TurnTabooCount;
        IsTurnSummary = true;
    }

    [RelayCommand]
    public async Task ProceedToNextTurnAsync()
    {
        if (CurrentGame == null) return;

        IsTurnSummary = false;

        // Takım sırasını ilerlet
        CurrentGame = await _gameService.NextTurnAsync(CurrentGame.Id);

        // Kazanma şartı veya tur kontrolü
        if (CurrentGame == null || CurrentGame.Status == GameStatus.Finished)
        {
            await EndGameAsync();
            return;
        }

        // Sıradaki takım için ara ekranı hazırla
        await SetupCurrentTeamDisplayAsync();
        IsReadyIntermission = true;
    }

    [RelayCommand]
    public async Task SubmitCorrectAsync()
    {
        if (CurrentRound == null || !IsRoundActive) return;

        _soundEffectService.PlayCorrect();
        TurnCorrectCount++;

        await _gameService.SubmitAnswerAsync(CurrentRound.Id, RoundResult.Correct, advanceTeam: false);
        CurrentGame = await _gameService.GetGameSessionAsync(CurrentGame!.Id);

        UpdateTeamScores();
        UpdateRecentRounds();

        // Hedef skora ulaşıldı mı?
        if (CurrentGame != null && CheckGameWon())
        {
            await EndGameAsync();
            return;
        }

        // Sıradaki kelimeye süre durmadan hemen geç
        await FetchNextWordCardAsync();
    }

    [RelayCommand]
    public async Task SubmitTabooAsync()
    {
        if (CurrentRound == null || !IsRoundActive) return;

        _soundEffectService.PlayTaboo();
        TurnTabooCount++;

        // Görsel TABU uyarısını göster
        TriggerTabooWarning();

        await _gameService.SubmitAnswerAsync(CurrentRound.Id, RoundResult.Taboo, advanceTeam: false);
        CurrentGame = await _gameService.GetGameSessionAsync(CurrentGame!.Id);

        UpdateTeamScores();
        UpdateRecentRounds();

        // Sıradaki kelimeye hemen geç
        await FetchNextWordCardAsync();
    }

    [RelayCommand]
    public async Task SubmitPassAsync()
    {
        if (CurrentRound == null || !IsRoundActive || !CanPass) return;

        _soundEffectService.PlayPass();
        TurnPassCount++;

        await _gameService.SubmitAnswerAsync(CurrentRound.Id, RoundResult.Passed, advanceTeam: false);
        CurrentGame = await _gameService.GetGameSessionAsync(CurrentGame!.Id);

        UpdatePassStatus();
        UpdateTeamScores();
        UpdateRecentRounds();

        // Sıradaki kelimeye hemen geç
        await FetchNextWordCardAsync();
    }

    [RelayCommand]
    public void PauseGame()
    {
        if (!IsRoundActive || IsPaused) return;

        _turnTimer?.Stop();
        IsPaused = true;
        _ = StopSpeechRecognitionAsync();
    }

    [RelayCommand]
    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        _turnTimer?.Start();

        if (IsAutoTabooEnabled)
        {
            _ = StartSpeechRecognitionAsync();
        }
    }

    [RelayCommand]
    public async Task EndGameAsync()
    {
        _turnTimer?.Stop();
        await StopSpeechRecognitionAsync();

        if (CurrentGame != null)
        {
            await _gameService.EndGameAsync(CurrentGame.Id);
            CurrentGame = await _gameService.GetGameSessionAsync(CurrentGame.Id);
        }

        IsRoundActive = false;
        IsReadyIntermission = false;
        IsTurnSummary = false;
        IsPaused = false;
        IsGameFinished = true;

        UpdateTeamScores();
        UpdateWinnerDetails();
    }

    [RelayCommand]
    public void PlayAgain()
    {
        StopActiveTurn();
        _navigationService.NavigateTo(Services.ViewType.GameSetup);
    }

    [RelayCommand]
    public void GoBack()
    {
        StopActiveTurn();
        _navigationService.NavigateTo(Services.ViewType.MainMenu);
    }

    private bool CheckGameWon()
    {
        if (CurrentGame == null) return false;
        var winScore = CurrentGame.GameSettings.ScoreToWin;
        if (winScore <= 0) return false;

        return CurrentGame.GameData.TeamScores.Values.Any(s => s >= winScore);
    }

    private void TriggerTabooWarning()
    {
        TabooDetected = true;
        DispatcherTimer.RunOnce(() =>
        {
            TabooDetected = false;
        }, TimeSpan.FromMilliseconds(700));
    }

    private void UpdateWinnerDetails()
    {
        var teams = GetTeamsAsync().GetAwaiter().GetResult();
        if (teams.Count == 0)
        {
            WinnerText = "Oyun Bitti!";
            return;
        }

        var sorted = teams.OrderByDescending(t => t.Score).ToList();
        var winner = sorted.FirstOrDefault();

        WinnerText = winner != null ? $"🏆 {winner.Name} Kazandı!" : "Oyun Bitti!";
        WinnerDetailsText = string.Join("  •  ", sorted.Select(t => $"{t.Name}: {t.Score} Puan"));
    }

    private void UpdateTeamScores()
    {
        TeamScoreList.Clear();
        if (CurrentGame?.GameData.TeamScores != null)
        {
            var teams = GetTeamsAsync().GetAwaiter().GetResult();
            foreach (var kvp in CurrentGame.GameData.TeamScores.OrderByDescending(x => x.Value))
            {
                var team = teams.FirstOrDefault(t => t.Id == kvp.Key);
                TeamScoreList.Add(new TeamScoreItem
                {
                    TeamName = team?.Name ?? $"Takım {kvp.Key}",
                    Score = kvp.Value
                });
            }
        }
        TeamScoreSummaryText = string.Join("  •  ", TeamScoreList.Select(t => $"{t.TeamName}: {t.Score} P"));
    }

    private void UpdateRecentRounds()
    {
        RecentRounds.Clear();
        if (CurrentGame?.Rounds != null)
        {
            var recent = CurrentGame.Rounds
                .Where(r => r.Result != RoundResult.Pending)
                .OrderByDescending(r => r.Id)
                .Take(6)
                .ToList();

            foreach (var round in recent)
            {
                RecentRounds.Add(new RoundHistoryItem
                {
                    RoundNumber = round.RoundNumber,
                    TeamName = round.Team?.Name ?? $"Takım {round.TeamId}",
                    Word = round.Word?.MainWord ?? "-",
                    ResultText = round.Result.GetDisplayText(),
                    ResultColor = round.Result.GetColor()
                });
            }
        }
    }

    [RelayCommand]
    public void ToggleAutoTaboo()
    {
        // Sesli tabu oyun kuralı olarak kilitlidir, oyun esnasında kapatılamaz
        if (IsAutoTabooEnabled)
        {
            LastRecognizedSpeech = "🔒 Mikrofon oyun boyunca kilitlidir ve kapatılamaz.";
        }
    }

    private async Task StartSpeechRecognitionAsync()
    {
        if (CurrentWord == null) return;

        if (!_speechService.IsEngineSupported)
        {
            LastRecognizedSpeech = "⚠️ Sesli tabu bu cihazda desteklenmiyor.";
            return;
        }

        _speechService.SetForbiddenWords(CurrentWord.ForbiddenWords);

        if (!await _speechService.IsAvailableAsync())
        {
            LastRecognizedSpeech = "⚠️ Vosk modeli bulunamadı";
            return;
        }

        try
        {
            await _speechService.StartListeningAsync();
        }
        catch (Exception ex)
        {
            LastRecognizedSpeech = $"⚠️ Mikrofon hatası: {ex.Message}";
        }
    }

    private async Task StopSpeechRecognitionAsync()
    {
        if (_speechService.IsListening)
        {
            await _speechService.StopListeningAsync();
        }
    }

    private void OnSpeechRecognized(string recognizedText)
    {
        if (string.IsNullOrWhiteSpace(recognizedText)) return;

        Dispatcher.UIThread.Post(async () =>
        {
            if (!IsAutoTabooEnabled || CurrentWord == null || !IsRoundActive)
            {
                LastRecognizedSpeech = $"🗣️ \"{recognizedText}\"";
                return;
            }

            var isTaboo = await _gameService.CheckTabooWordAsync(recognizedText, CurrentWord.ForbiddenWords);
            if (isTaboo)
            {
                // Yakınlık / Mesafe (Proximity) Kontrolü:
                // Sadece mikrofona/telefona yakın olan anlatıcının sesi otomatik tabu sayılır.
                // Uzaktan bağıran rakipler veya ortam sesleri elenir.
                var isClose = _speechService.IsVoiceClose(0.025f);
                if (isClose)
                {
                    LastRecognizedSpeech = $"🚫 Yasak kelime yakından söylendi: \"{recognizedText}\"";
                    await SubmitTabooAsync();
                }
                else
                {
                    LastRecognizedSpeech = $"🗣️ \"{recognizedText}\" (Uzaktan ses - Tabu sayılmadı)";
                }
            }
            else
            {
                LastRecognizedSpeech = $"🗣️ \"{recognizedText}\"";
            }
        });
    }

    private void OnSpeechError(string error)
    {
        Dispatcher.UIThread.Post(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Speech recognition error: {error}");
            LastRecognizedSpeech = $"⚠️ {error}";
        });
    }

    private async Task<List<Team>> GetTeamsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();

        if (CurrentGame?.GameData.TeamIds is { Count: > 0 } teamIds)
        {
            return await context.Teams
                .Where(t => teamIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder)
                .ToListAsync();
        }

        return await context.Teams
            .Where(t => t.GameSessions.Any(gs => gs.Id == CurrentGame!.Id))
            .ToListAsync();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _navigationService.NavigationRequested -= OnNavigationRequested;
        _speechService.SpeechRecognized -= OnSpeechRecognized;
        _speechService.ErrorOccurred -= OnSpeechError;

        _turnTimer?.Stop();
        _turnTimer = null;

        _ = StopSpeechRecognitionAsync();
    }
}

public class TeamScoreItem
{
    public string TeamName { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class RoundHistoryItem
{
    public int RoundNumber { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string ResultText { get; set; } = string.Empty;
    public SolidColorBrush ResultColor { get; set; } = new SolidColorBrush(Colors.Gray);
}