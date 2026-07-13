using home_gpt.Data;
using home_gpt.Models;
using home_gpt.Persistence;
using Spectre.Console;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace home_gpt.Training;

public static class WordTrainer
{
    public static void Run(WordTrainingConfig config, CancellationToken cancellationToken = default)
    {
        var dataset = new WordDataset(config.DataPath);
        var device = cuda.is_available() ? CUDA : CPU;
        var deviceName = device.ToString();

        if (config.ResumeFromCheckpoint)
        {
            var checkpoint = ModelCheckpoint.LoadMetadata(config.OutputDirectory);
            CheckpointCompatibility.ValidateForResume(dataset, checkpoint);

            if (checkpoint.EmbedSize != config.EmbedSize || checkpoint.HiddenSize != config.HiddenSize)
            {
                throw new InvalidOperationException(
                    "Model architecture does not match the checkpoint. Reload settings from the checkpoint.");
            }
        }

        using var model = new CharLanguageModel(
            dataset.Vocab.Size,
            config.EmbedSize,
            config.HiddenSize);
        model.to(device);

        if (config.ResumeFromCheckpoint)
            model.load(ModelCheckpoint.WeightsPath(config.OutputDirectory));

        TrainingSummary
            .Create(dataset, config, model, deviceName)
            .Display();

        if (config.ResumeFromCheckpoint)
        {
            AnsiConsole.MarkupLine(
                $"[cyan]Resuming training[/] from epoch [bold]{config.PreviousCompletedEpochs}[/], " +
                $"running [bold]{config.Epochs}[/] more epoch(s).");
            AnsiConsole.WriteLine();
        }

        using var lossFn = CrossEntropyLoss(ignore_index: CharVocab.PadIndex);
        using var optimizer = optim.Adam(model.parameters(), config.LearningRate);

        var totalEpochs = config.PreviousCompletedEpochs + config.Epochs;

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

                for (var epoch = 0; epoch < config.Epochs; epoch++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    model.train();
                    var epochLoss = 0.0;
                    var batchCount = 0;

                    for (var start = 0; start < dataset.Words.Count; start += config.BatchSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var (batchInputs, batchTargets) = dataset.GetBatch(start, config.BatchSize);
                        using var x = BatchToTensor(batchInputs).to(device);
                        using var y = BatchToTensor(batchTargets).to(device);

                        optimizer.zero_grad();
                        using var logits = model.call(x);
                        using var loss = lossFn.call(
                            logits.view(-1, dataset.Vocab.Size),
                            y.view(-1));
                        loss.backward();
                        optimizer.step();

                        epochLoss += loss.item<float>();
                        batchCount++;
                    }

                    task.Increment(1);
                    var avgLoss = epochLoss / batchCount;
                    var currentEpoch = config.PreviousCompletedEpochs + epoch + 1;
                    task.Description =
                        $"[green]Epoch {currentEpoch}/{totalEpochs}[/]  loss={avgLoss:G6}";
                }
            });

        Directory.CreateDirectory(config.OutputDirectory);
        model.save(ModelCheckpoint.WeightsPath(config.OutputDirectory));

        var completedEpochs = config.PreviousCompletedEpochs + config.Epochs;
        var metadata = new ModelMetadata(
            dataset.Vocab.Size,
            config.EmbedSize,
            config.HiddenSize,
            dataset.SequenceLength,
            dataset.Vocab.ToJson(),
            config.DataPath,
            completedEpochs,
            config.BatchSize,
            config.LearningRate);
        ModelCheckpoint.Save(config.OutputDirectory, metadata);

        AnsiConsole.MarkupLine(
            $"[bold green]Training complete.[/] Model saved to [cyan]{config.OutputDirectory}[/] " +
            $"([dim]{completedEpochs} total epochs[/])");
    }

    private static Tensor BatchToTensor(long[][] rows)
    {
        var batch = rows.Length;
        var seqLen = rows[0].Length;
        var flat = new long[batch * seqLen];

        for (var b = 0; b < batch; b++)
        for (var s = 0; s < seqLen; s++)
            flat[b * seqLen + s] = rows[b][s];

        return tensor(flat, new long[] { batch, seqLen }, dtype: ScalarType.Int64);
    }
}
