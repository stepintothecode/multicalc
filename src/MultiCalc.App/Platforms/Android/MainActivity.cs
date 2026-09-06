using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using AndroidView = Android.Views.View;

namespace MultiCalc.App;

/// <summary>
/// The single activity. SingleTop so returning to the app reuses the window rather than
/// stacking another copy of MultiCalc, which would be the opposite of the point.
/// </summary>
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,

    // Rotation is allowed again now that landscape has a layout of its own: the two pads
    // go side by side rather than trying to stack six rows into a third of the height.
    // Orientation is in ConfigurationChanges below, so the activity is never recreated
    // and nothing on the display is lost when the phone turns.
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Android 15 forces apps to draw edge to edge, which puts the keypad underneath the
        // navigation bar. The web view cannot see those insets reliably, so the content view
        // is padded natively instead and the page simply gets a smaller box to fill.
        if (FindViewById(global::Android.Resource.Id.Content) is { } content)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarPadding());
            ViewCompat.RequestApplyInsets(content);
        }
    }

    private sealed class SystemBarPadding : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        // Android.Views.View, spelled out: Microsoft.Maui.Controls.View is also in scope here.
        public WindowInsetsCompat OnApplyWindowInsets(AndroidView? view, WindowInsetsCompat? insets)
        {
            if (insets is null)
            {
                return WindowInsetsCompat.Consumed!;
            }

            var bars = insets.GetInsets(
                WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());

            if (view is not null && bars is not null)
            {
                view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            }

            return insets;
        }
    }
}
