using System.Diagnostics.CodeAnalysis;

namespace home_gpt.Cli.Ui;

[ExcludeFromCodeCoverage]
internal sealed class EscapeKeyMonitor : IDisposable
{
    private readonly CancellationTokenSource _listenerCts = new();
    private readonly CancellationTokenSource _trainingCts;
    private readonly Task _listenerTask;

    private EscapeKeyMonitor(CancellationTokenSource trainingCts)
    {
        _trainingCts = trainingCts;
        _listenerTask = Task.Run(ListenForEscape);
    }

    public static EscapeKeyMonitor Start(CancellationTokenSource trainingCts) => new(trainingCts);

    private void ListenForEscape()
    {
        while (!_listenerCts.IsCancellationRequested && !_trainingCts.IsCancellationRequested)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(50);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                _trainingCts.Cancel();
                return;
            }
        }
    }

    public void Dispose()
    {
        _listenerCts.Cancel();
        try
        {
            _listenerTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Listener was cancelled.
        }

        _listenerCts.Dispose();
    }
}
