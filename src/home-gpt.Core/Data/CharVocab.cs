using System.Text.Json;

namespace home_gpt.Data;

public sealed class CharVocab
{
    public const int PadIndex = 0;
    public const int EosIndex = 1;

    private readonly Dictionary<char, int> _charToIndex = new();
    private readonly List<char> _indexToChar = ['\0', '\0']; // PAD, EOS placeholders

    public int Size => _indexToChar.Count;

    public static CharVocab FromWords(IEnumerable<string> words)
    {
        var vocab = new CharVocab();
        foreach (var ch in words.SelectMany(w => w).Distinct().OrderBy(c => c))
            vocab.AddChar(ch);
        return vocab;
    }

    public static CharVocab FromJson(string json)
    {
        var chars = JsonSerializer.Deserialize<string>(json)
            ?? throw new InvalidDataException("Vocabulary file is empty.");
        var vocab = new CharVocab();
        foreach (var ch in chars)
            vocab.AddChar(ch);
        return vocab;
    }

    public string ToJson() => JsonSerializer.Serialize(
        new string(_indexToChar.Skip(2).ToArray()));

    public bool Contains(char ch) => _charToIndex.ContainsKey(ch);

    public int Encode(char ch) =>
        _charToIndex.TryGetValue(ch, out var index)
            ? index
            : throw new ArgumentException($"Character '{ch}' is not in the vocabulary.");

    public char Decode(int index)
    {
        if (index is PadIndex or EosIndex)
            throw new ArgumentException($"Index {index} is not a printable character.");
        return _indexToChar[index];
    }

    public long[] EncodeWord(string word)
    {
        var indices = new long[word.Length];
        for (var i = 0; i < word.Length; i++)
            indices[i] = Encode(word[i]);
        return indices;
    }

    private void AddChar(char ch)
    {
        if (_charToIndex.ContainsKey(ch))
            return;
        _charToIndex[ch] = _indexToChar.Count;
        _indexToChar.Add(ch);
    }
}
