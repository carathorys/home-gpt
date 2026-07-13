using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using home_gpt.Avalonia.Services;
using home_gpt.Avalonia.ViewModels;

namespace home_gpt.Avalonia;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var fileDialogs = new FileDialogService(() => this);
        var checkpointDialog = new CheckpointDialogService(() => this);
        var training = new TrainingViewModel(fileDialogs, checkpointDialog);
        var generation = new GenerationViewModel(fileDialogs);

        DataContext = new MainViewModel(training, generation);
    }
}
