using System;
using System.Threading.Tasks;
#if WINDOWS
using NAudio.Wave;
#endif

namespace TabuKA.Services;

public class DesktopPermissionService : IPermissionService
{
    private readonly IAudioService _audioService;

    public DesktopPermissionService(IAudioService audioService)
    {
        _audioService = audioService;
    }

    public async Task<bool> HasMicrophonePermissionAsync()
    {
        return await CheckAccessAsync();
    }

    public async Task<bool> RequestMicrophonePermissionAsync()
    {
        return await CheckAccessAsync();
    }

    private async Task<bool> CheckAccessAsync()
    {
        if (PermissionProvider.CustomPermissionHandler != null)
        {
            return await PermissionProvider.CustomPermissionHandler();
        }

        try
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                if (WaveIn.DeviceCount == 0)
                {
                    return false;
                }

                // Test creating a WaveIn instance to verify Windows privacy permissions
                using var testWaveIn = new WaveIn
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 50
                };
                return true;
            }
#endif
            var devices = await _audioService.GetAvailableInputDevicesAsync();
            return devices.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Microphone permission/device check failed: {ex.Message}");
            return false;
        }
    }
}
