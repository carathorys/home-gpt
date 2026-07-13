using home_gpt.Training;

namespace home_gpt_tests;

public sealed class WordTrainerTests
{
    [Fact]
    public void BatchToTensor_UsesBatchAndSequenceDimensions()
    {
        long[][] rows =
        [
            [1, 2, 3],
            [4, 5, 6]
        ];

        dynamic tensor = ReflectionHelpers.InvokePrivateStatic<object>(
            typeof(WordTrainer),
            "BatchToTensor",
            (object)rows);

        try
        {
            long[] shape = tensor.shape;
            Assert.Equal([2L, 3L], shape);
            Assert.Equal(1L, tensor[0, 0].item<long>());
            Assert.Equal(6L, tensor[1, 2].item<long>());
        }
        finally
        {
            tensor.Dispose();
        }
    }
}
