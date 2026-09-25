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

public partial class GameSetupViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    private readonly IGameService _gameService;
    private readonly INavigationService _navigationService;
    private readonly IDbContextFactory<TabuKA.Data.TabuKADbContext> _dbContextFactory;
    private readonly IPermissionService _permissionService;
    private readonly SpeechRecognitionManager _speechManager;

    [ObservableProperty]
    public partial List<GameSettings> AvailableSettings { get; set; } = new();

    [ObservableProperty]
    public partial GameSettings? SelectedSettings { get; set; }

    [ObservableProperty]
    public partial int RoundTimeSeconds { get; set; } = 60;

    [ObservableProperty]
    public partial int PassLimit { get; set; } = 3;

    [ObservableProperty]
    public partial int ScoreToWin { get; set; } = 0;

    [ObservableProperty]
    public partial bool EnableAutoTabooCheck { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPermissionDialogOpen { get; set; } = false;

    [ObservableProperty]
    public partial bool IsModelInstallDialogOpen { get; set; } = false;

    [ObservableProperty]
    public partial bool IsDownloadingModel { get; set; } = false;

    [ObservableProperty]
    public partial double ModelInstallProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string ModelInstallStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PermissionStatusBadge { get; set; } = string.Empty;

    private bool _isVerifyingPermission = false;

    partial void OnEnableAutoTabooCheckChanged(bool value)
    {
        if (_isVerifyingPermission) return;

        if (value)
        {
            _ = HandleEnableAutoTabooCheckAsync();
        }
        else
        {
            ValidationMessage = string.Empty;
            PermissionStatusBadge = string.Empty;
        }
    }

    private async Task HandleEnableAutoTabooCheckAsync()
    {
        var modelInstalled = await _speechManager.IsModelInstalledAsync();
        if (!modelInstalled)
        {
            _isVerifyingPermission = true;
            EnableAutoTabooCheck = false;
            _isVerifyingPermission = false;

            ModelInstallProgress = 0;
            IsDownloadingModel = false;
            ModelInstallStatusMessage = "Ses tanıma modeli kurulumu gerekiyor (~50 MB)";
            IsModelInstallDialogOpen = true;
            return;
        }

        IsPermissionDialogOpen = true;
    }

    [RelayCommand]
    public async Task DownloadAndInstallModelAsync()
    {
        if (IsDownloadingModel) return;

        IsDownloadingModel = true;
        ModelInstallProgress = 0;
        ModelInstallStatusMessage = "İndirme başlatılıyor...";

        var progress = new Progress<double>(p =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ModelInstallProgress = p;
            });
        });

        var success = await _speechManager.DownloadAndInstallModelAsync(progress, status =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ModelInstallStatusMessage = status;
            });
        });

        IsDownloadingModel = false;

        if (success)
        {
            IsModelInstallDialogOpen = false;
            await GrantMicrophonePermissionAsync();
        }
        else
        {
            ValidationMessage = "⚠️ Ses modeli indirilemedi veya kurulum tamamlanamadı. İnternet bağlantınızı kontrol edin.";
        }
    }

    [RelayCommand]
    public void CancelModelInstallDialog()
    {
        if (IsDownloadingModel) return;

        IsModelInstallDialogOpen = false;
        _isVerifyingPermission = true;
        EnableAutoTabooCheck = false;
        _isVerifyingPermission = false;
        PermissionStatusBadge = string.Empty;
    }

    [RelayCommand]
    public async Task GrantMicrophonePermissionAsync()
    {
        IsPermissionDialogOpen = false;
        _isVerifyingPermission = true;

        try
        {
            var granted = await _permissionService.RequestMicrophonePermissionAsync();
            if (granted)
            {
                EnableAutoTabooCheck = true;
                PermissionStatusBadge = "✅ Model Hazır • Mikrofon İzni Onaylandı";
                ValidationMessage = string.Empty;
            }
            else
            {
                EnableAutoTabooCheck = false;
                PermissionStatusBadge = "❌ Mikrofon Bulunamadı";
                ValidationMessage = "⚠️ Kullanılabilir mikrofon aygıtı bulunamadı veya sistem izni engelli.";
            }
        }
        catch (Exception ex)
        {
            EnableAutoTabooCheck = false;
            PermissionStatusBadge = "❌ Hata";
            ValidationMessage = $"Mikrofon kontrol hatası: {ex.Message}";
        }
        finally
        {
            _isVerifyingPermission = false;
        }
    }

    [RelayCommand]
    public void DenyMicrophonePermission()
    {
        _isVerifyingPermission = true;
        EnableAutoTabooCheck = false;
        _isVerifyingPermission = false;

        IsPermissionDialogOpen = false;
        PermissionStatusBadge = "❌ İzin Reddedildi";
        ValidationMessage = "⚠️ Mikrofon izni reddedildi. Sesli tabu tespiti pasife alındı.";
    }

    private bool _isUpdatingCategories = false;

    [ObservableProperty]
    public partial ObservableCollection<CategoryWrapper> Categories { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<Category> SelectedCategories { get; set; } = new();

    [ObservableProperty]
    public partial int TeamCount { get; set; } = 2;

    [ObservableProperty]
    public partial ObservableCollection<TeamSetupItem> Teams { get; set; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    public List<int> TimeOptions { get; } = new() { 30, 45, 60, 90, 120 };
    public List<int> PassOptions { get; } = new() { 1, 2, 3, 5, 0 }; // 0 = sınırsız
    public List<int> ScoreOptions { get; } = new() { 0, 15, 25, 50 }; // 0 = serbest

    public GameSetupViewModel(
        IDatabaseService databaseService, 
        ISettingsService settingsService, 
        IGameService gameService, 
        INavigationService navigationService,
        IDbContextFactory<TabuKA.Data.TabuKADbContext> dbContextFactory,
        IPermissionService permissionService,
        SpeechRecognitionManager speechManager)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
        _gameService = gameService;
        _navigationService = navigationService;
        _dbContextFactory = dbContextFactory;
        _permissionService = permissionService;
        _speechManager = speechManager;

        InitializeTeams();
        _ = LoadDataAsync();
    }

    private void InitializeTeams()
    {
        Teams.Clear();
        for (int i = 1; i <= TeamCount; i++)
        {
            Teams.Add(new TeamSetupItem { TeamNumber = i, Name = $"Takım {i}" });
        }
    }

    partial void OnTeamCountChanged(int value)
    {
        InitializeTeams();
    }

    private void OnCategorySelectionChanged()
    {
        if (_isUpdatingCategories) return;
        UpdateSelectedCategories();
    }

    private void UpdateSelectedCategories()
    {
        SelectedCategories.Clear();
        foreach (var cat in Categories.Where(c => c.IsSelected).Select(c => c.Category))
        {
            SelectedCategories.Add(cat);
        }
        OnPropertyChanged(nameof(SelectedCategories));
    }

    partial void OnSelectedSettingsChanged(GameSettings? value)
    {
        if (value == null) return;

        RoundTimeSeconds = value.RoundTimeSeconds > 0 ? value.RoundTimeSeconds : 60;
        PassLimit = value.PassLimit;
        ScoreToWin = value.ScoreToWin;
        _isVerifyingPermission = true;
        EnableAutoTabooCheck = value.EnableAutoTabooCheck;
        _isVerifyingPermission = false;

        _isUpdatingCategories = true;
        try
        {
            var selectedIds = value.SelectedCategoryIds ?? new List<int>();
            foreach (var wrapper in Categories)
            {
                wrapper.IsSelected = selectedIds.Contains(wrapper.Category.Id);
            }
        }
        finally
        {
            _isUpdatingCategories = false;
        }
        UpdateSelectedCategories();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            AvailableSettings = await GetGameSettingsAsync();
            
            var categories = await _databaseService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categories)
            {
                Categories.Add(new CategoryWrapper(cat, OnCategorySelectionChanged));
            }

            SelectedSettings = AvailableSettings.FirstOrDefault(s => s.IsDefault) ?? AvailableSettings.FirstOrDefault();
            if (SelectedSettings != null)
            {
                OnSelectedSettingsChanged(SelectedSettings);
            }
            else
            {
                UpdateSelectedCategories();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<List<GameSettings>> GetGameSettingsAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.GameSettings.ToListAsync();
    }

    [RelayCommand]
    private void ToggleCategory(CategoryWrapper wrapper)
    {
        if (wrapper == null) return;
        wrapper.IsSelected = !wrapper.IsSelected;
    }

    [RelayCommand]
    private void SelectAllCategories()
    {
        _isUpdatingCategories = true;
        try
        {
            foreach (var wrapper in Categories)
            {
                wrapper.IsSelected = true;
            }
        }
        finally
        {
            _isUpdatingCategories = false;
        }
        UpdateSelectedCategories();
    }

    [RelayCommand]
    private void DeselectAllCategories()
    {
        _isUpdatingCategories = true;
        try
        {
            foreach (var wrapper in Categories)
            {
                wrapper.IsSelected = false;
            }
        }
        finally
        {
            _isUpdatingCategories = false;
        }
        UpdateSelectedCategories();
    }

    [RelayCommand]
    private async Task StartGameAsync()
    {
        ValidationMessage = string.Empty;

        if (EnableAutoTabooCheck)
        {
            var hasPerm = await _permissionService.HasMicrophonePermissionAsync();
            if (!hasPerm)
            {
                EnableAutoTabooCheck = false;
                ValidationMessage = "⚠️ Mikrofon izni bulunamadı. Lütfen izin verin veya sesli tespiti kapatın.";
                return;
            }
        }

        var wordCount = await _databaseService.GetWordCountAsync();
        if (wordCount == 0)
        {
            ValidationMessage = "Kullanılabilir kelime yok. Lütfen kelime ekleyin veya veritabanını başlatın.";
            return;
        }

        var teams = Teams.Select(t => new Team 
        { 
            Name = string.IsNullOrWhiteSpace(t.Name) ? $"Takım {t.TeamNumber}" : t.Name.Trim(), 
            IsActive = true 
        }).ToList();

        // Use or clone settings with customized values
        var settings = SelectedSettings ?? new GameSettings { Name = "Özel Oyun" };
        settings.RoundTimeSeconds = RoundTimeSeconds;
        settings.PassLimit = PassLimit;
        settings.ScoreToWin = ScoreToWin;
        settings.EnableAutoTabooCheck = EnableAutoTabooCheck;
        settings.TeamCount = TeamCount;
        settings.SelectedCategoryIds = SelectedCategories.Select(c => c.Id).ToList();

        try
        {
            await _databaseService.UpdateGameSettingsAsync(settings);
            var gameSession = await _gameService.CreateGameAsync(settings, teams);
            
            _navigationService.NavigateTo(Services.ViewType.GamePlay, gameSession.Id);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Oyun başlatılamadı: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.NavigateTo(Services.ViewType.MainMenu);
    }
}

public partial class CategoryWrapper : ObservableObject
{
    private readonly Action? _onSelectionChanged;

    public Category Category { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        _onSelectionChanged?.Invoke();
    }

    public string Name => Category.Name;
    public string Description => Category.Description ?? "";
    public int SortOrder => Category.SortOrder;

    public CategoryWrapper(Category category, Action? onSelectionChanged = null)
    {
        Category = category;
        _onSelectionChanged = onSelectionChanged;
    }
}

public partial class TeamSetupItem : ObservableObject
{
    [ObservableProperty]
    public partial int TeamNumber { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
}