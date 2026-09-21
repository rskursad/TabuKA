using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabuKA.Services;

public interface ISpeechRecognitionService
{
    event Action<string>? SpeechRecognized;
    event Action<string>? ErrorOccurred;
    
    bool IsListening { get; }
    Task StartListeningAsync();
    Task StopListeningAsync();
    Task<bool> IsAvailableAsync();
    void SetForbiddenWords(IEnumerable<string> words);
    bool IsVoiceClose(float threshold = 0.025f);
}

public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly IAudioService _audioService;
    private readonly List<string> _forbiddenWords = new();
    private bool _isListening = false;
    private bool _disposed = false;
    private System.Timers.Timer? _simulationTimer;

    public bool IsVoiceClose(float threshold = 0.025f) => _audioService.IsVoiceClose(threshold);
    private readonly Random _random = new();

    public event Action<string>? SpeechRecognized;
    public event Action<string>? ErrorOccurred;

    public bool IsListening => _isListening;

    public SpeechRecognitionService(IAudioService audioService)
    {
        _audioService = audioService;
        _audioService.AudioDataAvailable += OnAudioDataAvailable;
        _audioService.ErrorOccurred += OnAudioError;
    }

    public void SetForbiddenWords(IEnumerable<string> words)
    {
        _forbiddenWords.Clear();
        _forbiddenWords.AddRange(words);
    }

    public async Task StartListeningAsync()
    {
        if (_isListening) return;

        var available = await IsAvailableAsync();
        if (!available)
        {
            ErrorOccurred?.Invoke("Speech recognition not available on this platform");
            return;
        }

        _isListening = true;
        await _audioService.StartRecordingAsync();
        
        // For demo: simulate speech recognition every few seconds
        _simulationTimer = new System.Timers.Timer(3000);
        _simulationTimer.Elapsed += (s, e) => SimulateRecognition();
        _simulationTimer.Start();
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;

        _isListening = false;
        _simulationTimer?.Stop();
        _simulationTimer?.Dispose();
        _simulationTimer = null;
        
        await _audioService.StopRecordingAsync();
    }

    public async Task<bool> IsAvailableAsync()
    {
        // Check if microphone is available
        var devices = await _audioService.GetAvailableInputDevicesAsync();
        return devices.Count > 0;
    }

    private void OnAudioDataAvailable(float[] audioData)
    {
        // In a real implementation, this would send audio to speech recognition engine
        // For now, we just monitor audio levels
        var maxAmplitude = 0f;
        foreach (var sample in audioData)
        {
            var abs = Math.Abs(sample);
            if (abs > maxAmplitude) maxAmplitude = abs;
        }
        
        // Could trigger voice activity detection here
    }

    private void OnAudioError(string error)
    {
        ErrorOccurred?.Invoke(error);
    }

    private void SimulateRecognition()
    {
        if (!_isListening) return;

        // Simulate recognized speech - in reality this would come from speech engine
        var testPhrases = new[]
        {
            "bu bir test kelimesidir",
            "kelime anlatma oyunu",
            "tabu kelimesi yasak",
            "doğru cevap verildi",
            "geçmek istiyorum",
            "süre doldu"
        };

        var phrase = testPhrases[_random.Next(testPhrases.Length)];
        SpeechRecognized?.Invoke(phrase);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _audioService.AudioDataAvailable -= OnAudioDataAvailable;
            _audioService.ErrorOccurred -= OnAudioError;
            _simulationTimer?.Stop();
            _simulationTimer?.Dispose();
            _disposed = true;
        }
    }
}