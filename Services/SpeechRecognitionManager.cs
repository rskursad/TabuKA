using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabuKA.Services;

public static class SpeechEngineNames
{
    public const string Simulation = "Simulation";
    public const string Vosk = "Vosk (Offline)";
}

/// <summary>
/// Routes speech recognition to the engine selected in settings
/// (Simulation or Vosk Offline) so the game view can keep using the
/// same ISpeechRecognitionService regardless of the engine.
/// </summary>
public class SpeechRecognitionManager : ISpeechRecognitionService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly SpeechRecognitionService _simulationService;
    private readonly VoskSpeechRecognitionService _voskService;
    private ISpeechRecognitionService _activeService;
    private bool _disposed;

    public event Action<string>? SpeechRecognized;
    public event Action<string>? ErrorOccurred;

    public bool IsListening => _activeService.IsListening;

    public SpeechRecognitionManager(
        ISettingsService settingsService,
        SpeechRecognitionService simulationService,
        VoskSpeechRecognitionService voskService)
    {
        _settingsService = settingsService;
        _simulationService = simulationService;
        _voskService = voskService;

        var engine = _settingsService.GetSettingAsync("SpeechEngine", string.Empty).GetAwaiter().GetResult();
        var voskAvailable = _voskService.IsAvailableAsync().GetAwaiter().GetResult();

        if (string.IsNullOrEmpty(engine))
        {
            engine = voskAvailable ? SpeechEngineNames.Vosk : SpeechEngineNames.Simulation;
        }
        else if (!IsVoskEngine(engine) && voskAvailable)
        {
            // Prefer Vosk offline speech recognition when the model is available
            engine = SpeechEngineNames.Vosk;
        }

        _activeService = IsVoskEngine(engine) ? _voskService : (ISpeechRecognitionService)_simulationService;

        Attach(_activeService);
    }

    public async Task<bool> IsVoskAvailableAsync()
    {
        return await _voskService.IsAvailableAsync();
    }

    private static bool IsVoskEngine(string engine)
    {
        return string.Equals(engine, SpeechEngineNames.Vosk, StringComparison.OrdinalIgnoreCase)
            || string.Equals(engine, "Vosk");
    }

    public async Task SetEngineAsync(string engine)
    {
        if (IsVoskEngine(engine) == ReferenceEquals(_activeService, _voskService))
            return;

        var wasListening = _activeService.IsListening;
        if (wasListening)
        {
            await _activeService.StopListeningAsync();
        }

        Detach(_activeService);

        if (IsVoskEngine(engine))
        {
            _activeService = _voskService;
        }
        else
        {
            _activeService = _simulationService;
        }

        Attach(_activeService);

        if (wasListening)
        {
            await _activeService.StartListeningAsync();
        }
    }

    public string GetEngineName()
    {
        return ReferenceEquals(_activeService, _voskService)
            ? SpeechEngineNames.Vosk
            : SpeechEngineNames.Simulation;
    }

    public async Task<bool> IsCurrentEngineAvailableAsync()
    {
        return await _activeService.IsAvailableAsync();
    }

    private void Attach(ISpeechRecognitionService service)
    {
        service.SpeechRecognized += OnSpeechRecognized;
        service.ErrorOccurred += OnErrorOccurred;
    }

    private void Detach(ISpeechRecognitionService service)
    {
        service.SpeechRecognized -= OnSpeechRecognized;
        service.ErrorOccurred -= OnErrorOccurred;
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
        _activeService.SetForbiddenWords(words);
    }

    public async Task StartListeningAsync()
    {
        await _activeService.StartListeningAsync();
    }

    public async Task StopListeningAsync()
    {
        await _activeService.StopListeningAsync();
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _activeService.IsAvailableAsync();
    }

    public bool IsVoiceClose(float threshold = 0.025f)
    {
        return _activeService.IsVoiceClose(threshold);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Detach(_activeService);
        _simulationService.Dispose();
        _voskService.Dispose();
        _disposed = true;
    }
}