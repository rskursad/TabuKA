using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabuKA.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TabuKA.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDatabaseService _databaseService;
    private readonly INavigationService _navigationService;
    private readonly SpeechRecognitionManager _speechManager;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "System";

    [ObservableProperty]
    public partial int MasterVolume { get; set; } = 80;

    [ObservableProperty]
    public partial bool AutoSave { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowTutorial { get; set; } = true;

    [ObservableProperty]
    public partial string DatabasePath { get; set; } = "tabuka.db";

    [ObservableProperty]
    public partial int WordCount { get; set; }

    [ObservableProperty]
    public partial int CategoryCount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsModelInstalled { get; set; }

    [ObservableProperty]
    public partial string ModelStatusText { get; set; } = "Kontrol ediliyor...";

    [ObservableProperty]
    public partial bool IsDownloadingModel { get; set; }

    [ObservableProperty]
    public partial double ModelDownloadProgress { get; set; }

    [ObservableProperty]
    public partial string ModelDownloadStatusMessage { get; set; } = string.Empty;

    private readonly ISoundEffectService _soundEffectService;

    public List<string> Themes { get; } = new() { "System", "Light", "Dark" };

    public SettingsViewModel(
        ISettingsService settingsService, 
        IDatabaseService databaseService, 
        INavigationService navigationService, 
        SpeechRecognitionManager speechManager,
        ISoundEffectService soundEffectService)
    {
        _settingsService = settingsService;
        _databaseService = databaseService;
        _navigationService = navigationService;
        _speechManager = speechManager;
        _soundEffectService = soundEffectService;

        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        try
        {
            SelectedTheme = await _settingsService.GetThemeAsync();
            MasterVolume = await _settingsService.GetMasterVolumeAsync();
            AutoSave = await _settingsService.GetSettingAsync("AutoSave", true);
            ShowTutorial = await _settingsService.GetSettingAsync("ShowTutorial", true);

            await RefreshModelStatusAsync();

            WordCount = await _databaseService.GetWordCountAsync();
            CategoryCount = await _databaseService.GetCategoryCountAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshModelStatusAsync()
    {
        IsModelInstalled = await _speechManager.IsModelInstalledAsync();
        ModelStatusText = IsModelInstalled ? "✅ Kurulu ve Hazır" : "⚠️ Model Yüklü Değil";
    }

    [RelayCommand]
    public async Task DownloadModelAsync()
    {
        if (IsDownloadingModel) return;

        IsDownloadingModel = true;
        ModelDownloadProgress = 0;
        ModelDownloadStatusMessage = "İndirme başlatılıyor...";
        StatusMessage = "Türkçe ses tanıma modeli indiriliyor...";

        var progress = new Progress<double>(p =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ModelDownloadProgress = p;
            });
        });

        var success = await _speechManager.DownloadAndInstallModelAsync(progress, status =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ModelDownloadStatusMessage = status;
            });
        });

        IsDownloadingModel = false;
        await RefreshModelStatusAsync();
        StatusMessage = success
            ? "Model başarıyla indirildi ve iç kurulum tamamlandı."
            : "Model indirilemedi veya kurulum başarısız oldu.";
    }

    [RelayCommand]
    public async Task DeleteModelAsync()
    {
        if (IsDownloadingModel) return;

        var deleted = await _speechManager.DeleteModelAsync();
        await RefreshModelStatusAsync();
        StatusMessage = deleted
            ? "Ses modeli başarıyla silindi."
            : "Silinecek model dosyası bulunamadı.";
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _ = _settingsService.SetThemeAsync(value);
        ApplyTheme(value);
    }

    partial void OnMasterVolumeChanged(int value)
    {
        _ = _settingsService.SetMasterVolumeAsync(value);
    }

    partial void OnAutoSaveChanged(bool value)
    {
        _ = _settingsService.SetSettingAsync("AutoSave", value);
    }

    partial void OnShowTutorialChanged(bool value)
    {
        _ = _settingsService.SetSettingAsync("ShowTutorial", value);
    }

    private void ApplyTheme(string theme)
    {
        if (Application.Current is App app)
        {
            app.RequestedThemeVariant = theme switch
            {
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default
            };
        }
    }

    [RelayCommand]
    private async Task ExportDatabaseAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Dışa aktarılıyor...";
            
            var fileName = $"tabuka_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            
            await _databaseService.ExportToJsonAsync(path);
            StatusMessage = $"Başarıyla dışa aktarıldı: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void TestSound()
    {
        _soundEffectService.PlayCorrect();
        Task.Delay(350).ContinueWith(_ => _soundEffectService.PlayTaboo());
    }

    [RelayCommand]
    private async Task ImportDatabaseAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "İçe aktarılıyor...";
            
            TopLevel? topLevel = null;
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                topLevel = desktop.MainWindow;
            }

            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "JSON Dosyası Seçin",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
                    }
                });

                if (files.Count > 0)
                {
                    var filePath = files[0].Path.LocalPath;
                    await _databaseService.ImportFromJsonAsync(filePath);
                    await LoadSettingsAsync();
                    StatusMessage = "Başarıyla içe aktarıldı";
                }
                else
                {
                    StatusMessage = "Dosya seçilmedi";
                }
            }
            else
            {
                StatusMessage = "Dosya seçici açılamadı";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetDatabaseAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Veritabanı sıfırlanıyor...";
            
            await _databaseService.ResetAsync();
            await LoadSettingsAsync();
            
            StatusMessage = "Veritabanı varsayılana sıfırlandı";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Yedek alınıyor...";
            
            var fileName = $"tabuka_db_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            
            await _databaseService.BackupAsync(path);
            StatusMessage = $"Yedek alındı: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
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
    private void OpenWordManagement()
    {
        _navigationService.NavigateTo(Services.ViewType.WordManagement);
    }
}