using home_gpt.Persistence;
using home_gpt.Training;

namespace home_gpt.Core.Tests;

public sealed class CheckpointSessionResolverTests
{
    [Fact]
    public void Resolve_ReturnsFreshStateWhenCheckpointMissing()
    {
        var state = CheckpointSessionResolver.Resolve(null, "/tmp/words.txt", 64, 128);

        Assert.False(state.ResumeFromCheckpoint);
        Assert.Equal(0, state.CompletedEpochs);
        Assert.False(state.IsArchitectureChanged);
        Assert.False(state.CanResume);
    }

    [Fact]
    public void Resolve_MarksArchitectureChangedWhenHiddenSizeDiffers()
    {
        var checkpoint = new ModelMetadata(12, 32, 64, 10, "\"abc\"", CompletedEpochs: 50);

        var state = CheckpointSessionResolver.Resolve(checkpoint, "/tmp/words.txt", 32, 128);

        Assert.False(state.ResumeFromCheckpoint);
        Assert.True(state.IsArchitectureChanged);
        Assert.Equal(50, state.CompletedEpochs);
    }

    [Fact]
    public void ApplyMetadata_UpdatesEditorValues()
    {
        string? dataPath = null;
        var embedSize = 0;
        var hiddenSize = 0;
        var completedEpochs = 0;
        var batchSize = 0;
        double learningRate = 0;

        CheckpointSessionResolver.ApplyMetadata(
            new ModelMetadata(12, 24, 48, 10, "\"abc\"", CompletedEpochs: 75, BatchSize: 16, LearningRate: 0.02),
            path => dataPath = path,
            value => embedSize = value,
            value => hiddenSize = value,
            value => completedEpochs = value,
            value => batchSize = value,
            value => learningRate = value);

        Assert.Equal(24, embedSize);
        Assert.Equal(48, hiddenSize);
        Assert.Equal(75, completedEpochs);
        Assert.Equal(16, batchSize);
        Assert.Equal(0.02, learningRate);
    }

    [Fact]
    public void CanResume_ReturnsFalseWhenDataPathMissing()
    {
        var checkpoint = new ModelMetadata(12, 32, 64, 10, "\"abc\"");

        Assert.False(CheckpointSessionResolver.CanResume(checkpoint, null));
    }
}
