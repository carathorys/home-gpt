using System.Diagnostics.CodeAnalysis;
using home_gpt.Training;
using Spectre.Console;

namespace home_gpt.Cli.Training;

[ExcludeFromCodeCoverage]
public static class WordTrainer
{
    [ExcludeFromCodeCoverage]
    public static void Run(WordTrainingConfig config, CancellationToken cancellationToken = default)
    {
        var preview = TrainingPreview.Compute(
            config.DataPath,
            config.OutputDirectory,
            config.Epochs,
            config.BatchSize,
            config.LearningRate,
            config.HiddenSize,
            config.EmbedSize);

        if (preview.Summary is not null)
            TrainingSummaryPresenter.Display(preview.Summary);

        if (config.ResumeFromCheckpoint)
        {
            AnsiConsole.MarkupLine(
                $"[cyan]Resuming training[/] from epoch [bold]{config.PreviousCompletedEpochs}[/], " +
                $"running [bold]{config.Epochs}[/] more epoch(s).");
            AnsiConsole.WriteLine();
        }

        TrainingResult? result = null;

        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Training[/]", maxValue: config.Epochs);

                result = WordTrainingService.Train(
                    config,
                    new SpectreTrainingProgress(task),
                    cancellationToken);
            });

        if (result is null)
            return;

        AnsiConsole.MarkupLine(
            $"[bold green]Training complete.[/] Model saved to [cyan]{result.OutputDirectory}[/] " +
            $"([dim]{result.CompletedEpochs} total epochs[/])");
    }

    internal sealed class SpectreTrainingProgress(ProgressTask task) : ITrainingProgress
    {
        public void OnEpochCompleted(TrainingEpochProgress progress)
        {
            task.Increment(1);
            task.Description =
                $"[green]Epoch {progress.CurrentEpoch}/{progress.TotalEpochs}[/]  loss={progress.AverageLoss:G6}";
        }
    }
}
