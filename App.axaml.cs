using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TabuKA.Data;
using TabuKA.Services;
using TabuKA.ViewModels;
using TabuKA.Views;

namespace TabuKA;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = ConfigureServices();
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        dbService.InitializeAsync().GetAwaiter().GetResult();
        
        // Import initial word data if database is empty
        _ = Task.Run(async () =>
        {
            await ImportInitialDataAsync(scope.ServiceProvider);
        });
        
        // Download Vosk model if not present
        _ = Task.Run(async () =>
        {
            await EnsureVoskModelAsync(scope.ServiceProvider);
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static string GetDatabaseConnectionString()
    {
        var localDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tabuka.db");
        if (File.Exists(localDb))
        {
            return $"Data Source={localDb}";
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            var dbDir = Path.Combine(appData, "TabuKA");
            try
            {
                Directory.CreateDirectory(dbDir);
                return $"Data Source={Path.Combine(dbDir, "tabuka.db")}";
            }
            catch
            {
                // Fallback to working directory
            }
        }

        return "Data Source=tabuka.db";
    }

    private static async Task ImportInitialDataAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var dbService = serviceProvider.GetRequiredService<IDatabaseService>();
            var wordCount = await dbService.GetWordCountAsync();

            if (wordCount == 0)
            {
                await dbService.SeedFromSeedDataAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initial data import failed: {ex.Message}");
        }
    }

    private static async Task EnsureVoskModelAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-tr");
            var hasModel = File.Exists(Path.Combine(modelPath, "final.mdl"))
                        || File.Exists(Path.Combine(modelPath, "am", "final.mdl"));
            
            if (!Directory.Exists(modelPath) || !hasModel)
            {
                System.Diagnostics.Debug.WriteLine("Vosk Turkish model not found. Downloading...");
                
                var speechService = serviceProvider.GetService<VoskSpeechRecognitionService>();
                if (speechService != null)
                {
                    var progress = new Progress<double>(p => 
                        System.Diagnostics.Debug.WriteLine($"Model download: {p:P0}"));
                    
                    await VoskSpeechRecognitionService.DownloadModelAsync(modelPath, progress);
                    System.Diagnostics.Debug.WriteLine("Vosk model downloaded successfully");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vosk model ensure failed: {ex.Message}");
        }
    }

    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        var connectionString = GetDatabaseConnectionString();
        services.AddDbContextFactory<TabuKADbContext>(options =>
            options.UseSqlite(connectionString));

            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IGameService, GameService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IAudioService, AudioService>();
            services.AddSingleton<ISoundEffectService, SoundEffectService>();
            services.AddSingleton<IPermissionService, DesktopPermissionService>();
            
            services.AddSingleton<AudioService>();
            services.AddSingleton<SpeechRecognitionService>();
            services.AddSingleton<VoskSpeechRecognitionService>();
            services.AddSingleton<SpeechRecognitionManager>();
            
            // Game views always talk to the manager, which routes to the engine selected in settings
            services.AddSingleton<ISpeechRecognitionService>(sp => sp.GetRequiredService<SpeechRecognitionManager>());

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<GameSetupViewModel>();
            services.AddSingleton<GamePlayViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<StatisticsViewModel>();
            services.AddSingleton<WordManagementViewModel>();

            return services;
        }
}