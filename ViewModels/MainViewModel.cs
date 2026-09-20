using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabuKA.Services;

namespace TabuKA.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    private readonly IGameService _gameService;
    private readonly INavigationService _navigationService;
    private readonly GameSetupViewModel _gameSetupViewModel;
    private readonly GamePlayViewModel _gamePlayViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly StatisticsViewModel _statisticsViewModel;
    private readonly WordManagementViewModel _wordManagementViewModel;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "TabuKA - Kelime Anlatma Oyunu";

    [ObservableProperty]
    public partial int WordCount { get; set; }

    [ObservableProperty]
    public partial int CategoryCount { get; set; }

    [ObservableProperty]
    public partial bool IsDatabaseReady { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; set; }

    public MainViewModel(
        IDatabaseService databaseService, 
        ISettingsService settingsService, 
        IGameService gameService, 
        INavigationService navigationService,
        GameSetupViewModel gameSetupViewModel,
        GamePlayViewModel gamePlayViewModel,
        SettingsViewModel settingsViewModel,
        StatisticsViewModel statisticsViewModel,
        WordManagementViewModel wordManagementViewModel)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
        _gameService = gameService;
        _navigationService = navigationService;
        _gameSetupViewModel = gameSetupViewModel;
        _gamePlayViewModel = gamePlayViewModel;
        _settingsViewModel = settingsViewModel;
        _statisticsViewModel = statisticsViewModel;
        _wordManagementViewModel = wordManagementViewModel;

        _navigationService.NavigationRequested += OnNavigationRequested;
        CurrentViewModel = this;
        
        _ = LoadStatsAsync();
    }

    private void OnNavigationRequested(Services.ViewType viewType)
    {
        CurrentViewModel = viewType switch
        {
            Services.ViewType.MainMenu => this,
            Services.ViewType.GameSetup => _gameSetupViewModel,
            Services.ViewType.GamePlay => _gamePlayViewModel,
            Services.ViewType.Settings => _settingsViewModel,
            Services.ViewType.Statistics => _statisticsViewModel,
            Services.ViewType.WordManagement => _wordManagementViewModel,
            _ => this
        };
    }

    [RelayCommand]
    private async Task LoadStatsAsync()
    {
        try
        {
            IsDatabaseReady = await _databaseService.IsDatabaseInitializedAsync();
            if (IsDatabaseReady)
            {
                WordCount = await _databaseService.GetWordCountAsync();
                CategoryCount = await _databaseService.GetCategoryCountAsync();
            }
        }
        catch
        {
            IsDatabaseReady = false;
        }
    }

    [RelayCommand]
    private void NavigateToGameSetup()
    {
        _navigationService.NavigateTo(Services.ViewType.GameSetup);
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo(Services.ViewType.Settings);
    }

    [RelayCommand]
    private void NavigateToStatistics()
    {
        _navigationService.NavigateTo(Services.ViewType.Statistics);
    }

    [RelayCommand]
    private void NavigateToWordManagement()
    {
        _navigationService.NavigateTo(Services.ViewType.WordManagement);
    }
}