namespace home_gpt.Training;

public sealed record TrainingResult(
    string OutputDirectory,
    int CompletedEpochs,
    TrainingSummary Summary);
