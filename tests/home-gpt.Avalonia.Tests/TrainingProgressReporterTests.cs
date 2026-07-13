using home_gpt.Avalonia.ViewModels;
using home_gpt.Training;

namespace home_gpt.Avalonia.Tests;

public sealed class TrainingProgressReporterTests
{
    [Fact]
    public void Format_ComputesPercentAndStatusForFirstEpoch()
    {
        var progress = new TrainingEpochProgress(1, 10, 0.123456);

        var (percent, status) = TrainingProgressReporter.Format(progress, epochsThisRun: 5, previousCompleted: 0);

        Assert.Equal(20, percent);
        Assert.Contains("Epoch 1/10", status);
        Assert.Contains("loss=0.123456", status);
    }

    [Fact]
    public void Format_ComputesPercentWhenResumingFromCheckpoint()
    {
        var progress = new TrainingEpochProgress(12, 20, 0.5);

        var (percent, status) = TrainingProgressReporter.Format(progress, epochsThisRun: 10, previousCompleted: 10);

        Assert.Equal(20, percent);
        Assert.Contains("Epoch 12/20", status);
    }
}
