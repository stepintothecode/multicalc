using MultiCalc.Domain.Calculation;
using MultiCalc.Domain.Sessions;
using MultiCalc.Storage;
using MultiCalc.TestSupport;
using Xunit;

namespace MultiCalc.Storage.Tests;

public sealed class JsonSessionStoreTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Nothing_saved_yet_reads_as_null_rather_than_an_empty_book()
    {
        using var directory = new TempDirectory();

        Assert.Null(await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_saved_book_comes_back_whole()
    {
        using var directory = new TempDirectory();
        var store = new JsonSessionStore(directory.Path);

        var book = SessionBook.Start("a", At).AddNew("b", At);
        var typed = book.Active.WithDraft(ExpressionDraft.FromExpression("12+3"))
            .WithResult(new CalculationEntry("2+3", "5", At), 5m)
            .Rename("Rent");

        await store.SaveAsync(book.Replace(typed), TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Sessions.Count);
        Assert.Equal("b", loaded.ActiveId);
        Assert.Equal("Rent", loaded.Active.Name);
        Assert.Equal("5", loaded.Active.Draft.Expression);
        Assert.Single(loaded.Active.History);
        Assert.Equal("2+3", loaded.Active.History[0].Expression);
    }

    [Fact]
    public async Task Saving_twice_replaces_rather_than_appends()
    {
        using var directory = new TempDirectory();
        var store = new JsonSessionStore(directory.Path);

        await store.SaveAsync(SessionBook.Start("a", At).AddNew("b", At), TestContext.Current.CancellationToken);
        await store.SaveAsync(SessionBook.Start("c", At), TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Sessions);
        Assert.Equal("c", loaded.ActiveId);
    }

    [Fact]
    public async Task Tag_colours_survive_a_restart()
    {
        using var directory = new TempDirectory();
        var store = new JsonSessionStore(directory.Path);

        var book = SessionBook.Start("a", At).AddNew("b", At);
        var recoloured = book.Replace(book.Active.WithTint(CalculatorTint.FromHue(320)));

        await store.SaveAsync(recoloured, TestContext.Current.CancellationToken);
        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(320, loaded.Active.Tint.Hue);
    }

    [Fact]
    public async Task The_scientific_setting_survives_a_restart()
    {
        using var directory = new TempDirectory();
        var store = new JsonSessionStore(directory.Path);

        var book = SessionBook.Start("a", At);
        await store.SaveAsync(
            book.Replace(book.Active.WithScientific(true)),
            TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.True(loaded.Active.Scientific);
    }

    [Fact]
    public async Task Sessions_saved_before_colours_existed_still_load()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "sessions.json"),
            """{"Version":1,"ActiveId":"a","Sessions":[{"Id":"a","Name":"Rent","Expression":"2+2"}]}""",
            TestContext.Current.CancellationToken);

        var loaded = await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal("Rent", loaded.Active.Name);
        Assert.Equal(CalculatorTint.Default, loaded.Active.Tint);
    }

    [Fact]
    public async Task A_named_colour_from_the_build_before_the_picker_keeps_its_colour()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "sessions.json"),
            """{"Version":1,"ActiveId":"a","Sessions":[{"Id":"a","Name":"Rent","Colour":"Pink"}]}""",
            TestContext.Current.CancellationToken);

        var loaded = await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(325, loaded.Active.Tint.Hue);
    }

    [Fact]
    public async Task A_corrupt_file_reads_as_null_rather_than_taking_the_app_down()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "sessions.json"),
            "{ not json",
            TestContext.Current.CancellationToken);

        Assert.Null(await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_file_from_a_newer_build_is_refused_rather_than_half_read()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "sessions.json"),
            """{"Version":99,"ActiveId":"a","Sessions":[{"Id":"a","Name":"Calculator 1"}]}""",
            TestContext.Current.CancellationToken);

        Assert.Null(await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_session_without_an_id_is_dropped_rather_than_restored_broken()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "sessions.json"),
            """{"Version":1,"ActiveId":"a","Sessions":[{"Id":"","Name":"Ghost"}]}""",
            TestContext.Current.CancellationToken);

        Assert.Null(await new JsonSessionStore(directory.Path).LoadAsync(TestContext.Current.CancellationToken));
    }
}
