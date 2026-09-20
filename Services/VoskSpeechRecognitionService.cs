using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Wave;
using Vosk;

namespace TabuKA.Services;

public class VoskSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly IAudioService _audioService;
    private readonly string _modelPath;
    private Model? _model;
    private VoskRecognizer? _recognizer;
    private bool _isListening = false;
    private bool _disposed = false;
    private List<string> _forbiddenWords = new();
    private readonly int _sampleRate = 16000;

    public event Action<string>? SpeechRecognized;
    public event Action<string>? ErrorOccurred;

    public bool IsListening => _isListening;

    public bool IsVoiceClose(float threshold = 0.025f) => _audioService.IsVoiceClose(threshold);

    public VoskSpeechRecognitionService(IAudioService audioService, string? modelPath = null)
    {
        _audioService = audioService;
        _modelPath = ResolveModelPath(modelPath);
        
        _audioService.AudioDataAvailable += OnAudioDataAvailable;
        _audioService.ErrorOccurred += OnAudioError;
    }

    public static string ResolveModelPath(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
            return customPath;

        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-tr"),
            Path.Combine(AppContext.BaseDirectory, "Models", "vosk-model-tr"),
            Path.Combine(Directory.GetCurrentDirectory(), "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Models", "vosk-model-tr"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabuKA", "Models", "vosk-model-tr")
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var full = Path.GetFullPath(candidate);
                if (Directory.Exists(full) && (File.Exists(Path.Combine(full, "final.mdl")) || File.Exists(Path.Combine(full, "am", "final.mdl"))))
                {
                    return full;
                }
            }
            catch
            {
                // Continue candidate search
            }
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-tr");
    }

    public void SetForbiddenWords(IEnumerable<string> words)
    {
        _forbiddenWords.Clear();
        _forbiddenWords.AddRange(words);
    }

    public async Task StartListeningAsync()
    {
        if (_isListening) return;

        if (!await IsAvailableAsync())
        {
            ErrorOccurred?.Invoke("Vosk modeli bulunamadı. Lütfen Türkçe modeli kontrol edin.");
            return;
        }

        try
        {
            _model = new Model(_modelPath);
            RecreateRecognizer();
            _isListening = true;
            
            // Start audio recording through our audio service
            await _audioService.StartRecordingAsync();
        }
        catch (Exception ex)
        {
            _isListening = false;
            ErrorOccurred?.Invoke($"Vosk başlatılamadı: {ex.Message}");
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;

        _isListening = false;
        
        try
        {
            await _audioService.StopRecordingAsync();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Failed to stop recording: {ex.Message}");
        }
        finally
        {
            _recognizer?.Dispose();
            _recognizer = null;
            _model?.Dispose();
            _model = null;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        var resolved = ResolveModelPath(_modelPath);
        if (!Directory.Exists(resolved)) return false;

        var hasModel = File.Exists(Path.Combine(resolved, "final.mdl"))
                    || File.Exists(Path.Combine(resolved, "am", "final.mdl"));

        return await Task.FromResult(hasModel);
    }

    private void RecreateRecognizer()
    {
        _recognizer?.Dispose();
        if (_model != null)
        {
            // Do NOT restrict grammar! Natural continuous Turkish speech recognition requires
            // the full Turkish vocabulary model to transcribe spoken sentences accurately.
            _recognizer = new VoskRecognizer(_model, _sampleRate);
            _recognizer.SetWords(true);
            _recognizer.SetPartialWords(true);
        }
    }

    private void OnAudioDataAvailable(float[] audioData)
    {
        if (!_isListening || _recognizer == null) return;

        try
        {
            // Convert float[] to byte[] (16-bit PCM)
            var byteData = new byte[audioData.Length * 2];
            for (int i = 0; i < audioData.Length; i++)
            {
                short sample = (short)Math.Clamp(audioData[i] * 32767, -32768, 32767);
                byteData[i * 2] = (byte)(sample & 0xFF);
                byteData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            // Process audio through Vosk
            if (_recognizer.AcceptWaveform(byteData, byteData.Length))
            {
                var result = _recognizer.Result();
                using var doc = JsonDocument.Parse(result);
                if (doc.RootElement.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        SpeechRecognized?.Invoke(text);
                    }
                }
            }
            else
            {
                // Partial result for instant taboo detection
                var partialResult = _recognizer.PartialResult();
                using var partialDoc = JsonDocument.Parse(partialResult);
                if (partialDoc.RootElement.TryGetProperty("partial", out var partialTextElement))
                {
                    var partialText = partialTextElement.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(partialText))
                    {
                        SpeechRecognized?.Invoke(partialText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Vosk processing error: {ex.Message}");
        }
    }

    private void OnAudioError(string error)
    {
        ErrorOccurred?.Invoke(error);
    }

    public static async Task<bool> DownloadModelAsync(string modelPath, IProgress<double>? progress = null)
    {
        try
        {
            // Turkish model URL (small model ~50MB)
            var modelUrl = "https://alphacephei.com/vosk/models/vosk-model-small-tr-0.3.zip";
            
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
            
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes > 0 && progress != null;
            
            using var stream = await response.Content.ReadAsStreamAsync();
            var zipPath = Path.Combine(Path.GetDirectoryName(modelPath)!, "vosk-model.zip");
            
            using (var fileStream = File.Create(zipPath))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    
                    if (canReportProgress)
                    {
                        progress?.Report((double)totalRead / totalBytes);
                    }
                }
            }
            
            // Extract zip
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.GetDirectoryName(modelPath)!, true);
            File.Delete(zipPath);
            
            // Rename extracted folder to expected name
            var extractedDirs = Directory.GetDirectories(Path.GetDirectoryName(modelPath)!);
            foreach (var dir in extractedDirs)
            {
                if (dir.Contains("vosk-model"))
                {
                    if (Directory.Exists(modelPath))
                        Directory.Delete(modelPath, true);
                    Directory.Move(dir, modelPath);
                    break;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Model download failed: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ = StopListeningAsync();
            _audioService.AudioDataAvailable -= OnAudioDataAvailable;
            _audioService.ErrorOccurred -= OnAudioError;
            _disposed = true;
        }
    }
}