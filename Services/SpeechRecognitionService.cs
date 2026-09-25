using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabuKA.Services;

/// <summary>
/// Core speech recognition interface used by gameplay and speech management.
/// Simulation has been removed; implementations use the offline Vosk speech recognition engine.
/// </summary>
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