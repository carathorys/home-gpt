using System.Reflection;

namespace home_gpt.Core.Tests;

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

    public string CreateFile(string relativePath, string contents)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, contents);
        return fullPath;
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

internal static class ReflectionHelpers
{
    public static T InvokePrivateStatic<T>(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Method '{methodName}' was not found on '{type.Name}'.");

        return (T)method.Invoke(null, args)!;
    }
}
