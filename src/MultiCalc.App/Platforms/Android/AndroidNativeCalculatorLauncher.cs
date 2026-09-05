using Android.Content;
using Android.Content.PM;
using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;
using Application = Android.App.Application;

namespace MultiCalc.App.Platforms.Android;

/// <summary>
/// Opens extra copies of whatever calculator the phone shipped with.
/// <para>
/// Two flags do the work: NEW_TASK puts the calculator in its own task, and MULTIPLE_TASK
/// stops Android from reusing the task that is already there. Together they produce a
/// genuinely separate window with its own Recents card and its own state.
/// </para>
/// <para>
/// It only works when the target activity's launch mode is standard or singleTop. A
/// singleTask or singleInstance activity is collapsed back into the existing task no matter
/// what flags are set, so the launch mode is checked up front rather than discovered by a
/// button that silently does nothing.
/// </para>
/// </summary>
public sealed class AndroidNativeCalculatorLauncher : INativeCalculatorLauncher
{
    private const ActivityFlags SpawnFlags = ActivityFlags.NewTask | ActivityFlags.MultipleTask;

    /// <summary>
    /// No match filter at all, and that matters: MATCH_DEFAULT_ONLY only returns activities
    /// declaring CATEGORY_DEFAULT, which launcher entry points do not. Asking for it here
    /// makes every query below come back empty on every phone.
    /// </summary>
    private const PackageInfoFlags MatchAnything = 0;

    /// <summary>
    /// Calculators that ship on the phones people actually own. Checked because
    /// CATEGORY_APP_CALCULATOR is optional and plenty of manufacturers skip it: Samsung's
    /// calculator, for one, declares only CATEGORY_LAUNCHER, so the category search alone
    /// finds nothing on a Galaxy.
    /// </summary>
    private static readonly string[] KnownPackages =
    [
        "com.sec.android.app.popupcalculator",  // Samsung
        "com.google.android.calculator",        // Google, and most stock builds
        "com.android.calculator2",              // AOSP
        "com.miui.calculator",                  // Xiaomi, Redmi, Poco
        "com.oneplus.calculator",               // OnePlus
        "com.coloros.calculator",               // Oppo, Realme
        "com.oppo.calculator",
        "com.realme.calculator",
        "com.vivo.calculator",                  // Vivo
        "com.bbk.calculator",
        "com.transsion.calculator",             // Tecno, Infinix, itel
        "com.asus.calculator",
        "com.huawei.calculator",
        "com.motorola.calculator",
        "com.htc.calculator",
        "com.lge.calculator",
        "com.nothing.calculator",
    ];

    private readonly ILogger<AndroidNativeCalculatorLauncher> logger;

    /// <summary>Creates the launcher.</summary>
    public AndroidNativeCalculatorLauncher(ILogger<AndroidNativeCalculatorLauncher> logger) =>
        this.logger = logger;

    /// <inheritdoc />
    public NativeCalculatorStatus Probe()
    {
        var packages = Application.Context.PackageManager;

        if (packages is null)
        {
            return NativeCalculatorStatus.Unsupported;
        }

        var activity = FindByCategory(packages)
            ?? FindByKnownPackage(packages)
            ?? FindByLauncherScan(packages);

        if (activity is null)
        {
            logger.LogInformation("No calculator app found by any strategy");
            return new NativeCalculatorStatus(NativeCalculatorSupport.NoCalculatorFound);
        }

        var support = AcceptsASecondTask(activity)
            ? NativeCalculatorSupport.Supported
            : NativeCalculatorSupport.SingleInstanceOnly;

        logger.LogInformation(
            "Calculator {Package}/{Activity} launchMode={Mode} support={Support}",
            activity.PackageName,
            activity.Name,
            activity.LaunchMode,
            support);

        return new NativeCalculatorStatus(
            support,
            activity.PackageName,
            activity.Name,
            SafeLabel(activity, packages));
    }

    /// <inheritdoc />
    public NativeLaunchOutcome LaunchNewInstance()
    {
        var status = Probe();

        if (!status.CanSpawn || status.PackageName is null || status.ActivityName is null)
        {
            return NativeLaunchOutcome.NotSupported;
        }

        using var intent = new Intent(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);
        intent.SetComponent(new ComponentName(status.PackageName, status.ActivityName));
        intent.SetFlags(SpawnFlags);

        try
        {
            // Starting from the visible activity rather than the application context keeps this
            // inside Android's rules on background activity launches.
            var host = Platform.CurrentActivity as Context ?? Application.Context;
            host.StartActivity(intent);

            return NativeLaunchOutcome.Launched;
        }
        catch (global::Android.Content.ActivityNotFoundException ex)
        {
            logger.LogWarning(ex, "The calculator activity vanished between probing and launching");
            return NativeLaunchOutcome.NotSupported;
        }
        catch (global::Java.Lang.SecurityException ex)
        {
            logger.LogWarning(ex, "Not allowed to start the calculator");
            return NativeLaunchOutcome.Failed;
        }
    }

    /// <summary>The category Android reserves for calculators. Correct, but optional, so it often misses.</summary>
    private ActivityInfo? FindByCategory(PackageManager packages)
    {
        using var intent = new Intent(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryAppCalculator);

        return QueryFirst(packages, intent);
    }

    /// <summary>The manufacturer calculators, asked for by name.</summary>
    private ActivityInfo? FindByKnownPackage(PackageManager packages)
    {
        foreach (var package in KnownPackages)
        {
            using var intent = new Intent(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryLauncher);
            intent.SetPackage(package);

            if (QueryFirst(packages, intent) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Last resort: look through everything with a launcher icon for something that calls
    /// itself a calculator. Catches builds and regions the list above does not know about.
    /// </summary>
    private ActivityInfo? FindByLauncherScan(PackageManager packages)
    {
        using var intent = new Intent(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);

        try
        {
#pragma warning disable CA1422 // The typed-flags overload is API 33+; this build still targets older.
            var matches = packages.QueryIntentActivities(intent, MatchAnything);
#pragma warning restore CA1422

            return matches
                .Select(match => match.ActivityInfo)
                .FirstOrDefault(info => info is not null && LooksLikeACalculator(info, packages));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not scan launcher activities");
            return null;
        }
    }

    private static bool LooksLikeACalculator(ActivityInfo info, PackageManager packages)
    {
        if (info.PackageName is null || info.Name is null)
        {
            return false;
        }

        if (info.PackageName.Contains("calculator", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // "Calculator" is what the icon says, in whatever language the label happens to be.
        var label = SafeLabel(info, packages);

        return label is not null && label.Contains("calculator", StringComparison.OrdinalIgnoreCase);
    }

    private ActivityInfo? QueryFirst(PackageManager packages, Intent intent)
    {
        try
        {
#pragma warning disable CA1422
            var matches = packages.QueryIntentActivities(intent, MatchAnything);
#pragma warning restore CA1422

            return matches
                .Select(match => match.ActivityInfo)
                .FirstOrDefault(info => info is not null && info.Name is not null && info.PackageName is not null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not query for {Intent}", intent.Package ?? "calculators");
            return null;
        }
    }

    /// <summary>
    /// Standard and singleTop activities can be told to start in a fresh task.
    /// singleTask and singleInstance cannot, whatever flags are used.
    /// </summary>
    private static bool AcceptsASecondTask(ActivityInfo activity) =>
        activity.LaunchMode is LaunchMode.Multiple or LaunchMode.SingleTop;

    private static string? SafeLabel(ActivityInfo activity, PackageManager packages)
    {
        try
        {
            var label = activity.LoadLabel(packages);
            return string.IsNullOrWhiteSpace(label) ? null : label;
        }
        catch (Exception)
        {
            // A missing label is cosmetic. The UI falls back to a generic name.
            return null;
        }
    }
}
