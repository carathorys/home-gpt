using home_gpt.Data;

namespace home_gpt.Persistence;

public static class CheckpointCompatibility
{
    public static void ValidateForResume(WordDataset dataset, ModelMetadata metadata)
    {
        if (dataset.Vocab.Size != metadata.VocabSize)
        {
            throw new InvalidOperationException(
                $"Vocabulary size mismatch: dataset has {dataset.Vocab.Size} tokens, " +
                $"checkpoint expects {metadata.VocabSize}.");
        }

        if (dataset.Vocab.ToJson() != metadata.VocabJson)
        {
            throw new InvalidOperationException(
                "Vocabulary mismatch: the selected data file uses different characters than the checkpoint.");
        }

        if (dataset.SequenceLength != metadata.SequenceLength)
        {
            throw new InvalidOperationException(
                $"Sequence length mismatch: dataset needs {dataset.SequenceLength}, " +
                $"checkpoint expects {metadata.SequenceLength}. " +
                "Use the same training data or train from scratch.");
        }
    }
}
