using home_gpt.Data;
using home_gpt.Models;
using Spectre.Console;

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

    public Table ToTable(string title = "[bold]Training summary[/]") =>
        BuildTable(title);

    public void Display()
    {
        AnsiConsole.Write(ToTable());
        AnsiConsole.WriteLine();
    }

    private Table BuildTable(string title)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title(title)
            .AddColumn("[dim]Setting[/]")
            .AddColumn("[dim]Value[/]");

        table.AddRow("Data file", Markup.Escape(DataPath));
        table.AddRow("Output directory", Markup.Escape(OutputDirectory));
        table.AddRow("Device", Device);
        table.AddEmptyRow();
        table.AddRow("Words", WordCount.ToString("N0"));
        table.AddRow("Vocabulary size", VocabSize.ToString("N0"));
        table.AddRow("Max word length", MaxWordLength.ToString("N0"));
        table.AddRow("Sequence length", SequenceLength.ToString("N0"));
        table.AddEmptyRow();
        table.AddRow("Epochs", Epochs.ToString("N0"));
        table.AddRow("Batch size", BatchSize.ToString("N0"));
        table.AddRow("Batches per epoch", BatchesPerEpoch.ToString("N0"));
        table.AddRow("Total training steps", TotalSteps.ToString("N0"));
        table.AddRow("Learning rate", LearningRate.ToString("G"));
        table.AddEmptyRow();
        table.AddRow("Embedding size", EmbedSize.ToString("N0"));
        table.AddRow("Hidden size", HiddenSize.ToString("N0"));
        table.AddRow("Parameters", $"{ParameterCount:N0}");
        table.AddRow("Model size (float32)", $"{ParameterSizeMb:F2} MB");

        return table;
    }

    private static long CountParameters(CharLanguageModel model)
    {
        long count = 0;
        foreach (var parameter in model.parameters())
            count += parameter.numel();

        return count;
    }
}
