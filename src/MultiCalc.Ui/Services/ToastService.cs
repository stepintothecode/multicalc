namespace MultiCalc.Ui.Services;

/// <summary>A short message at the bottom of the screen that fades on its own.</summary>
public sealed class ToastService
{
    private const int VisibleMilliseconds = 2600;

    private CancellationTokenSource? dismiss;

    /// <summary>Raised when the message appears or goes away.</summary>
    public event Action? Changed;

    /// <summary>What is showing, or null when nothing is.</summary>
    public string? Message { get; private set; }

    /// <summary>Shows a message, replacing any that is already up.</summary>
    public void Show(string message)
    {
        Message = message;
        Changed?.Invoke();

        dismiss?.Cancel();
        dismiss = new CancellationTokenSource();

        var token = dismiss.Token;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(VisibleMilliseconds, token).ConfigureAwait(false);

                    Message = null;
                    Changed?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    // Replaced by a newer message.
                }
            },
            CancellationToken.None);
    }
}
