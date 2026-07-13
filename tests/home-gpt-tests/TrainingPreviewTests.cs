using home_gpt.Training;

namespace home_gpt_tests;

public sealed class TrainingPreviewTests
{
    [Fact]
    public void Compute_ReturnsSummaryWhenDataFileExists()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            cat
            dog
            bird
            """);

        var preview = TrainingPreview.Compute(
            path,
            fs.CreateDirectory("model"),
            epochs: 5,
            batchSize: 2,
            learningRate: 0.01,
            hiddenSize: 16,
            embedSize: 8);

        Assert.NotNull(preview.Summary);
        Assert.Equal(3, preview.Summary.WordCount);
        Assert.Equal(5, preview.Summary.Epochs);
        Assert.Equal(2, preview.Summary.BatchSize);
    }

    [Fact]
    public void Compute_ReturnsPartialPreviewWhenDataFileMissing()
    {
        var preview = TrainingPreview.Compute(
            "/missing/words.txt",
            "models/out",
            epochs: 10,
            batchSize: 32,
            learningRate: 0.003,
            hiddenSize: 128,
            embedSize: 64);

        Assert.Null(preview.Summary);
        Assert.Contains("not found", preview.DatasetError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_UpdatesWhenNumericSettingsChange()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("words.txt", "cat\ndog\nbird\nfish\n");

        var preview = TrainingPreview.Compute(path, "model", epochs: 10, batchSize: 2, 0.003, 16, 8);
        Assert.Equal(2, preview.Summary!.BatchesPerEpoch);
        Assert.Equal(20, preview.Summary.TotalSteps);

        var updated = TrainingPreview.Compute(path, "model", epochs: 10, batchSize: 5, 0.003, 16, 8);
        Assert.Equal(1, updated.Summary!.BatchesPerEpoch);
        Assert.Equal(10, updated.Summary.TotalSteps);
    }
}
