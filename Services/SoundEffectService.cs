using System;
using System.IO;
using System.Threading.Tasks;
#if WINDOWS
using NAudio.Wave;
#endif

namespace TabuKA.Services;

public interface ISoundEffectService
{
    void PlayCorrect();
    void PlayTaboo();
    void PlayPass();
    void PlayTick();
    void PlayTimeUp();
}

public class SoundEffectService : ISoundEffectService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private bool _disposed;
    private readonly byte[] _correctPcm;
    private readonly byte[] _tabooPcm;
    private readonly byte[] _passPcm;
    private readonly byte[] _tickPcm;
    private readonly byte[] _timeUpPcm;
#if WINDOWS
    private readonly WaveFormat _format = new(22050, 16, 1);
#endif

    public SoundEffectService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Pre-synthesize PCM sound bytes for zero-latency playback
        _correctPcm = GenerateChimePcm();
        _tabooPcm = GenerateBuzzerPcm();
        _passPcm = GeneratePassPcm();
        _tickPcm = GenerateTickPcm();
        _timeUpPcm = GenerateTimeUpPcm();
    }

    public void PlayCorrect() => PlayPcmAsync(_correctPcm);
    public void PlayTaboo() => PlayPcmAsync(_tabooPcm);
    public void PlayPass() => PlayPcmAsync(_passPcm);
    public void PlayTick() => PlayPcmAsync(_tickPcm);
    public void PlayTimeUp() => PlayPcmAsync(_timeUpPcm);

    private void PlayPcmAsync(byte[] pcmData)
    {
        Task.Run(async () =>
        {
            try
            {
                if (!OperatingSystem.IsWindows()) return;

                var masterVolume = await _settingsService.GetMasterVolumeAsync();
                if (masterVolume <= 0) return;

                var volumeRatio = Math.Clamp(masterVolume / 100f, 0f, 1f);

                // Adjust PCM volume
                var adjustedPcm = new byte[pcmData.Length];
                for (int i = 0; i < pcmData.Length; i += 2)
                {
                    short sample = BitConverter.ToInt16(pcmData, i);
                    short scaled = (short)Math.Clamp(sample * volumeRatio, short.MinValue, short.MaxValue);
                    adjustedPcm[i] = (byte)(scaled & 0xFF);
                    adjustedPcm[i + 1] = (byte)((scaled >> 8) & 0xFF);
                }

#if WINDOWS
                using var ms = new MemoryStream(adjustedPcm);
                using var rawStream = new RawSourceWaveStream(ms, _format);
                using var waveOut = new WaveOut();
                waveOut.Init(rawStream);
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(10);
                }
#endif
            }
            catch
            {
                // Ignore audio device playback errors
            }
        });
    }

    private static byte[] GenerateChimePcm()
    {
        // 2-tone pleasant chime: 659Hz (E5) for 100ms, then 880Hz (A5) for 150ms
        int sampleRate = 22050;
        int tone1Samples = (int)(sampleRate * 0.10);
        int tone2Samples = (int)(sampleRate * 0.18);
        int totalSamples = tone1Samples + tone2Samples;
        var pcm = new byte[totalSamples * 2];

        for (int i = 0; i < tone1Samples; i++)
        {
            double decay = 1.0 - ((double)i / tone1Samples * 0.4);
            short s = (short)(Math.Sin(2 * Math.PI * 659.25 * i / sampleRate) * 12000 * decay);
            pcm[i * 2] = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        for (int i = 0; i < tone2Samples; i++)
        {
            int idx = tone1Samples + i;
            double decay = Math.Exp(-3.0 * i / tone2Samples);
            short s = (short)(Math.Sin(2 * Math.PI * 880.0 * i / sampleRate) * 14000 * decay);
            pcm[idx * 2] = (byte)(s & 0xFF);
            pcm[idx * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return pcm;
    }

    private static byte[] GenerateBuzzerPcm()
    {
        // Low harsh buzzer: 140Hz square wave with 280Hz harmonic for 280ms
        int sampleRate = 22050;
        int totalSamples = (int)(sampleRate * 0.28);
        var pcm = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double decay = 1.0 - ((double)i / totalSamples * 0.2);
            double v1 = Math.Sin(2 * Math.PI * 140 * t) > 0 ? 1.0 : -1.0;
            double v2 = Math.Sin(2 * Math.PI * 280 * t) > 0 ? 0.5 : -0.5;
            short s = (short)((v1 + v2) * 8000 * decay);
            pcm[i * 2] = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return pcm;
    }

    private static byte[] GeneratePassPcm()
    {
        // Neutral short double-click / swoosh: 440Hz -> 330Hz quick chirp (90ms)
        int sampleRate = 22050;
        int totalSamples = (int)(sampleRate * 0.09);
        var pcm = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double freq = 440.0 - (110.0 * i / totalSamples);
            double decay = Math.Exp(-5.0 * i / totalSamples);
            short s = (short)(Math.Sin(2 * Math.PI * freq * t) * 11000 * decay);
            pcm[i * 2] = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return pcm;
    }

    private static byte[] GenerateTickPcm()
    {
        // Woodblock tick sound (30ms) for countdown
        int sampleRate = 22050;
        int totalSamples = (int)(sampleRate * 0.035);
        var pcm = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            double decay = Math.Exp(-20.0 * i / totalSamples);
            short s = (short)(Math.Sin(2 * Math.PI * 900.0 * i / sampleRate) * 10000 * decay);
            pcm[i * 2] = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return pcm;
    }

    private static byte[] GenerateTimeUpPcm()
    {
        // Classic airhorn/whistle double-burst for time up (400ms)
        int sampleRate = 22050;
        int totalSamples = (int)(sampleRate * 0.42);
        var pcm = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            bool silent = (i > sampleRate * 0.18 && i < sampleRate * 0.22);
            if (silent)
            {
                pcm[i * 2] = 0;
                pcm[i * 2 + 1] = 0;
                continue;
            }

            double v = (Math.Sin(2 * Math.PI * 220 * t) > 0 ? 0.7 : -0.7)
                     + (Math.Sin(2 * Math.PI * 440 * t) > 0 ? 0.3 : -0.3);
            short s = (short)(v * 10000);
            pcm[i * 2] = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        return pcm;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
