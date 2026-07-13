using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using home_gpt.Avalonia.Services;
using home_gpt.Training;

namespace home_gpt.Avalonia.Services;

[ExcludeFromCodeCoverage]
public sealed class CheckpointDialogService(Func<Window?> windowProvider) : ICheckpointDialogService
{
    public async Task<TrainingStartChoice?> PromptAsync(CheckpointPromptKind kind, string outputDirectory)
    {
        var window = windowProvider();
        if (window is null)
            return TrainingStartChoice.Cancel;

        var dialog = new CheckpointDialog(kind, outputDirectory);
        return await dialog.ShowDialog<TrainingStartChoice?>(window);
    }
}
