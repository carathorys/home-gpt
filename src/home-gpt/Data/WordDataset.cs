namespace home_gpt.Data;

/// <summary>
/// One word per line. Characters are preserved as-is; the vocabulary is built
/// from every distinct character that appears in the training file.
/// </summary>
public sealed class WordDataset
{
    public IReadOnlyList<string> Words { get; }
    public CharVocab Vocab { get; }
    public int MaxWordLength { get; }
    public int SequenceLength { get; }

    public WordDataset(string path)
    {
        Words = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(NormalizeWord)
            .ToArray();

        if (Words.Count == 0)
            throw new InvalidDataException($"No words found in '{path}'.");

        Vocab = CharVocab.FromWords(Words);
        MaxWordLength = Words.Max(w => w.Length);
        SequenceLength = MaxWordLength + 1;
    }

    public (long[] input, long[] target) GetSequence(string word)
    {
        var input = new long[SequenceLength];
        var target = new long[SequenceLength];

        for (var i = 0; i < SequenceLength; i++)
        {
            input[i] = CharVocab.PadIndex;
            target[i] = CharVocab.PadIndex;
        }

        for (var i = 0; i < word.Length; i++)
            input[i] = Vocab.Encode(word[i]);

        for (var i = 0; i < word.Length - 1; i++)
            target[i] = Vocab.Encode(word[i + 1]);

        if (word.Length > 0)
            target[word.Length - 1] = CharVocab.EosIndex;

        return (input, target);
    }

    public (long[][] batchInputs, long[][] batchTargets) GetBatch(int start, int size)
    {
        var count = Math.Min(size, Words.Count - start);
        var inputs = new long[count][];
        var targets = new long[count][];

        for (var i = 0; i < count; i++)
            (inputs[i], targets[i]) = GetSequence(Words[start + i]);

        return (inputs, targets);
    }

    private static string NormalizeWord(string word)
    {
        if (word.Any(char.IsControl))
            throw new InvalidDataException($"Word '{word}' contains control characters.");

        return word;
    }
}
