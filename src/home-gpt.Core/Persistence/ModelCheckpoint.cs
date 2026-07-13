using System.Text.Json;

namespace home_gpt.Persistence;

public static class ModelCheckpoint
{
    public const string WeightsFileName = "model.pt";
    public const string MetadataFileName = "metadata.json";

    public static string WeightsPath(string directory) => Path.Combine(directory, WeightsFileName);
    public static string MetadataPath(string directory) => Path.Combine(directory, MetadataFileName);

    public static void Save(string directory, ModelMetadata metadata)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(MetadataPath(directory), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    public static ModelMetadata LoadMetadata(string directory)
    {
        var path = MetadataPath(directory);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Model metadata not found at '{path}'.");

        return JsonSerializer.Deserialize<ModelMetadata>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Could not read metadata from '{path}'.");
    }

    public static bool Exists(string directory) =>
        File.Exists(WeightsPath(directory)) && File.Exists(MetadataPath(directory));

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
