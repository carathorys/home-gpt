using home_gpt.Persistence;
using home_gpt.Training;

namespace home_gpt.Training;

public sealed class TrainingConfigState
{
    public const string DefaultModelDirectory = "models/word-model";

    private ModelMetadata? _loadedCheckpoint;

    public string? DataPath { get; set; }
    public string OutputDirectory { get; set; } = DefaultModelDirectory;
    public int Epochs { get; set; } = 100;
    public int BatchSize { get; set; } = 32;
    public double LearningRate { get; set; } = 0.003;
    public int HiddenSize { get; set; } = 128;
    public int EmbedSize { get; set; } = 64;
    public int CompletedEpochs { get; private set; }
    public bool ResumeFromCheckpoint { get; private set; }
    public ModelMetadata? LoadedCheckpoint => _loadedCheckpoint;

    public TrainingConfigState()
    {
        TryLoadCheckpointFromOutputDirectory();
    }

    public string EpochsLabel => ResumeFromCheckpoint ? "Additional epochs" : "Epochs";

    public WordTrainingConfig ToConfig() =>
        new(
            DataPath!,
            OutputDirectory,
            Epochs,
            BatchSize,
            LearningRate,
            HiddenSize,
            EmbedSize,
            ResumeFromCheckpoint,
            CompletedEpochs);

    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(DataPath))
        {
            error = "Select a data file before starting training.";
            return false;
        }

        if (!File.Exists(DataPath))
        {
            error = $"Data file not found: {DataPath}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public void TryLoadCheckpointFromOutputDirectory()
    {
        try
        {
            if (!ModelCheckpoint.Exists(OutputDirectory))
            {
                ClearCheckpointState();
                return;
            }

            var metadata = ModelCheckpoint.LoadMetadata(OutputDirectory);
            ApplyCheckpointMetadata(metadata);
            _loadedCheckpoint = metadata;
            ApplySessionState();
        }
        catch
        {
            ClearCheckpointState();
        }
    }

    public void ApplySessionState()
    {
        var state = CheckpointSessionResolver.Resolve(_loadedCheckpoint, DataPath, EmbedSize, HiddenSize);
        ResumeFromCheckpoint = state.ResumeFromCheckpoint;
        CompletedEpochs = state.CompletedEpochs;
    }

    public bool IsArchitectureChanged() =>
        CheckpointSessionResolver.IsArchitectureChanged(_loadedCheckpoint, EmbedSize, HiddenSize);

    public void ClearResume()
    {
        ResumeFromCheckpoint = false;
        CompletedEpochs = 0;
    }

    public void EnableResume()
    {
        ResumeFromCheckpoint = true;
        CompletedEpochs = _loadedCheckpoint?.CompletedEpochs ?? 0;
    }

    private void ApplyCheckpointMetadata(ModelMetadata metadata)
    {
        CheckpointSessionResolver.ApplyMetadata(
            metadata,
            path => DataPath = path,
            value => EmbedSize = value,
            value => HiddenSize = value,
            value => CompletedEpochs = value,
            value => BatchSize = value,
            value => LearningRate = value);
    }

    private void ClearCheckpointState()
    {
        _loadedCheckpoint = null;
        ResumeFromCheckpoint = false;
        CompletedEpochs = 0;
    }
}
