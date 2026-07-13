using System.Diagnostics.CodeAnalysis;
using home_gpt.Cli.Inference;
using home_gpt.Cli.Training;
using home_gpt.Cli.Ui;
using home_gpt.Inference;
using Spectre.Console;
using static TorchSharp.torch;

namespace home_gpt;

[ExcludeFromCodeCoverage]
public static class App
{
    private const string DefaultModelDirectory = "models/word-model";

    public static void Run()
    {
        AnsiConsole.Write(new FigletText("home-gpt").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[dim]TorchSharp character-level word model[/]");
        AnsiConsole.MarkupLine(
            cuda.is_available()
                ? "[green]CUDA is available.[/]"
                : "[yellow]CUDA not detected — using CPU.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .AddChoices("Train model", "Generate words", "Exit"));

            if (action == "Exit")
                break;

            try
            {
                switch (action)
                {
                    case "Train model":
                        RunTraining();
                        break;
                    case "Generate words":
                        RunGeneration();
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine("[dim]Goodbye.[/]");
    }

    private static void RunTraining()
    {
        var editor = new TrainingConfigEditor();

        while (true)
        {
            if (editor.Run() == TrainingEditorResult.BackToMainMenu)
                return;

            using var trainingCts = new CancellationTokenSource();
            using var escapeMonitor = EscapeKeyMonitor.Start(trainingCts);

            try
            {
                AnsiConsole.MarkupLine(
                    "[dim]Press [yellow]Esc[/] during training to cancel and return to configuration.[/]");
                AnsiConsole.WriteLine();

                WordTrainer.Run(editor.ToConfig(), trainingCts.Token);
                return;
            }
            catch (OperationCanceledException) when (trainingCts.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine("[yellow]Training cancelled.[/]");
                AnsiConsole.WriteLine();
            }
        }
    }

    private static void RunGeneration()
    {
        var modelDirectory = ResolveModelDirectoryForGeneration();

        if (modelDirectory is null)
        {
            AnsiConsole.MarkupLine("[yellow]No model directory selected. Train a model first.[/]");
            return;
        }

        using var generator = WordGeneratorInteractive.Load(modelDirectory);
        generator.RunInteractive();
    }

    private static string? ResolveModelDirectoryForGeneration()
    {
        if (Directory.Exists(DefaultModelDirectory))
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select trained model directory[/]")
                    .AddChoices(
                        $"Use default: {DefaultModelDirectory}",
                        "Choose a different directory",
                        "Back to main menu"));

            return choice switch
            {
                "Choose a different directory" => FilePicker.PromptForDirectory("Select trained model directory"),
                "Back to main menu" => null,
                _ => DefaultModelDirectory
            };
        }

        return FilePicker.PromptForDirectory("Select trained model directory");
    }
}
