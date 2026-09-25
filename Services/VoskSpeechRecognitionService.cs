using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Vosk;

namespace TabuKA.Services;

public class VoskSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly IAudioService _audioService;
    private string _modelPath;
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

    public static string GetDefaultInstallPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            return Path.Combine(appData, "TabuKA", "Models", "vosk-model-tr");
        }
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-tr");
    }

    public static bool HasModelFiles(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        return File.Exists(Path.Combine(path, "final.mdl"))
            || File.Exists(Path.Combine(path, "am", "final.mdl"));
    }

    public static string ResolveModelPath(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath) && HasModelFiles(customPath))
            return customPath;

        var candidates = new[]
        {
            GetDefaultInstallPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabuKA", "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-tr"),
            Path.Combine(AppContext.BaseDirectory, "Models", "vosk-model-tr"),
            Path.Combine(Directory.GetCurrentDirectory(), "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "bin", "Debug", "net10.0", "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TabuKA", "bin", "Debug", "net10.0", "Models", "vosk-model-tr"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Models", "vosk-model-tr")
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var full = Path.GetFullPath(candidate);
                if (Directory.Exists(full) && HasModelFiles(full))
                {
                    return full;
                }
            }
            catch
            {
                // Continue candidate search
            }
        }

        return GetDefaultInstallPath();
    }

    public void SetForbiddenWords(IEnumerable<string> words)
    {
        _forbiddenWords.Clear();
        _forbiddenWords.AddRange(words);
    }

    public async Task StartListeningAsync()
    {
        if (_isListening) return;

        _modelPath = ResolveModelPath(_modelPath);
        if (!await IsAvailableAsync())
        {
            ErrorOccurred?.Invoke("Vosk modeli bulunamadı. Lütfen Ayarlar veya Oyun Kurulumu menüsünden modeli indirin.");
            return;
        }

        try
        {
            _model = new Model(_modelPath);
            RecreateRecognizer();
            _isListening = true;

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
            ErrorOccurred?.Invoke($"Kayıt durdurulamadı: {ex.Message}");
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
        _modelPath = ResolveModelPath(_modelPath);
        return await Task.FromResult(HasModelFiles(_modelPath));
    }

    private void RecreateRecognizer()
    {
        _recognizer?.Dispose();
        if (_model != null)
        {
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
            var byteData = new byte[audioData.Length * 2];
            for (int i = 0; i < audioData.Length; i++)
            {
                short sample = (short)Math.Clamp(audioData[i] * 32767, -32768, 32767);
                byteData[i * 2] = (byte)(sample & 0xFF);
                byteData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

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
            ErrorOccurred?.Invoke($"Vosk işleme hatası: {ex.Message}");
        }
    }

    private void OnAudioError(string error)
    {
        ErrorOccurred?.Invoke(error);
    }

    public async Task<bool> DownloadAndInstallModelAsync(IProgress<double>? progress = null, Action<string>? statusCallback = null)
    {
        var target = GetDefaultInstallPath();
        var success = await DownloadAndInstallModelAsync(target, progress, statusCallback);
        if (success)
        {
            _modelPath = ResolveModelPath();
        }
        return success;
    }

    public async Task<bool> DeleteModelAsync()
    {
        var success = await DeleteModelFilesAsync();
        _modelPath = ResolveModelPath();
        return success;
    }

    public static async Task<bool> DownloadAndInstallModelAsync(
        string? targetPath = null,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null)
    {
        var destination = targetPath ?? GetDefaultInstallPath();
        var modelsParentDir = Path.GetDirectoryName(destination)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabuKA", "Models");

        try
        {
            Directory.CreateDirectory(modelsParentDir);
            statusCallback?.Invoke("Model sunucusuna bağlanılıyor...");

            var modelUrl = "https://alphacephei.com/vosk/models/vosk-model-small-tr-0.3.zip";
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            using var response = await httpClient.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 36855784L;
            var tempZip = Path.Combine(modelsParentDir, $"vosk_dl_{Guid.NewGuid():N}.zip");
            var tempExtract = Path.Combine(modelsParentDir, $"vosk_ext_{Guid.NewGuid():N}");

            try
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(tempZip))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        var pct = (double)totalRead / totalBytes;
                        progress?.Report(pct * 80.0); // 0-80% for download
                        var mbRead = totalRead / (1024.0 * 1024.0);
                        var mbTotal = totalBytes / (1024.0 * 1024.0);
                        statusCallback?.Invoke($"İndiriliyor: %{pct * 100:0.0} ({mbRead:0.1} MB / {mbTotal:0.1} MB)");
                    }
                }

                statusCallback?.Invoke("Arşiv açılıyor ve iç kurulum yapılıyor...");
                progress?.Report(85.0);

                if (Directory.Exists(tempExtract))
                    Directory.Delete(tempExtract, true);
                Directory.CreateDirectory(tempExtract);

                await Task.Run(() => ZipFile.ExtractToDirectory(tempZip, tempExtract, true));
                progress?.Report(92.0);

                var foundDir = FindModelDirectory(tempExtract);
                if (foundDir == null)
                {
                    throw new InvalidOperationException("İndirilen arşivde model dosyaları (final.mdl) bulunamadı.");
                }

                statusCallback?.Invoke("Model dosyaları kuruluyor...");
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }
                Directory.CreateDirectory(destination);

                CopyDirectory(foundDir, destination);
                progress?.Report(98.0);

                statusCallback?.Invoke("Model doğrulanıyor...");
                if (!HasModelFiles(destination))
                {
                    throw new InvalidOperationException("Model dosyaları doğrulanamadı.");
                }

                statusCallback?.Invoke("İç kurulum tamamlandı! Model kullanıma hazır.");
                progress?.Report(100.0);
                return true;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
                }
                catch
                {
                    // Ignore temp cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Model install error: {ex.Message}");
            statusCallback?.Invoke($"Kurulum hatası: {ex.Message}");
            return false;
        }
    }

    private static string? FindModelDirectory(string root)
    {
        if (HasModelFiles(root)) return root;

        foreach (var sub in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (HasModelFiles(sub)) return sub;
        }
        return null;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }

    public static async Task<bool> DeleteModelFilesAsync(string? targetPath = null)
    {
        try
        {
            var deletedAny = false;
            var targets = new List<string>();
            if (!string.IsNullOrEmpty(targetPath))
            {
                targets.Add(targetPath);
            }
            targets.Add(GetDefaultInstallPath());
            targets.Add(ResolveModelPath());

            foreach (var path in targets)
            {
                if (Directory.Exists(path) && HasModelFiles(path))
                {
                    await Task.Run(() => Directory.Delete(path, true));
                    deletedAny = true;
                }
            }
            return deletedAny;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete model: {ex.Message}");
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