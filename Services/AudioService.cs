using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if WINDOWS
using NAudio.Wave;
#endif

#pragma warning disable CS0067, CS0414

namespace TabuKA.Services;

public interface IAudioService
{
    event Action<float[]>? AudioDataAvailable;
    event Action<string>? ErrorOccurred;
    
    bool IsRecording { get; }
    float CurrentRmsLevel { get; }
    float RecentPeakRms { get; }
    bool IsVoiceClose(float threshold = 0.025f);
    void ResetRecentLevels();
    void ProcessSamplesForTesting(float[] samples);

    Task StartRecordingAsync();
    Task StopRecordingAsync();
    Task<List<string>> GetAvailableInputDevicesAsync();
    void SetInputDevice(int deviceNumber);
}

public class AudioService : IAudioService, IDisposable
{
#if WINDOWS
    private WaveIn? _waveIn;
#endif
    private readonly int _sampleRate = 16000;
    private readonly int _channels = 1;
    private int _selectedDeviceNumber = 0;
    private bool _isRecording = false;
    private bool _disposed = false;

    private readonly object _energyLock = new();
    private readonly Queue<float> _recentRmsHistory = new();

    public float CurrentRmsLevel { get; private set; }
    public float RecentPeakRms { get; private set; }

    public event Action<float[]>? AudioDataAvailable;
    public event Action<string>? ErrorOccurred;

    public bool IsRecording => _isRecording;

    public bool IsVoiceClose(float threshold = 0.025f)
    {
        lock (_energyLock)
        {
            return RecentPeakRms >= threshold;
        }
    }

    public void ResetRecentLevels()
    {
        lock (_energyLock)
        {
            _recentRmsHistory.Clear();
            CurrentRmsLevel = 0;
            RecentPeakRms = 0;
        }
    }

    public void ProcessSamplesForTesting(float[] samples)
    {
        UpdateEnergyLevels(samples);
        AudioDataAvailable?.Invoke(samples);
    }

    private void UpdateEnergyLevels(float[] floatData)
    {
        if (floatData.Length == 0) return;

        double sumSquares = 0;
        for (int i = 0; i < floatData.Length; i++)
        {
            sumSquares += floatData[i] * floatData[i];
        }

        float bufferRms = (float)Math.Sqrt(sumSquares / floatData.Length);

        lock (_energyLock)
        {
            CurrentRmsLevel = bufferRms;
            _recentRmsHistory.Enqueue(bufferRms);
            while (_recentRmsHistory.Count > 20)
            {
                _recentRmsHistory.Dequeue();
            }
            RecentPeakRms = _recentRmsHistory.Count > 0 ? _recentRmsHistory.Max() : 0f;
        }
    }

    public AudioService()
    {
        if (OperatingSystem.IsWindows())
        {
            InitializeWaveIn();
        }
    }

    private void InitializeWaveIn()
    {
#if WINDOWS
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            if (WaveIn.DeviceCount == 0)
            {
                ErrorOccurred?.Invoke("Kullanılabilir mikrofon bulunamadı. Lütfen mikrofonunuzu ve Windows mikrofon izinlerini kontrol edin.");
                return;
            }

            _waveIn?.Dispose();
            _waveIn = new WaveIn
            {
                DeviceNumber = Math.Min(_selectedDeviceNumber, Math.Max(0, WaveIn.DeviceCount - 1)),
                WaveFormat = new WaveFormat(_sampleRate, 16, _channels),
                BufferMilliseconds = 100
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Mikrofon hazırlanamadı: {ex.Message}");
        }
#endif
    }

#if WINDOWS
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var floatData = new float[e.BytesRecorded / 2];
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            floatData[i / 2] = sample / 32768f;
        }
        UpdateEnergyLevels(floatData);
        AudioDataAvailable?.Invoke(floatData);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;
        if (e.Exception != null)
        {
            ErrorOccurred?.Invoke($"Recording error: {e.Exception.Message}");
        }
    }
#endif

    public async Task StartRecordingAsync()
    {
#if WINDOWS
        if (_isRecording || !OperatingSystem.IsWindows()) return;
        
        try
        {
            InitializeWaveIn();
            if (_waveIn == null) return;
            _isRecording = true;
            _waveIn.StartRecording();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _isRecording = false;
            ErrorOccurred?.Invoke($"Failed to start recording: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    public async Task StopRecordingAsync()
    {
#if WINDOWS
        if (!_isRecording || !OperatingSystem.IsWindows()) return;
        
        try
        {
            _waveIn?.StopRecording();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Failed to stop recording: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    public async Task<List<string>> GetAvailableInputDevicesAsync()
    {
        var devices = new List<string>();
#if WINDOWS
        if (!OperatingSystem.IsWindows())
        {
            devices.Add("Varsayılan Mikrofon");
            return devices;
        }

        var deviceCount = WaveIn.DeviceCount;
        
        for (int i = 0; i < deviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            devices.Add($"{i}: {caps.ProductName} ({caps.Channels} ch)");
        }
#else
        devices.Add("Varsayılan Mikrofon");
#endif
        return await Task.FromResult(devices);
    }

    public void SetInputDevice(int deviceNumber)
    {
        _selectedDeviceNumber = deviceNumber;
        if (_isRecording)
        {
            _ = StopRecordingAsync();
            _ = StartRecordingAsync();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
#if WINDOWS
            if (_isRecording)
            {
                _waveIn?.StopRecording();
            }
            _waveIn?.Dispose();
#endif
            _disposed = true;
        }
    }
}