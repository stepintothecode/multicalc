namespace MultiCalc.Ui.Services;

/// <summary>
/// Routes the phone's back gesture into the Blazor UI.
/// <para>
/// Nothing does this on its own: the web view has no browser chrome, so a back press goes
/// straight to the activity and closes the app, whatever is on screen. Pages and sheets
/// register what back should mean while they are up, and the newest registration answers
/// first.
/// </para>
/// </summary>
public sealed class BackNavigation
{
    private readonly List<Func<bool>> handlers = [];

    /// <summary>
    /// Registers a handler for as long as the returned token is undisposed. It returns true
    /// when it consumed the press, and false to let whatever is behind it decide.
    /// </summary>
    public IDisposable Register(Func<bool> handler)
    {
        handlers.Add(handler);
        return new Registration(this, handler);
    }

    /// <summary>
    /// Offers the press to each handler, newest first.
    /// Returns false when nothing wanted it, which means the app should close.
    /// </summary>
    public bool Handle()
    {
        for (var i = handlers.Count - 1; i >= 0; i--)
        {
            if (handlers[i]())
            {
                return true;
            }
        }

        return false;
    }

    private sealed class Registration : IDisposable
    {
        private readonly BackNavigation owner;
        private readonly Func<bool> handler;

        public Registration(BackNavigation owner, Func<bool> handler)
        {
            this.owner = owner;
            this.handler = handler;
        }

        public void Dispose() => owner.handlers.Remove(handler);
    }
}
