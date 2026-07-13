using home_gpt.Data;
using home_gpt.Persistence;

namespace home_gpt.Training;

public sealed record CheckpointSessionState(
    bool ResumeFromCheckpoint,
    int CompletedEpochs,
    bool IsArchitectureChanged,
    bool CanResume);

public static class CheckpointSessionResolver
{
    public static void ApplyMetadata(
        ModelMetadata metadata,
        Action<string?> setDataPath,
        Action<int> setEmbedSize,
        Action<int> setHiddenSize,
        Action<int> setCompletedEpochs,
        Action<int> setBatchSize,
        Action<double> setLearningRate)
    {
        setEmbedSize(metadata.EmbedSize);
        setHiddenSize(metadata.HiddenSize);
        setCompletedEpochs(metadata.CompletedEpochs);

        if (metadata.BatchSize > 0)
            setBatchSize(metadata.BatchSize);

        if (metadata.LearningRate > 0)
            setLearningRate(metadata.LearningRate);

        if (!string.IsNullOrWhiteSpace(metadata.DataPath) && File.Exists(metadata.DataPath))
            setDataPath(metadata.DataPath);
    }

    public static bool IsArchitectureChanged(ModelMetadata? checkpoint, int embedSize, int hiddenSize) =>
        checkpoint is not null &&
        (hiddenSize != checkpoint.HiddenSize || embedSize != checkpoint.EmbedSize);

    public static CheckpointSessionState Resolve(
        ModelMetadata? checkpoint,
        string? dataPath,
        int embedSize,
        int hiddenSize)
    {
        if (checkpoint is null)
            return new CheckpointSessionState(false, 0, false, false);

        var architectureChanged = IsArchitectureChanged(checkpoint, embedSize, hiddenSize);
        if (architectureChanged)
        {
            return new CheckpointSessionState(
                false,
                checkpoint.CompletedEpochs,
                true,
                false);
        }

        if (CanResume(checkpoint, dataPath))
        {
            return new CheckpointSessionState(
                true,
                checkpoint.CompletedEpochs,
                false,
                true);
        }

        return new CheckpointSessionState(
            false,
            checkpoint.CompletedEpochs,
            false,
            false);
    }

    public static bool CanResume(ModelMetadata checkpoint, string? dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
            return false;

        try
        {
            var dataset = new WordDataset(dataPath);
            CheckpointCompatibility.ValidateForResume(dataset, checkpoint);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
