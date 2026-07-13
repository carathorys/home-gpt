using home_gpt.Data;
using home_gpt.Models;

namespace home_gpt.Training;

public sealed record TrainingSummary(
    string DataPath,
    string OutputDirectory,
    string Device,
    int WordCount,
    int VocabSize,
    int MaxWordLength,
    int SequenceLength,
    int Epochs,
    int BatchSize,
    double LearningRate,
    int HiddenSize,
    int EmbedSize,
    long ParameterCount,
    int BatchesPerEpoch,
    int TotalSteps)
{
    public double ParameterSizeMb => ParameterCount * sizeof(float) / (1024.0 * 1024.0);

    public static TrainingSummary Create(
        WordDataset dataset,
        WordTrainingConfig config,
        CharLanguageModel model,
        string device)
    {
        var batchesPerEpoch = (dataset.Words.Count + config.BatchSize - 1) / config.BatchSize;

        return new TrainingSummary(
            DataPath: config.DataPath,
            OutputDirectory: config.OutputDirectory,
            Device: device,
            WordCount: dataset.Words.Count,
            VocabSize: dataset.Vocab.Size,
            MaxWordLength: dataset.MaxWordLength,
            SequenceLength: dataset.SequenceLength,
            Epochs: config.Epochs,
            BatchSize: config.BatchSize,
            LearningRate: config.LearningRate,
            HiddenSize: config.HiddenSize,
            EmbedSize: config.EmbedSize,
            ParameterCount: CountParameters(model),
            BatchesPerEpoch: batchesPerEpoch,
            TotalSteps: batchesPerEpoch * config.Epochs);
    }

    private static long CountParameters(CharLanguageModel model)
    {
        long count = 0;
        foreach (var parameter in model.parameters())
            count += parameter.numel();

        return count;
    }
}
