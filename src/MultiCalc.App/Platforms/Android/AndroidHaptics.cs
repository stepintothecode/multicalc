using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;
using Application = Android.App.Application;

namespace MultiCalc.App.Platforms.Android;

/// <summary>
/// The key press buzz, driven straight from the system vibrator.
/// <para>
/// A deliberate short pulse, not <c>EFFECT_TICK</c>. The predefined effects are routed as
/// touch feedback, so the system scales them by the phone's touch vibration setting and
/// they can fire, report success in logcat, and still not be felt. A one shot with a real
/// amplitude is the same buzz every time.
/// </para>
/// </summary>
public sealed class AndroidHaptics : IHaptics
{
    private const long DurationMilliseconds = 22;
    private const int Amplitude = 140;

    private readonly ILogger<AndroidHaptics> logger;
    private readonly Lazy<Vibrator?> vibrator;

    /// <summary>Creates the haptics adapter.</summary>
    public AndroidHaptics(ILogger<AndroidHaptics> logger)
    {
        this.logger = logger;
        vibrator = new Lazy<Vibrator?>(Resolve);
    }

    /// <inheritdoc />
    public void Tap()
    {
        var device = vibrator.Value;

        if (device is null || !device.HasVibrator)
        {
            return;
        }

        try
        {
            // Phones without amplitude control ignore the number and use their own default.
            var amplitude = device.HasAmplitudeControl ? Amplitude : VibrationEffect.DefaultAmplitude;

            device.Vibrate(VibrationEffect.CreateOneShot(DurationMilliseconds, amplitude));
        }
        catch (Exception ex)
        {
            // A phone that refuses to buzz must not cost us the key press.
            logger.LogDebug(ex, "Could not vibrate");
        }
    }

    private Vibrator? Resolve()
    {
        var context = Platform.CurrentActivity as Context ?? Application.Context;

        try
        {
            // Android 12 routes this through a manager that knows about multiple vibrators.
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var manager = context.GetSystemService(Context.VibratorManagerService) as VibratorManager;
                return manager?.DefaultVibrator;
            }

#pragma warning disable CA1422 // The direct service is the only option below API 31.
            return context.GetSystemService(Context.VibratorService) as Vibrator;
#pragma warning restore CA1422
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "No vibrator on this device");
            return null;
        }
    }
}
