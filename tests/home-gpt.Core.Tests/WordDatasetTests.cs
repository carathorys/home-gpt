using home_gpt.Data;

namespace home_gpt.Core.Tests;

public sealed class WordDatasetTests
{
    [Fact]
    public void Constructor_IgnoresCommentsAndBlankLines_AndPreservesCharacters()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            # comment

              Café
            abál
            HELLO
            
            """);

        var dataset = new WordDataset(path);

        Assert.Equal(["Café", "abál", "HELLO"], dataset.Words);
        Assert.Equal(5, dataset.MaxWordLength);
        Assert.Equal(6, dataset.SequenceLength);
    }

    [Fact]
    public void Constructor_PreservesSpecialCharacters()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            addig-addig
            l'amour
            de.
            D-dúr
            hello!
            """);

        var dataset = new WordDataset(path);

        Assert.Equal(["addig-addig", "l'amour", "de.", "D-dúr", "hello!"], dataset.Words);
        Assert.True(dataset.Vocab.Contains('-'));
        Assert.True(dataset.Vocab.Contains('.'));
        Assert.True(dataset.Vocab.Contains('!'));
        Assert.True(dataset.Vocab.Contains('ú'));
    }

    [Fact]
    public void Constructor_ThrowsWhenNoUsableWordsExist()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "empty.txt",
            """
            # comment


            """);

        var exception = Assert.Throws<InvalidDataException>(() => new WordDataset(path));

        Assert.Contains("No words found", exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsForControlCharacters()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("bad.txt", "hel\tlo\n");

        var exception = Assert.Throws<InvalidDataException>(() => new WordDataset(path));

        Assert.Contains("control characters", exception.Message);
    }

    [Fact]
    public void GetSequence_ShiftsTargetsToPredictNextCharacter()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile("words.txt", "cat\n");
        var dataset = new WordDataset(path);

        var (input, target) = dataset.GetSequence("cat");

        Assert.Equal(dataset.SequenceLength, input.Length);
        Assert.Equal(dataset.SequenceLength, target.Length);
        Assert.Equal(dataset.Vocab.Encode('c'), input[0]);
        Assert.Equal(dataset.Vocab.Encode('a'), input[1]);
        Assert.Equal(dataset.Vocab.Encode('t'), input[2]);
        Assert.Equal(CharVocab.PadIndex, input[3]);
        Assert.Equal(dataset.Vocab.Encode('a'), target[0]);
        Assert.Equal(dataset.Vocab.Encode('t'), target[1]);
        Assert.Equal(CharVocab.EosIndex, target[2]);
        Assert.Equal(CharVocab.PadIndex, target[3]);
    }

    [Fact]
    public void GetBatch_ReturnsRequestedSlice()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            cat
            dog
            bird
            """);
        var dataset = new WordDataset(path);

        var (inputs, targets) = dataset.GetBatch(1, 2);

        Assert.Equal(2, inputs.Length);
        Assert.Equal(2, targets.Length);
        Assert.Equal(dataset.Vocab.Encode('d'), inputs[0][0]);
        Assert.Equal(dataset.Vocab.Encode('b'), inputs[1][0]);
    }

    [Fact]
    public void GetBatch_TruncatesWhenRequestRunsPastEnd()
    {
        using var fs = new TestFileSystem();
        var path = fs.CreateFile(
            "words.txt",
            """
            cat
            dog
            """);
        var dataset = new WordDataset(path);

        var (inputs, targets) = dataset.GetBatch(1, 10);

        Assert.Single(inputs);
        Assert.Single(targets);
        Assert.Equal(dataset.Vocab.Encode('d'), inputs[0][0]);
    }
}
