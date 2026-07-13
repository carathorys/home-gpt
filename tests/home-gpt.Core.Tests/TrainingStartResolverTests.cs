using home_gpt.Training;

namespace home_gpt.Core.Tests;

public sealed class TrainingStartResolverTests
{
    [Fact]
    public void GetPromptKind_ReturnsNoneWhenCheckpointMissing()
    {
        var state = new TrainingConfigState();

        Assert.Equal(CheckpointPromptKind.None, TrainingStartResolver.GetPromptKind(state));
    }

    [Fact]
    public void GetPromptKind_ReturnsArchitectureChangedWhenHiddenSizeDiffers()
    {
        var state = new TrainingConfigState { HiddenSize = 128, EmbedSize = 32 };
        SetField(state, "_loadedCheckpoint", new home_gpt.Persistence.ModelMetadata(
            12, 32, 64, 10, "\"abc\"", CompletedEpochs: 50));

        Assert.Equal(CheckpointPromptKind.ArchitectureChanged, TrainingStartResolver.GetPromptKind(state));
    }

    [Fact]
    public void TryApplyChoice_EnablesResumeWhenRequested()
    {
        var state = new TrainingConfigState();
        SetField(state, "_loadedCheckpoint", new home_gpt.Persistence.ModelMetadata(
            12, 32, 64, 10, "\"abc\"", CompletedEpochs: 50));

        Assert.True(TrainingStartResolver.TryApplyChoice(state, TrainingStartChoice.ContinueFromCheckpoint));

        var config = state.ToConfig();
        Assert.True(config.ResumeFromCheckpoint);
        Assert.Equal(50, config.PreviousCompletedEpochs);
    }

    [Fact]
    public void TryApplyChoice_CancelReturnsFalse()
    {
        var state = new TrainingConfigState();

        Assert.False(TrainingStartResolver.TryApplyChoice(state, TrainingStartChoice.Cancel));
    }

    private static void SetField(TrainingConfigState state, string fieldName, object? value)
    {
        var field = typeof(TrainingConfigState).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found.");

        field.SetValue(state, value);
    }
}
