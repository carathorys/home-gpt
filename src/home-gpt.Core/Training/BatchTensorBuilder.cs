using TorchSharp;
using static TorchSharp.torch;

namespace home_gpt.Training;

public static class BatchTensorBuilder
{
    public static Tensor FromRows(long[][] rows)
    {
        var batch = rows.Length;
        var seqLen = rows[0].Length;
        var flat = new long[batch * seqLen];

        for (var b = 0; b < batch; b++)
        for (var s = 0; s < seqLen; s++)
            flat[b * seqLen + s] = rows[b][s];

        return tensor(flat, new long[] { batch, seqLen }, dtype: ScalarType.Int64);
    }
}
