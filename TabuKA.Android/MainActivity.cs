using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using TabuKA.Services;

#pragma warning disable CA1416

namespace TabuKA.Android;

[Activity(
    Label = "TabuKA",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private static TaskCompletionSource<bool>? _permissionTcs;
    private const int RecordAudioRequestCode = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        PermissionProvider.CustomPermissionHandler = RequestAudioPermissionAsync;
    }

    private Task<bool> RequestAudioPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M ||
            CheckSelfPermission(Manifest.Permission.RecordAudio) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _permissionTcs = new TaskCompletionSource<bool>();
        RequestPermissions(new[] { Manifest.Permission.RecordAudio }, RecordAudioRequestCode);
        return _permissionTcs.Task;
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == RecordAudioRequestCode)
        {
            var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
            _permissionTcs?.TrySetResult(granted);
        }
    }
}
