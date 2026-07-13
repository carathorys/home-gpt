using home_gpt.Training;
using static TorchSharp.torch;

namespace home_gpt.Core.Tests;

public sealed class BatchTensorBuilderTests
{
    [Fact]
    public void FromRows_UsesBatchAndSequenceDimensions()
    {
        long[][] rows =
        [
            [1, 2, 3],
            [4, 5, 6]
        ];

        using var tensor = BatchTensorBuilder.FromRows(rows);

        Assert.Equal([2L, 3L], tensor.shape);
        Assert.Equal(1L, tensor[0, 0].item<long>());
        Assert.Equal(6L, tensor[1, 2].item<long>());
    }
}
