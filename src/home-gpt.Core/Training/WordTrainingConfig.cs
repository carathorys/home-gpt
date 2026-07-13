namespace home_gpt.Training;

public sealed record WordTrainingConfig(
    string DataPath,
    string OutputDirectory,
    int Epochs = 100,
    int BatchSize = 32,
    double LearningRate = 0.003,
    int HiddenSize = 128,
    int EmbedSize = 64,
    bool ResumeFromCheckpoint = false,
    int PreviousCompletedEpochs = 0);
