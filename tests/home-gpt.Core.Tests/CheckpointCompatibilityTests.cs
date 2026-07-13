using home_gpt.Data;
using home_gpt.Persistence;

namespace home_gpt.Core.Tests;

public sealed class CheckpointCompatibilityTests
{
    [Fact]
    public void ValidateForResume_SucceedsWhenDatasetMatchesCheckpoint()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("words.txt", "cat\ndog\n");
        var dataset = new WordDataset(path);
        var metadata = new ModelMetadata(
            dataset.Vocab.Size,
            EmbedSize: 8,
            HiddenSize: 16,
            dataset.SequenceLength,
            dataset.Vocab.ToJson());

        CheckpointCompatibility.ValidateForResume(dataset, metadata);
    }

    [Fact]
    public void ValidateForResume_ThrowsWhenVocabularyDiffers()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("words.txt", "cat\n");
        var dataset = new WordDataset(path);
        var metadata = new ModelMetadata(
            dataset.Vocab.Size,
            EmbedSize: 8,
            HiddenSize: 16,
            dataset.SequenceLength,
            "\"xyz\"");

        var exception = Assert.Throws<InvalidOperationException>(
            () => CheckpointCompatibility.ValidateForResume(dataset, metadata));

        Assert.Contains("Vocabulary mismatch", exception.Message);
    }
}
