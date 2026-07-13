using home_gpt.Data;
using home_gpt.Models;
using static TorchSharp.torch;

namespace home_gpt.Training;

public sealed class TrainingPreview
{
    private static readonly Dictionary<string, CacheEntry> DatasetCache = new();

    public TrainingSummary? Summary { get; init; }
    public string Device { get; init; } = CPU.ToString();
    public string? DatasetError { get; init; }

    public static TrainingPreview Compute(
        string? dataPath,
        string outputDirectory,
        int epochs,
        int batchSize,
        double learningRate,
        int hiddenSize,
        int embedSize)
    {
        var device = cuda.is_available() ? CUDA : CPU;
        var deviceName = device.ToString();

        if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
        {
            return new TrainingPreview
            {
                Device = deviceName,
                DatasetError = string.IsNullOrWhiteSpace(dataPath)
                    ? null
                    : $"Data file not found: {dataPath}"
            };
        }

        try
        {
            var dataset = GetDataset(dataPath);
            var config = new WordTrainingConfig(
                dataPath,
                outputDirectory,
                epochs,
                batchSize,
                learningRate,
                hiddenSize,
                embedSize);

            using var model = new CharLanguageModel(dataset.Vocab.Size, embedSize, hiddenSize);

            return new TrainingPreview
            {
                Summary = TrainingSummary.Create(dataset, config, model, deviceName),
                Device = deviceName
            };
        }
        catch (Exception ex)
        {
            return new TrainingPreview
            {
                Device = deviceName,
                DatasetError = ex.Message
            };
        }
    }

    private static WordDataset GetDataset(string path)
    {
        var ticks = File.GetLastWriteTimeUtc(path).Ticks;

        if (DatasetCache.TryGetValue(path, out var entry) && entry.LastWriteTicks == ticks)
            return entry.Dataset;

        var dataset = new WordDataset(path);
        DatasetCache[path] = new CacheEntry(ticks, dataset);
        return dataset;
    }

    private sealed record CacheEntry(long LastWriteTicks, WordDataset Dataset);
}
