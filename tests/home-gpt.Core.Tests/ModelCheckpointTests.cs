using home_gpt.Persistence;

namespace home_gpt.Core.Tests;

public sealed class ModelCheckpointTests
{
    [Fact]
    public void PathHelpers_ReturnExpectedFileNames()
    {
        var directory = "/tmp/model";

        Assert.EndsWith("model.pt", ModelCheckpoint.WeightsPath(directory));
        Assert.EndsWith("metadata.json", ModelCheckpoint.MetadataPath(directory));
    }

    [Fact]
    public void Save_AndLoadMetadata_RoundTripValues()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");
        var metadata = new ModelMetadata(
            12, 8, 16, 20, "\"abc\"",
            DataPath: "/tmp/words.txt",
            CompletedEpochs: 50,
            BatchSize: 16,
            LearningRate: 0.01);

        ModelCheckpoint.Save(directory, metadata);

        var loaded = ModelCheckpoint.LoadMetadata(directory);

        Assert.Equal(metadata, loaded);
    }

    [Fact]
    public void Exists_ReturnsFalseWhenOnlyMetadataExists()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");

        ModelCheckpoint.Save(directory, new ModelMetadata(1, 2, 3, 4, "\"a\""));

        Assert.False(ModelCheckpoint.Exists(directory));
    }

    [Fact]
    public void Exists_ReturnsTrueWhenWeightsAndMetadataExist()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");

        ModelCheckpoint.Save(directory, new ModelMetadata(1, 2, 3, 4, "\"a\""));
        File.WriteAllText(ModelCheckpoint.WeightsPath(directory), "weights");

        Assert.True(ModelCheckpoint.Exists(directory));
    }

    [Fact]
    public void Save_AndLoadMetadata_RoundTripsLegacyMetadataWithoutNewFields()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");
        File.WriteAllText(
            ModelCheckpoint.MetadataPath(directory),
            """
            {
              "VocabSize": 5,
              "EmbedSize": 4,
              "HiddenSize": 8,
              "SequenceLength": 6,
              "VocabJson": "\"ab\""
            }
            """);

        var loaded = ModelCheckpoint.LoadMetadata(directory);

        Assert.Equal(5, loaded.VocabSize);
        Assert.Equal(4, loaded.EmbedSize);
        Assert.Equal(8, loaded.HiddenSize);
        Assert.Equal(6, loaded.SequenceLength);
        Assert.Equal("\"ab\"", loaded.VocabJson);
        Assert.Equal("", loaded.DataPath);
        Assert.Equal(0, loaded.CompletedEpochs);
    }

    [Fact]
    public void LoadMetadata_ThrowsWhenMetadataFileIsMissing()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");

        var exception = Assert.Throws<FileNotFoundException>(() => ModelCheckpoint.LoadMetadata(directory));

        Assert.Contains("Model metadata not found", exception.Message);
    }
}
