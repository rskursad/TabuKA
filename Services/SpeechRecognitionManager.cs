using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabuKA.Services;

public static class SpeechEngineNames
{
    public const string Vosk = "Vosk (Offline)";
}

/// <summary>
/// Manages speech recognition powered exclusively by the real Vosk offline speech engine.
/// Simulation has been completely removed. Provides model verification, in-app download,
/// and internal installation for voice control.
/// </summary>
public class SpeechRecognitionManager : ISpeechRecognitionService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly VoskSpeechRecognitionService _voskService;
    private bool _disposed;

    public event Action<string>? SpeechRecognized;
    public event Action<string>? ErrorOccurred;

    public bool IsListening => _voskService.IsListening;

    public SpeechRecognitionManager(
        ISettingsService settingsService,
        VoskSpeechRecognitionService voskService)
    {
        _settingsService = settingsService;
        _voskService = voskService;

        _voskService.SpeechRecognized += OnSpeechRecognized;
        _voskService.ErrorOccurred += OnErrorOccurred;
    }

    public async Task<bool> IsModelInstalledAsync()
    {
        return await _voskService.IsAvailableAsync();
    }

    public async Task<bool> DownloadAndInstallModelAsync(IProgress<double>? progress = null, Action<string>? statusCallback = null)
    {
        return await _voskService.DownloadAndInstallModelAsync(progress, statusCallback);
    }

    public async Task<bool> DeleteModelAsync()
    {
        return await _voskService.DeleteModelAsync();
    }

    public string GetInstalledModelPath()
    {
        return VoskSpeechRecognitionService.ResolveModelPath();
    }

    public string GetEngineName()
    {
        return SpeechEngineNames.Vosk;
    }

    public async Task<bool> IsCurrentEngineAvailableAsync()
    {
        return await _voskService.IsAvailableAsync();
    }

    private void OnSpeechRecognized(string text)
    {
        SpeechRecognized?.Invoke(text);
    }

    private void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(error);
    }

    public void SetForbiddenWords(IEnumerable<string> words)
    {
        _voskService.SetForbiddenWords(words);
    }

    public async Task StartListeningAsync()
    {
        await _voskService.StartListeningAsync();
    }

    public async Task StopListeningAsync()
    {
        await _voskService.StopListeningAsync();
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _voskService.IsAvailableAsync();
    }

    public bool IsVoiceClose(float threshold = 0.025f)
    {
        return _voskService.IsVoiceClose(threshold);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _voskService.SpeechRecognized -= OnSpeechRecognized;
        _voskService.ErrorOccurred -= OnErrorOccurred;
        _voskService.Dispose();
        _disposed = true;
    }
}