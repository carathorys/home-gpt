using home_gpt.Training;
using home_gpt.Ui;
using home_gpt.Persistence;

namespace home_gpt_tests;

public sealed class TrainingConfigEditorTests
{
    [Fact]
    public void ToConfig_UsesCurrentEditorValues()
    {
        var editor = new TrainingConfigEditor();
        SetProperty(editor, nameof(TrainingConfigEditor.DataPath), "/tmp/words.txt");
        SetProperty(editor, nameof(TrainingConfigEditor.OutputDirectory), "/tmp/model");
        SetProperty(editor, nameof(TrainingConfigEditor.Epochs), 25);
        SetProperty(editor, nameof(TrainingConfigEditor.BatchSize), 16);
        SetProperty(editor, nameof(TrainingConfigEditor.LearningRate), 0.01);
        SetProperty(editor, nameof(TrainingConfigEditor.HiddenSize), 64);
        SetProperty(editor, nameof(TrainingConfigEditor.EmbedSize), 32);

        var config = editor.ToConfig();

        Assert.Equal("/tmp/words.txt", config.DataPath);
        Assert.Equal("/tmp/model", config.OutputDirectory);
        Assert.Equal(25, config.Epochs);
        Assert.Equal(16, config.BatchSize);
        Assert.Equal(0.01, config.LearningRate);
        Assert.Equal(64, config.HiddenSize);
        Assert.Equal(32, config.EmbedSize);
    }

    [Fact]
    public void Constructor_LoadsCheckpointMetadataFromDefaultModelDirectory()
    {
        using var fs = new TestFileSystem();
        var originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(fs.RootPath);

        try
        {
            var modelDirectory = fs.CreateDirectory("models/word-model");
            var metadata = new ModelMetadata(
                VocabSize: 12,
                EmbedSize: 24,
                HiddenSize: 48,
                SequenceLength: 10,
                VocabJson: "\"abc\"",
                DataPath: "/tmp/words.txt",
                CompletedEpochs: 75,
                BatchSize: 16,
                LearningRate: 0.02);
            ModelCheckpoint.Save(modelDirectory, metadata);
            File.WriteAllText(ModelCheckpoint.WeightsPath(modelDirectory), "weights");

            var editor = new TrainingConfigEditor();

            Assert.Equal("models/word-model", editor.OutputDirectory);
            Assert.Equal(24, editor.EmbedSize);
            Assert.Equal(48, editor.HiddenSize);
            Assert.Equal(16, editor.BatchSize);
            Assert.Equal(0.02, editor.LearningRate);
            Assert.Equal(75, editor.CompletedEpochs);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void ToConfig_DisablesResumeWhenCheckpointCriticalSettingsChanged()
    {
        var editor = new TrainingConfigEditor();
        SetField(editor, "_loadedCheckpoint", new ModelMetadata(
            VocabSize: 12,
            EmbedSize: 32,
            HiddenSize: 64,
            SequenceLength: 10,
            VocabJson: "\"abc\"",
            CompletedEpochs: 50));
        SetProperty(editor, nameof(TrainingConfigEditor.DataPath), "/tmp/words.txt");
        SetProperty(editor, nameof(TrainingConfigEditor.OutputDirectory), "/tmp/model");
        SetProperty(editor, nameof(TrainingConfigEditor.Epochs), 25);
        SetProperty(editor, nameof(TrainingConfigEditor.BatchSize), 16);
        SetProperty(editor, nameof(TrainingConfigEditor.LearningRate), 0.01);
        SetProperty(editor, nameof(TrainingConfigEditor.HiddenSize), 128);
        SetProperty(editor, nameof(TrainingConfigEditor.EmbedSize), 32);

        InvokeMethod(editor, "UpdateResumeStateFromCheckpoint");
        var config = editor.ToConfig();

        Assert.False(config.ResumeFromCheckpoint);
        Assert.Equal(50, config.PreviousCompletedEpochs);
    }

    private static void SetProperty(TrainingConfigEditor editor, string propertyName, object value)
    {
        var property = typeof(TrainingConfigEditor).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");

        property.SetValue(editor, value);
    }

    private static void SetField(TrainingConfigEditor editor, string fieldName, object? value)
    {
        var field = typeof(TrainingConfigEditor).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found.");

        field.SetValue(editor, value);
    }

    private static void InvokeMethod(TrainingConfigEditor editor, string methodName)
    {
        var method = typeof(TrainingConfigEditor).GetMethod(
            methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");

        method.Invoke(editor, []);
    }
}
