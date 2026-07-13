using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using home_gpt.Training;
using static TorchSharp.torch;

namespace home_gpt.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(TrainingViewModel training, GenerationViewModel generation)
    {
        Training = training;
        Generation = generation;
        DeviceStatus = cuda.is_available()
            ? "CUDA is available."
            : "CUDA not detected — using CPU.";
    }

    public TrainingViewModel Training { get; }
    public GenerationViewModel Generation { get; }

    [ObservableProperty]
    private string _deviceStatus = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex;
}
