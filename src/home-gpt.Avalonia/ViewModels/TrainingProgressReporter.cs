using home_gpt.Training;

namespace home_gpt.Avalonia.ViewModels;

public static class TrainingProgressReporter
{
    public static (double Percent, string Status) Format(
        TrainingEpochProgress progress,
        int epochsThisRun,
        int previousCompleted)
    {
        var completedThisRun = progress.CurrentEpoch - previousCompleted;
        var percent = completedThisRun / (double)epochsThisRun * 100;
        var status =
            $"Epoch {progress.CurrentEpoch}/{progress.TotalEpochs}  loss={progress.AverageLoss:G6}";
        return (percent, status);
    }
}
