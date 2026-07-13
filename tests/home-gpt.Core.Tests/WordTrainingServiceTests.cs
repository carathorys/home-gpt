using home_gpt.Training;

namespace home_gpt.Core.Tests;

public sealed class WordTrainingServiceTests
{
    [Fact]
    public void Train_SavesCheckpointAfterSingleEpoch()
    {
        using var fs = new TestFileSystem();
        var dataPath = fs.CreateFile("words.txt", "cat\ndog\n");
        var outputDirectory = fs.CreateDirectory("model");

        var result = WordTrainingService.Train(
            new WordTrainingConfig(
                dataPath,
                outputDirectory,
                Epochs: 1,
                BatchSize: 2,
                LearningRate: 0.01,
                HiddenSize: 8,
                EmbedSize: 4));

        Assert.Equal(1, result.CompletedEpochs);
        Assert.Equal(outputDirectory, result.OutputDirectory);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "model.pt")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "metadata.json")));
    }
}
