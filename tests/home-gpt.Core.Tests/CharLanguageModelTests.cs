using home_gpt.Models;
using static TorchSharp.torch;

namespace home_gpt.Core.Tests;

public sealed class CharLanguageModelTests
{
    [Fact]
    public void Constructor_StoresModelDimensions()
    {
        using var model = new CharLanguageModel(vocabSize: 7, embedSize: 5, hiddenSize: 11);

        Assert.Equal(5, model.EmbedSize);
        Assert.Equal(11, model.HiddenSize);
    }

    [Fact]
    public void Forward_ReturnsLogitsForEachBatchAndTimeStep()
    {
        using var model = new CharLanguageModel(vocabSize: 9, embedSize: 4, hiddenSize: 6);
        using var input = tensor(new long[] { 2, 3, 4, 5, 6, 7 }, new long[] { 2, 3 }, dtype: ScalarType.Int64);

        using var output = model.forward(input);

        Assert.Equal([2L, 3L, 9L], output.shape);
    }
}
