namespace home_gpt.Avalonia.Services;

public interface IFileDialogService
{
    Task<string?> PickFileAsync(string title);
    Task<string?> PickDirectoryAsync(string title);
}
