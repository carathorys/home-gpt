using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace home_gpt.Avalonia.Services;

[ExcludeFromCodeCoverage]
public sealed class FileDialogService(Func<TopLevel?> topLevelProvider) : IFileDialogService
{
    public async Task<string?> PickFileAsync(string title)
    {
        var topLevel = topLevelProvider();
        if (topLevel?.StorageProvider is null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickDirectoryAsync(string title)
    {
        var topLevel = topLevelProvider();
        if (topLevel?.StorageProvider is null)
            return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
