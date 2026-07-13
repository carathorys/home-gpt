using home_gpt.Data;
using home_gpt.Models;
using home_gpt.Persistence;
using home_gpt.Training;
using TorchSharp;
using static TorchSharp.torch;

namespace home_gpt.Inference;

public sealed class WordGenerationService : IDisposable
{
    private readonly CharLanguageModel _model;
    private readonly CharVocab _vocab;
    private readonly Device _device;
    private readonly int _maxLength;

    private WordGenerationService(CharLanguageModel model, CharVocab vocab, Device device, int maxLength)
    {
        _model = model;
        _vocab = vocab;
        _device = device;
        _maxLength = maxLength;
    }

    public static WordGenerationService Load(string modelDirectory)
    {
        if (!ModelCheckpoint.Exists(modelDirectory))
            throw new FileNotFoundException($"No trained model found in '{modelDirectory}'.");

        var metadata = ModelCheckpoint.LoadMetadata(modelDirectory);
        var vocab = CharVocab.FromJson(metadata.VocabJson);
        var device = TorchDevice.Select();

        var model = new CharLanguageModel(metadata.VocabSize, metadata.EmbedSize, metadata.HiddenSize);
        model.load(ModelCheckpoint.WeightsPath(modelDirectory));
        model.to(device);
        model.eval();

        return new WordGenerationService(model, vocab, device, metadata.SequenceLength - 1);
    }

    internal static WordGenerationService CreateForTesting(
        CharLanguageModel model,
        CharVocab vocab,
        Device device,
        int maxLength) =>
        new(model, vocab, device, maxLength);

    public string Generate(string prefix, double temperature = 0.8)
    {
        prefix = prefix.Trim();
        ValidatePrefix(prefix);

        var indices = prefix.Length == 0
            ? new List<long> { CharVocab.PadIndex }
            : new List<long>(_vocab.EncodeWord(prefix));
        var generated = new System.Text.StringBuilder(prefix);

        using var _ = no_grad();
        for (var step = prefix.Length; step < _maxLength; step++)
        {
            using var input = tensor(indices.ToArray(), new long[] { 1, indices.Count }, dtype: ScalarType.Int64)
                .to(_device);
            using var logits = _model.call(input);
            using var nextLogits = logits[0, indices.Count - 1];

            var nextIndex = SampleIndex(nextLogits, temperature);
            if (nextIndex is CharVocab.EosIndex or CharVocab.PadIndex)
                break;

            indices.Add(nextIndex);
            generated.Append(_vocab.Decode((int)nextIndex));
        }

        return generated.ToString();
    }

    internal static long SampleIndex(Tensor logits, double temperature)
    {
        if (temperature <= 0)
            return logits.argmax().item<long>();

        using var scaled = logits / temperature;
        using var probs = scaled.softmax(dim: 0);
        return probs.multinomial(1, replacement: true).item<long>();
    }

    private void ValidatePrefix(string prefix)
    {
        foreach (var ch in prefix)
        {
            if (!_vocab.Contains(ch))
                throw new ArgumentException($"Character '{ch}' was not seen during training.");
        }
    }

    public void Dispose() => _model.Dispose();
}
