using home_gpt.Data;
using home_gpt.Models;
using home_gpt.Persistence;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace home_gpt.Training;

public static class WordTrainingService
{
    public static TrainingResult Train(
        WordTrainingConfig config,
        ITrainingProgress? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dataset = new WordDataset(config.DataPath);
        var device = TorchDevice.Select();
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

        var summary = TrainingSummary.Create(dataset, config, model, deviceName);

        using var lossFn = CrossEntropyLoss(ignore_index: CharVocab.PadIndex);
        using var optimizer = optim.Adam(model.parameters(), config.LearningRate);

        var totalEpochs = config.PreviousCompletedEpochs + config.Epochs;

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
                using var x = BatchTensorBuilder.FromRows(batchInputs).to(device);
                using var y = BatchTensorBuilder.FromRows(batchTargets).to(device);

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

            var avgLoss = epochLoss / batchCount;
            var currentEpoch = config.PreviousCompletedEpochs + epoch + 1;
            progress?.OnEpochCompleted(new TrainingEpochProgress(currentEpoch, totalEpochs, avgLoss));
        }

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

        return new TrainingResult(config.OutputDirectory, completedEpochs, summary);
    }
}
