using System.Collections.ObjectModel;
using home_gpt.Training;

namespace home_gpt.Avalonia.ViewModels;

public static class TrainingPreviewRows
{
    public static ObservableCollection<PreviewRow> Build(TrainingConfigState state)
    {
        var preview = TrainingPreview.Compute(
            state.DataPath,
            state.OutputDirectory,
            state.Epochs,
            state.BatchSize,
            state.LearningRate,
            state.HiddenSize,
            state.EmbedSize);

        var rows = new ObservableCollection<PreviewRow>();

        if (preview.Summary is not null)
        {
            var summary = preview.Summary;
            rows.Add(new("Data file", summary.DataPath));
            rows.Add(new("Output directory", summary.OutputDirectory));
            rows.Add(new("Device", summary.Device));
            rows.Add(new(string.Empty, string.Empty));
            rows.Add(new("Words", summary.WordCount.ToString("N0")));
            rows.Add(new("Vocabulary size", summary.VocabSize.ToString("N0")));
            rows.Add(new("Max word length", summary.MaxWordLength.ToString("N0")));
            rows.Add(new("Sequence length", summary.SequenceLength.ToString("N0")));
            rows.Add(new(string.Empty, string.Empty));
            rows.Add(new(state.EpochsLabel, summary.Epochs.ToString("N0")));
            rows.Add(new("Batch size", summary.BatchSize.ToString("N0")));
            rows.Add(new("Batches per epoch", summary.BatchesPerEpoch.ToString("N0")));
            rows.Add(new("Total training steps", summary.TotalSteps.ToString("N0")));
            rows.Add(new("Learning rate", summary.LearningRate.ToString("G")));
            rows.Add(new(string.Empty, string.Empty));
            rows.Add(new("Embedding size", summary.EmbedSize.ToString("N0")));
            rows.Add(new("Hidden size", summary.HiddenSize.ToString("N0")));
            rows.Add(new("Parameters", $"{summary.ParameterCount:N0}"));
            rows.Add(new("Model size (float32)", $"{summary.ParameterSizeMb:F2} MB"));
        }
        else
        {
            rows.Add(new("Data file", state.DataPath ?? "(not set)"));
            rows.Add(new("Output directory", state.OutputDirectory));
            rows.Add(new("Device", preview.Device));
            rows.Add(new(string.Empty, string.Empty));
            rows.Add(new("Data", preview.DatasetError ?? "(select a data file)"));
        }

        if (state.LoadedCheckpoint is not null)
        {
            rows.Add(new(string.Empty, string.Empty));
            rows.Add(new("Completed epochs", state.CompletedEpochs.ToString("N0")));
            rows.Add(new(
                "Checkpoint",
                state.IsArchitectureChanged()
                    ? "Loaded, but critical settings changed"
                    : "Loaded and ready to continue"));
        }

        return rows;
    }
}
