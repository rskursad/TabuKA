using System;
using System.Threading.Tasks;

namespace TabuKA.Services;

public interface IPermissionService
{
    Task<bool> HasMicrophonePermissionAsync();
    Task<bool> RequestMicrophonePermissionAsync();
}

public static class PermissionProvider
{
    public static Func<Task<bool>>? CustomPermissionHandler { get; set; }
}
