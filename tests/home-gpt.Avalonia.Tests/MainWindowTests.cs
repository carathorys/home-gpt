using home_gpt.Avalonia;

namespace home_gpt.Avalonia.Tests;

public sealed class MainWindowTests
{
    [Fact]
    public void MainWindow_CanBeConstructed()
    {
        AvaloniaTestHost.EnsureInitialized();

        var window = new MainWindow();

        Assert.Equal("home-gpt", window.Title);
    }
}
