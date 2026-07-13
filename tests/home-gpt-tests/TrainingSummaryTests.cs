using home_gpt.Data;
using home_gpt.Models;
using home_gpt.Training;
using static TorchSharp.torch;

namespace home_gpt_tests;

public sealed class TrainingSummaryTests
{
    [Fact]
    public void Create_ComputesBatchAndStepCounts()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            cat
            dog
            bird
            fish
            frog
            """);
        var dataset = new WordDataset(path);
        var config = new WordTrainingConfig(
            DataPath: path,
            OutputDirectory: fs.CreateDirectory("model"),
            Epochs: 10,
            BatchSize: 2);
        using var model = new CharLanguageModel(dataset.Vocab.Size, config.EmbedSize, config.HiddenSize);

        var summary = TrainingSummary.Create(dataset, config, model, "cpu");

        Assert.Equal(5, summary.WordCount);
        Assert.Equal(3, summary.BatchesPerEpoch);
        Assert.Equal(30, summary.TotalSteps);
    }

    [Fact]
    public void Create_CountsModelParameters()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("words.txt", "cat\n");
        var dataset = new WordDataset(path);
        var config = new WordTrainingConfig(path, fs.CreateDirectory("model"));
        using var model = new CharLanguageModel(dataset.Vocab.Size, embedSize: 8, hiddenSize: 16);

        var summary = TrainingSummary.Create(dataset, config, model, CPU.type.ToString());

        Assert.True(summary.ParameterCount > 0);
        Assert.True(summary.ParameterSizeMb > 0);
    }
}
