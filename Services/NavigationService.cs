using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TabuKA.ViewModels;

namespace TabuKA.Services;

public enum ViewType
{
    MainMenu,
    GameSetup,
    GamePlay,
    Settings,
    Statistics,
    WordManagement
}

public interface INavigationService
{
    event Action<ViewType>? NavigationRequested;
    object? NavigationParameter { get; }
    void NavigateTo(ViewType viewType, object? parameter = null);
}

public class NavigationService : ObservableObject, INavigationService
{
    public event Action<ViewType>? NavigationRequested;

    private ViewType _currentView = ViewType.MainMenu;
    public ViewType CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    private object? _navigationParameter;
    public object? NavigationParameter
    {
        get => _navigationParameter;
        private set => SetProperty(ref _navigationParameter, value);
    }

    public void NavigateTo(ViewType viewType, object? parameter = null)
    {
        CurrentView = viewType;
        NavigationParameter = parameter;
        NavigationRequested?.Invoke(viewType);
    }
}