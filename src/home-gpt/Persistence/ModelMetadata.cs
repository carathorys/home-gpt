namespace home_gpt.Persistence;

public sealed record ModelMetadata(
    int VocabSize,
    int EmbedSize,
    int HiddenSize,
    int SequenceLength,
    string VocabJson,
    string DataPath = "",
    int CompletedEpochs = 0,
    int BatchSize = 0,
    double LearningRate = 0);
