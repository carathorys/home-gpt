namespace home_gpt.Cli.Tests;

internal sealed class TestFileSystem : IDisposable
{
    public string RootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "home-gpt-tests",
        Guid.NewGuid().ToString("N"));

    public TestFileSystem()
    {
        Directory.CreateDirectory(RootPath);
    }

    public string CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
    }
}
