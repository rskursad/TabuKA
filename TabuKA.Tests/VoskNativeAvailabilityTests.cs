using System;
using System.IO;
using System.Threading.Tasks;
using TabuKA.Services;
using Xunit;

namespace TabuKA.Tests;

/// <summary>
/// Vosk native kütüphanesi (libvosk) yalnızca linux-x64 / win-x64 / osx-universal için
/// paketleniyor. Android'de P/Invoke başarısız oluyor ve oyun "Vosk başlatılamadı: libvosk"
/// hatası veriyordu. Bu testler, motorun çalışmadığı platformlarda oyunun sessizce
/// devam etmesini ve doğru mesajı göstermesini doğrular.
/// </summary>
public class VoskNativeAvailabilityTests
{
    [Fact]
    public void IsNativeLoadFailure_DetectsDllNotFound()
    {
        Assert.True(VoskNative.IsNativeLoadFailure(new DllNotFoundException("libvosk")));
    }

    [Fact]
    public void IsNativeLoadFailure_DetectsBadImageFormat()
    {
        Assert.True(VoskNative.IsNativeLoadFailure(new BadImageFormatException("wrong arch")));
    }

    [Fact]
    public void IsNativeLoadFailure_DetectsWrappedInnerException()
    {
        var wrapped = new InvalidOperationException("outer", new DllNotFoundException("libvosk"));

        Assert.True(VoskNative.IsNativeLoadFailure(wrapped));
    }

    [Fact]
    public void IsNativeLoadFailure_IgnoresUnrelatedException()
    {
        Assert.False(VoskNative.IsNativeLoadFailure(new IOException("disk full")));
        Assert.False(VoskNative.IsNativeLoadFailure(null));
    }

    [Fact]
    public void Probe_DoesNotThrow_AndIsCached()
    {
        var first = VoskNative.IsAvailable;
        var second = VoskNative.IsAvailable;

        Assert.Equal(first, second);
    }

    [Fact]
    public void UnavailableReason_IsEmptyWhenSupported_NonEmptyOtherwise()
    {
        if (VoskNative.IsAvailable)
            Assert.Empty(VoskNative.UnavailableReason);
        else
            Assert.NotEmpty(VoskNative.UnavailableReason);
    }

    [Fact]
    public async Task IsAvailableAsync_IsFalseWhenEngineUnsupported_EvenIfModelPresent()
    {
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio);

        // Model dosyaları var olsa bile native kütüphane yoksa motor çalışamaz.
        var modelPresent = VoskSpeechRecognitionService.HasModelFiles(
            VoskSpeechRecognitionService.ResolveModelPath());

        var available = await vosk.IsAvailableAsync();

        Assert.False(available);
        Assert.Equal(modelPresent && VoskNative.IsAvailable, available);
    }

    [Fact]
    public async Task StartListeningAsync_WhenEngineUnsupported_DoesNotThrow()
    {
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio);

        string? error = null;
        vosk.ErrorOccurred += message => error = message;

        if (VoskNative.IsAvailable)
            return; // Bu makinede motor çalışıyor, senaryo geçerli değil.

        // Oyun akışını bozmamalı: exception fırlatmamalı.
        await vosk.StartListeningAsync();

        Assert.False(vosk.IsListening);
        Assert.NotNull(error);
        Assert.DoesNotContain("libvosk başlat", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartListeningAsync_WhenModelMissing_ReportsModelErrorAndDoesNotThrow()
    {
        using var audio = new AudioService();
        using var vosk = new VoskSpeechRecognitionService(audio);

        if (VoskSpeechRecognitionService.HasModelFiles(
                VoskSpeechRecognitionService.ResolveModelPath()))
        {
            return; // Model kurulu, senaryo geçerli değil.
        }

        string? error = null;
        vosk.ErrorOccurred += message => error = message;

        await vosk.StartListeningAsync();

        Assert.False(vosk.IsListening);
        Assert.NotNull(error);
    }
}
