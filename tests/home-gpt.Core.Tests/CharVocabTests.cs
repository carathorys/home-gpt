using home_gpt.Data;

namespace home_gpt.Core.Tests;

public sealed class CharVocabTests
{
    [Fact]
    public void FromWords_BuildsSortedUniqueVocabulary()
    {
        var vocab = CharVocab.FromWords(["cab", "dad"]);

        Assert.Equal(6, vocab.Size);
        Assert.True(vocab.Contains('a'));
        Assert.True(vocab.Contains('b'));
        Assert.True(vocab.Contains('c'));
        Assert.True(vocab.Contains('d'));

        Assert.Equal(2, vocab.Encode('a'));
        Assert.Equal(3, vocab.Encode('b'));
        Assert.Equal(4, vocab.Encode('c'));
        Assert.Equal(5, vocab.Encode('d'));
    }

    [Fact]
    public void ToJson_AndFromJson_RoundTripVocabulary()
    {
        var original = CharVocab.FromWords(["zebra", "apple"]);

        var json = original.ToJson();
        var roundTrip = CharVocab.FromJson(json);

        Assert.Equal(original.Size, roundTrip.Size);
        Assert.Equal(original.Encode('a'), roundTrip.Encode('a'));
        Assert.Equal(original.Encode('l'), roundTrip.Encode('l'));
        Assert.Equal('z', roundTrip.Decode(roundTrip.Encode('z')));
    }

    [Fact]
    public void EncodeWord_EncodesEachCharacterInOrder()
    {
        var vocab = CharVocab.FromWords(["word"]);

        var encoded = vocab.EncodeWord("word");

        Assert.Equal(
            [vocab.Encode('w'), vocab.Encode('o'), vocab.Encode('r'), vocab.Encode('d')],
            encoded);
    }

    [Fact]
    public void Encode_ThrowsForUnknownCharacter()
    {
        var vocab = CharVocab.FromWords(["abc"]);

        var exception = Assert.Throws<ArgumentException>(() => vocab.Encode('z'));

        Assert.Contains("not in the vocabulary", exception.Message);
    }

    [Theory]
    [InlineData(CharVocab.PadIndex)]
    [InlineData(CharVocab.EosIndex)]
    public void Decode_ThrowsForReservedIndexes(int index)
    {
        var vocab = CharVocab.FromWords(["abc"]);

        var exception = Assert.Throws<ArgumentException>(() => vocab.Decode(index));

        Assert.Contains("not a printable character", exception.Message);
    }
}
