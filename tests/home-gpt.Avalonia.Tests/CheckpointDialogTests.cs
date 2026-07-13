using home_gpt.Avalonia.Services;
using home_gpt.Training;

namespace home_gpt.Avalonia.Tests;

public sealed class CheckpointDialogServiceTests
{
    [Fact]
    public async Task PromptAsync_ReturnsCancelWhenOwnerMissing()
    {
        var service = new CheckpointDialogService(() => null);

        var result = await service.PromptAsync(
            CheckpointPromptKind.ExistingCheckpoint,
            "/tmp/model");

        Assert.Equal(TrainingStartChoice.Cancel, result);
    }
}
