using System;
using System.Runtime.InteropServices;

namespace TabuKA.Services;

/// <summary>
/// Detects whether the native Vosk library (libvosk) can actually be loaded in this process.
///
/// The Vosk NuGet package only ships native binaries for linux-x64, win-x64 and osx-universal.
/// It has no Android/iOS binary, so on a phone the P/Invoke inside Vosk.Model fails with
/// DllNotFoundException("libvosk") and the game shows "Vosk başlatılamadı: libvosk".
/// Availability is probed once and cached; the probe itself never throws.
/// </summary>
public static class VoskNative
{
    private const string LibraryName = "libvosk";

    private static readonly object Gate = new();
    private static bool _probed;
    private static bool _available;

    /// <summary>
    /// True when the native Vosk library was found and loaded successfully.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            if (_probed)
                return _available;

            lock (Gate)
            {
                if (_probed)
                    return _available;

                _available = Probe();
                _probed = true;
                return _available;
            }
        }
    }

    /// <summary>
    /// Human readable reason shown to the user when <see cref="IsAvailable"/> is false.
    /// </summary>
    public static string UnavailableReason
    {
        get
        {
            if (IsAvailable)
                return string.Empty;

            if (OperatingSystem.IsAndroid())
                return "Bu Android cihazda sesli tabu için \"libvosk\" yerel kütüphanesi bulunamadı. Sesli kontrol bu cihazda kullanılamaz.";

            if (OperatingSystem.IsIOS())
                return "iOS'ta Vosk ses tanıma desteklenmiyor. Sesli kontrol kullanılamaz.";

            if (OperatingSystem.IsBrowser())
                return "Tarayıcıda Vosk ses tanıma çalışmaz. Sesli kontrol kullanılamaz.";

            return "\"libvosk\" yerel kütüphanesi yüklenemedi. Sesli tanıma kullanılamaz.";
        }
    }

    private static bool Probe()
    {
        try
        {
            if (NativeLibrary.TryLoad(LibraryName, out var handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (BadImageFormatException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (Exception)
        {
        }

        return false;
    }

    /// <summary>
    /// True when the exception means the native Vosk library could not be used,
    /// as opposed to an actual programming/IO error.
    /// </summary>
    public static bool IsNativeLoadFailure(Exception? exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is DllNotFoundException
                or BadImageFormatException
                or EntryPointNotFoundException
                or TypeInitializationException)
            {
                return true;
            }
        }

        return false;
    }
}
