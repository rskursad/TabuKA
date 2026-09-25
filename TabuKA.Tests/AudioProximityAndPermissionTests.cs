using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TabuKA.Services;
using Xunit;

namespace TabuKA.Tests;

public class AudioProximityAndPermissionTests
{
    [Fact]
    public void IsVoiceClose_HighAmplitude_ReturnsTrue()
    {
        using var audioService = new AudioService();

        // Simulate close speaking (narrator close to mic, high amplitude ~0.15)
        var closeSamples = new float[1600]; // 100ms
        for (int i = 0; i < closeSamples.Length; i++)
        {
            closeSamples[i] = (float)(0.15 * Math.Sin(2 * Math.PI * 440 * i / 16000));
        }

        audioService.ProcessSamplesForTesting(closeSamples);

        Assert.True(audioService.IsVoiceClose(0.025f));
        Assert.True(audioService.RecentPeakRms > 0.025f);
    }

    [Fact]
    public void IsVoiceClose_LowAmplitude_ReturnsFalse()
    {
        using var audioService = new AudioService();

        // Simulate distant speech / background room chatter (low amplitude ~0.008)
        var distantSamples = new float[1600];
        for (int i = 0; i < distantSamples.Length; i++)
        {
            distantSamples[i] = (float)(0.008 * Math.Sin(2 * Math.PI * 440 * i / 16000));
        }

        audioService.ProcessSamplesForTesting(distantSamples);

        Assert.False(audioService.IsVoiceClose(0.025f));
        Assert.True(audioService.RecentPeakRms < 0.025f);
    }

    [Fact]
    public void ResetRecentLevels_ClearsEnergyLevels()
    {
        using var audioService = new AudioService();

        var samples = new float[1600];
        Array.Fill(samples, 0.5f);
        audioService.ProcessSamplesForTesting(samples);

        Assert.True(audioService.IsVoiceClose(0.025f));

        audioService.ResetRecentLevels();

        Assert.Equal(0f, audioService.CurrentRmsLevel);
        Assert.Equal(0f, audioService.RecentPeakRms);
        Assert.False(audioService.IsVoiceClose(0.025f));
    }

    [Fact]
    public async Task DesktopPermissionService_WhenCustomHandlerRegistered_UsesCustomHandler()
    {
        using var audioService = new AudioService();
        var service = new DesktopPermissionService(audioService);

        try
        {
            PermissionProvider.CustomPermissionHandler = () => Task.FromResult(false);
            var result = await service.RequestMicrophonePermissionAsync();
            Assert.False(result);

            PermissionProvider.CustomPermissionHandler = () => Task.FromResult(true);
            result = await service.RequestMicrophonePermissionAsync();
            Assert.True(result);
        }
        finally
        {
            PermissionProvider.CustomPermissionHandler = null;
        }
    }

    [Fact]
    public async Task AudioService_StartRecording_FiresDataAvailable()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var audio = new AudioService();
        var devices = await audio.GetAvailableInputDevicesAsync();
        if (devices.Count == 0) return;

        bool received = false;
        audio.AudioDataAvailable += data => { if (data.Length > 0) received = true; };
        await audio.StartRecordingAsync();
        await Task.Delay(800);
        await audio.StopRecordingAsync();
        Assert.True(received, "AudioDataAvailable was never called by WaveIn!");
    }

    [Theory]
    [InlineData("kırmızı bir elma", "KIRMIZI", true)]
    [InlineData("kirmizi bir elma", "KIRMIZI", true)]
    [InlineData("odada ışık yanıyor", "IŞIK", true)]
    [InlineData("arabadan indik", "ARABA", true)]
    [InlineData("yeşil bir ağaçta oturuyor", "AĞAÇ", true)]
    [InlineData("tatlı bir pasta", "TUZLU", false)]
    public async Task CheckTabooWordAsync_AdvancedTurkishMatching_WorksAccurately(string spoken, string forbiddenWord, bool expected)
    {
        var (_, gameService, _) = GamePlayFlowTests.Setup(GamePlayFlowTests.BuildWords(5));
        var result = await gameService.CheckTabooWordAsync(spoken, new List<string> { forbiddenWord });
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task VoskSpeechRecognitionService_IsAvailable_ReturnsTrue()
    {
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio);

        var isAvailable = await vosk.IsAvailableAsync();
        Assert.True(isAvailable, "Vosk Turkish model was not found in candidate paths!");
    }

    internal sealed class FakeSettingsService : ISettingsService
    {
        public Task<T> GetSettingAsync<T>(string key, T defaultValue = default!) => Task.FromResult(defaultValue);
        public Task SetSettingAsync<T>(string key, T value) => Task.CompletedTask;
        public Task<TabuKA.Entities.AppSettings?> GetAppSettingAsync(string key) => Task.FromResult<TabuKA.Entities.AppSettings?>(null);
        public Task<List<TabuKA.Entities.AppSettings>> GetAllSettingsAsync() => Task.FromResult(new List<TabuKA.Entities.AppSettings>());
        public Task ResetToDefaultsAsync() => Task.CompletedTask;
        public Task<string> GetThemeAsync() => Task.FromResult("System");
        public Task SetThemeAsync(string theme) => Task.CompletedTask;
        public Task<int> GetMasterVolumeAsync() => Task.FromResult(80);
        public Task SetMasterVolumeAsync(int volume) => Task.CompletedTask;
    }

    [Fact]
    public void SpeechRecognitionManager_GetEngineName_ReturnsVoskOnly()
    {
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio);
        var settings = new FakeSettingsService();
        using var manager = new SpeechRecognitionManager(settings, vosk);

        Assert.Equal(SpeechEngineNames.Vosk, manager.GetEngineName());
    }

    [Fact]
    public void VoskSpeechRecognitionService_GetDefaultInstallPath_ReturnsValidPath()
    {
        var path = VoskSpeechRecognitionService.GetDefaultInstallPath();
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.Contains("vosk-model-tr", path);
    }

    [Fact]
    public void VoskSpeechRecognitionService_HasModelFiles_FalseForNonExistentDirectory()
    {
        var nonExistent = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.False(VoskSpeechRecognitionService.HasModelFiles(nonExistent));
    }

    [Fact]
    public async Task GameSetupViewModel_WhenModelNotInstalled_PreventsEnablingAndOpensInstallDialog()
    {
        var nonExistentPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio, nonExistentPath);
        var settings = new FakeSettingsService();
        using var manager = new SpeechRecognitionManager(settings, vosk);

        var (contextFactory, gameService, _) = GamePlayFlowTests.Setup(GamePlayFlowTests.BuildWords(5));
        var db = new DatabaseService(contextFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseService>.Instance);
        var perm = new DesktopPermissionService(audio);

        var vm = new TabuKA.ViewModels.GameSetupViewModel(
            db, settings, gameService, new NavigationService(), contextFactory, perm, manager);

        // When model is not available, turning on voice control triggers model install dialog and keeps toggle off
        vm.EnableAutoTabooCheck = true;
        await Task.Delay(100);

        // If model is not installed, it opens the install dialog and keeps EnableAutoTabooCheck false
        if (!await manager.IsModelInstalledAsync())
        {
            Assert.False(vm.EnableAutoTabooCheck);
            Assert.True(vm.IsModelInstallDialogOpen);

            vm.CancelModelInstallDialog();
            Assert.False(vm.IsModelInstallDialogOpen);
            Assert.False(vm.EnableAutoTabooCheck);
        }
    }
}
