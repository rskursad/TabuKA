using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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

    static App()
    {
        ConfigureNativeLibraryResolver();
    }

    private static void ConfigureNativeLibraryResolver()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            NativeLibrary.SetDllImportResolver(typeof(App).Assembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName == "libvosk")
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var libPath = Path.Combine(baseDir, "libvosk.so");
                    if (File.Exists(libPath))
                    {
                        return NativeLibrary.Load(libPath, assembly, searchPath);
                    }
                }
                return IntPtr.Zero;
            });
        }
    }

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

        // Seed bundled words before the UI is shown so a new game always starts with words ready
        ImportInitialDataAsync(scope.ServiceProvider).GetAwaiter().GetResult();

        try
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var savedTheme = settingsService.GetThemeAsync().GetAwaiter().GetResult();
            RequestedThemeVariant = savedTheme switch
            {
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default
            };
        }
        catch
        {
            // Fallback to default
        }

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
            services.AddSingleton<VoskSpeechRecognitionService>();
            services.AddSingleton<SpeechRecognitionManager>();
            
            // Game views and settings interact with SpeechRecognitionManager
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