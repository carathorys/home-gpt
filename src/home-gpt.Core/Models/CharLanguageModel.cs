using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace home_gpt.Models;

public sealed class CharLanguageModel : Module<Tensor, Tensor>
{
    private readonly Embedding _embedding;
    private readonly LSTM _lstm;
    private readonly Linear _decoder;

    public int HiddenSize { get; }
    public int EmbedSize { get; }

    public CharLanguageModel(int vocabSize, int embedSize, int hiddenSize, string name = "char-lm") : base(name)
    {
        EmbedSize = embedSize;
        HiddenSize = hiddenSize;

        _embedding = Embedding(vocabSize, embedSize);
        _lstm = LSTM(embedSize, hiddenSize, batchFirst: true);
        _decoder = Linear(hiddenSize, vocabSize);

        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        using var embedded = _embedding.call(input);
        var (output, _, _) = _lstm.call(embedded);
        return _decoder.call(output);
    }
}
