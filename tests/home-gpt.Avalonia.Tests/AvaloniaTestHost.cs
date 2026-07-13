using Avalonia;
using Avalonia.Headless;

namespace home_gpt.Avalonia.Tests;

public static class AvaloniaTestHost
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
                return;

            AppBuilder.Configure<home_gpt.Avalonia.App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();

            _initialized = true;
        }
    }
}
