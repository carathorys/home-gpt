using home_gpt.Persistence;
using home_gpt.Training;

namespace home_gpt.Core.Tests;

public sealed class TrainingConfigStateTests
{
    [Fact]
    public void ToConfig_UsesCurrentStateValues()
    {
        var state = new TrainingConfigState
        {
            DataPath = "/tmp/words.txt",
            OutputDirectory = "/tmp/model",
            Epochs = 25,
            BatchSize = 16,
            LearningRate = 0.01,
            HiddenSize = 64,
            EmbedSize = 32
        };

        var config = state.ToConfig();

        Assert.Equal("/tmp/words.txt", config.DataPath);
        Assert.Equal("/tmp/model", config.OutputDirectory);
        Assert.Equal(25, config.Epochs);
        Assert.Equal(16, config.BatchSize);
        Assert.Equal(0.01, config.LearningRate);
        Assert.Equal(64, config.HiddenSize);
        Assert.Equal(32, config.EmbedSize);
    }

    [Fact]
    public void Constructor_LoadsCheckpointMetadataFromDefaultModelDirectory()
    {
        using var fs = new TestFileSystem();
        var originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(fs.RootPath);

        try
        {
            var modelDirectory = fs.CreateDirectory("models/word-model");
            var metadata = new ModelMetadata(
                VocabSize: 12,
                EmbedSize: 24,
                HiddenSize: 48,
                SequenceLength: 10,
                VocabJson: "\"abc\"",
                DataPath: "/tmp/words.txt",
                CompletedEpochs: 75,
                BatchSize: 16,
                LearningRate: 0.02);
            ModelCheckpoint.Save(modelDirectory, metadata);
            File.WriteAllText(ModelCheckpoint.WeightsPath(modelDirectory), "weights");

            var state = new TrainingConfigState();

            Assert.Equal("models/word-model", state.OutputDirectory);
            Assert.Equal(24, state.EmbedSize);
            Assert.Equal(48, state.HiddenSize);
            Assert.Equal(16, state.BatchSize);
            Assert.Equal(0.02, state.LearningRate);
            Assert.Equal(75, state.CompletedEpochs);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void ApplySessionState_DisablesResumeWhenArchitectureChanged()
    {
        var state = new TrainingConfigState
        {
            DataPath = "/tmp/words.txt",
            OutputDirectory = "/tmp/model",
            HiddenSize = 128,
            EmbedSize = 32
        };

        SetField(state, "_loadedCheckpoint", new ModelMetadata(
            VocabSize: 12,
            EmbedSize: 32,
            HiddenSize: 64,
            SequenceLength: 10,
            VocabJson: "\"abc\"",
            CompletedEpochs: 50));

        state.ApplySessionState();
        var config = state.ToConfig();

        Assert.False(config.ResumeFromCheckpoint);
        Assert.Equal(50, config.PreviousCompletedEpochs);
    }

    [Fact]
    public void TryValidate_ReturnsFalseWhenDataPathMissing()
    {
        var state = new TrainingConfigState { DataPath = null };

        var valid = state.TryValidate(out var error);

        Assert.False(valid);
        Assert.Contains("Select a data file", error);
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
