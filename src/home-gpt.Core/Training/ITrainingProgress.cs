namespace home_gpt.Training;

public sealed record TrainingEpochProgress(
    int CurrentEpoch,
    int TotalEpochs,
    double AverageLoss);

public interface ITrainingProgress
{
    void OnEpochCompleted(TrainingEpochProgress progress);
}
