using static TorchSharp.torch;

namespace home_gpt.Training;

public static class TorchDevice
{
    public static Device Select() => cuda.is_available() ? CUDA : CPU;

    public static string SelectName() => Select().ToString();
}
