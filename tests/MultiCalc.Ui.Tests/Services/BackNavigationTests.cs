using MultiCalc.Ui.Services;
using Xunit;

namespace MultiCalc.Ui.Tests.Services;

public sealed class BackNavigationTests
{
    [Fact]
    public void With_nothing_registered_the_press_is_not_ours()
    {
        // False is what lets the activity close the app, which is right on the home screen.
        Assert.False(new BackNavigation().Handle());
    }

    [Fact]
    public void A_handler_that_takes_the_press_stops_it_going_further()
    {
        var back = new BackNavigation();
        back.Register(() => true);

        Assert.True(back.Handle());
    }

    [Fact]
    public void The_newest_handler_answers_first()
    {
        var back = new BackNavigation();
        var order = new List<string>();

        back.Register(() => { order.Add("page"); return true; });
        back.Register(() => { order.Add("sheet"); return true; });

        back.Handle();

        Assert.Equal(["sheet"], order);
    }

    [Fact]
    public void A_handler_that_declines_falls_through_to_the_one_behind_it()
    {
        var back = new BackNavigation();
        var reached = false;

        back.Register(() => { reached = true; return true; });
        back.Register(() => false);

        Assert.True(back.Handle());
        Assert.True(reached);
    }

    [Fact]
    public void Every_handler_declining_lets_the_app_close()
    {
        var back = new BackNavigation();

        back.Register(() => false);
        back.Register(() => false);

        Assert.False(back.Handle());
    }

    [Fact]
    public void Disposing_a_registration_takes_it_out_of_the_chain()
    {
        var back = new BackNavigation();
        var registration = back.Register(() => true);

        registration.Dispose();

        Assert.False(back.Handle());
    }

    [Fact]
    public void Disposing_one_leaves_the_others_alone()
    {
        var back = new BackNavigation();

        back.Register(() => true);
        back.Register(() => true).Dispose();

        Assert.True(back.Handle());
    }
}
