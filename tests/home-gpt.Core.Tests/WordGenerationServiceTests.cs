using home_gpt.Data;
using home_gpt.Inference;
using home_gpt.Models;
using home_gpt.Persistence;
using static TorchSharp.torch;

namespace home_gpt.Core.Tests;

public sealed class WordGenerationServiceTests
{
    [Fact]
    public void Load_ThrowsWhenCheckpointFilesAreMissing()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("missing-model");

        var exception = Assert.Throws<FileNotFoundException>(() => WordGenerationService.Load(directory));

        Assert.Contains("No trained model found", exception.Message);
    }

    [Fact]
    public void Load_CanReadSavedModelAndGenerateTrimmedPrefix()
    {
        using var fs = new TestFileSystem();
        var directory = fs.CreateDirectory("model");
        var vocab = CharVocab.FromWords(["abal"]);
        var metadata = new ModelMetadata(
            vocab.Size,
            EmbedSize: 4,
            HiddenSize: 8,
            SequenceLength: 5,
            VocabJson: vocab.ToJson());

        using (var model = new CharLanguageModel(metadata.VocabSize, metadata.EmbedSize, metadata.HiddenSize))
        {
            model.save(ModelCheckpoint.WeightsPath(directory));
        }

        ModelCheckpoint.Save(directory, metadata);

        using var generator = WordGenerationService.Load(directory);
        var generated = generator.Generate("  abal  ", temperature: 0);

        Assert.Equal("abal", generated);
    }

    [Fact]
    public void Generate_AllowsEmptyPrefix()
    {
        using var generator = CreateGenerator(["cat"], maxLength: 4);

        var generated = generator.Generate("   ");

        Assert.NotNull(generated);
    }

    [Fact]
    public void Generate_AcceptsSpecialCharactersInPrefixWhenTrained()
    {
        using var generator = CreateGenerator(["D-dúr"], maxLength: 5);

        var generated = generator.Generate("D-dúr");

        Assert.Equal("D-dúr", generated);
    }

    [Fact]
    public void Generate_AcceptsPartialPrefixWithSpecialCharacters()
    {
        using var generator = CreateGenerator(["D-dúr"], maxLength: 5);

        var generated = generator.Generate("D-d");

        Assert.StartsWith("D-d", generated);
    }

    [Fact]
    public void Generate_ThrowsForCharactersOutsideTrainingVocabulary()
    {
        using var generator = CreateGenerator(["cat"], maxLength: 4);

        var exception = Assert.Throws<ArgumentException>(() => generator.Generate("dog"));

        Assert.Contains("was not seen during training", exception.Message);
    }

    [Fact]
    public void Generate_ReturnsTrimmedPrefixWhenAlreadyAtMaxLength()
    {
        using var generator = CreateGenerator(["abal"], maxLength: 4);

        var generated = generator.Generate("  abal  ");

        Assert.Equal("abal", generated);
    }

    [Fact]
    public void Generate_StripsWhitespaceBeforeValidation()
    {
        using var generator = CreateGenerator(["cat"], maxLength: 3);

        var generated = generator.Generate("  cat ");

        Assert.Equal("cat", generated);
    }

    [Fact]
    public void SampleIndex_UsesArgmaxWhenTemperatureIsZero()
    {
        using var logits = tensor([1f, 5f, 2f]);

        var index = WordGenerationService.SampleIndex(logits, temperature: 0);

        Assert.Equal(1L, index);
    }

    private static WordGenerationService CreateGenerator(IEnumerable<string> words, int maxLength)
    {
        var vocab = CharVocab.FromWords(words);
        var model = new CharLanguageModel(vocab.Size, embedSize: 4, hiddenSize: 8);
        return WordGenerationService.CreateForTesting(model, vocab, CPU, maxLength);
    }
}
