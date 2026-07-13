using home_gpt.Training;
using Spectre.Console;

namespace home_gpt.Cli.Training;

public static class TrainingSummaryPresenter
{
    public static void Display(TrainingSummary summary)
    {
        AnsiConsole.Write(ToTable(summary));
        AnsiConsole.WriteLine();
    }

    public static Table ToTable(TrainingSummary summary, string title = "[bold]Training summary[/]")
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title(title)
            .AddColumn("[dim]Setting[/]")
            .AddColumn("[dim]Value[/]");

        table.AddRow("Data file", Markup.Escape(summary.DataPath));
        table.AddRow("Output directory", Markup.Escape(summary.OutputDirectory));
        table.AddRow("Device", summary.Device);
        table.AddEmptyRow();
        table.AddRow("Words", summary.WordCount.ToString("N0"));
        table.AddRow("Vocabulary size", summary.VocabSize.ToString("N0"));
        table.AddRow("Max word length", summary.MaxWordLength.ToString("N0"));
        table.AddRow("Sequence length", summary.SequenceLength.ToString("N0"));
        table.AddEmptyRow();
        table.AddRow("Epochs", summary.Epochs.ToString("N0"));
        table.AddRow("Batch size", summary.BatchSize.ToString("N0"));
        table.AddRow("Batches per epoch", summary.BatchesPerEpoch.ToString("N0"));
        table.AddRow("Total training steps", summary.TotalSteps.ToString("N0"));
        table.AddRow("Learning rate", summary.LearningRate.ToString("G"));
        table.AddEmptyRow();
        table.AddRow("Embedding size", summary.EmbedSize.ToString("N0"));
        table.AddRow("Hidden size", summary.HiddenSize.ToString("N0"));
        table.AddRow("Parameters", $"{summary.ParameterCount:N0}");
        table.AddRow("Model size (float32)", $"{summary.ParameterSizeMb:F2} MB");

        return table;
    }
}
