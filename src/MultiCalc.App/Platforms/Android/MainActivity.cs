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

    // Portrait only. Six rows of keys plus the display cannot be sized usably across a
    // landscape phone, and a clipped keypad is worse than not rotating. Landscape would
    // need a two column layout, which is a separate piece of work.
    ScreenOrientation = ScreenOrientation.Portrait,
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
