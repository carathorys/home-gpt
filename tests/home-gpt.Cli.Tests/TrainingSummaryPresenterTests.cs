using home_gpt.Cli.Training;
using home_gpt.Training;

namespace home_gpt.Cli.Tests;

public sealed class TrainingSummaryPresenterTests
{
    [Fact]
    public void ToTable_IncludesCoreSummaryValues()
    {
        var summary = new TrainingSummary(
            DataPath: "/tmp/words.txt",
            OutputDirectory: "/tmp/model",
            Device: "cpu",
            WordCount: 100,
            VocabSize: 10,
            MaxWordLength: 5,
            SequenceLength: 6,
            Epochs: 3,
            BatchSize: 8,
            LearningRate: 0.01,
            HiddenSize: 16,
            EmbedSize: 8,
            ParameterCount: 1234,
            BatchesPerEpoch: 13,
            TotalSteps: 39);

        var table = TrainingSummaryPresenter.ToTable(summary);

        Assert.Equal(2, table.Columns.Count);
        Assert.NotEmpty(table.Rows);
    }
}
