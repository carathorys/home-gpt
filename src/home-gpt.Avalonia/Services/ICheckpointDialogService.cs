using home_gpt.Training;

namespace home_gpt.Avalonia.Services;

public interface ICheckpointDialogService
{
    Task<TrainingStartChoice?> PromptAsync(CheckpointPromptKind kind, string outputDirectory);
}
