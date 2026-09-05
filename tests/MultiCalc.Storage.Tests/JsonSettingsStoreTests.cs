using MultiCalc.Domain.Settings;
using MultiCalc.Storage;
using MultiCalc.TestSupport;
using Xunit;

namespace MultiCalc.Storage.Tests;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public async Task A_fresh_install_gets_the_defaults()
    {
        using var directory = new TempDirectory();

        var settings = await new JsonSettingsStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppSettings.Default, settings);
        Assert.Equal(ThemePreference.System, settings.Theme);
        Assert.True(settings.HapticFeedback);
    }

    [Fact]
    public async Task Saved_settings_come_back()
    {
        using var directory = new TempDirectory();
        var store = new JsonSettingsStore(directory.Path);

        var saved = new AppSettings
        {
            Theme = ThemePreference.Dark,
            HapticFeedback = false,
        };

        await store.SaveAsync(saved, TestContext.Current.CancellationToken);

        Assert.Equal(saved, await store.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_unrecognised_theme_name_falls_back_to_the_default()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "settings.json"),
            """{"Version":1,"Theme":"Neon","HapticFeedback":false}""",
            TestContext.Current.CancellationToken);

        var settings = await new JsonSettingsStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppSettings.Default.Theme, settings.Theme);
        Assert.False(settings.HapticFeedback);
    }

    [Fact]
    public async Task Settings_left_over_from_an_older_build_still_load()
    {
        using var directory = new TempDirectory();

        // These two were dropped when the New button stopped being configurable. A file
        // written before that must still open rather than resetting someone's theme.
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "settings.json"),
            """{"Version":1,"HasCompletedSetup":true,"NewCalculatorTarget":"Native","Theme":"Dark"}""",
            TestContext.Current.CancellationToken);

        var settings = await new JsonSettingsStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ThemePreference.Dark, settings.Theme);
    }

    [Fact]
    public async Task A_corrupt_file_falls_back_to_the_defaults()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "settings.json"),
            "not json at all",
            TestContext.Current.CancellationToken);

        Assert.Equal(
            AppSettings.Default,
            await new JsonSettingsStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken));
    }
}
