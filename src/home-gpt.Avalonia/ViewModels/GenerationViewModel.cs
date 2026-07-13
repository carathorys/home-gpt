using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using home_gpt.Avalonia.Services;
using home_gpt.Inference;
using home_gpt.Persistence;
using home_gpt.Training;

namespace home_gpt.Avalonia.ViewModels;

public partial class GenerationViewModel : ViewModelBase, IDisposable
{
    private readonly IFileDialogService _fileDialogs;
    private WordGenerationService? _service;

    public GenerationViewModel(IFileDialogService fileDialogs)
    {
        _fileDialogs = fileDialogs;
        ModelDirectory = TrainingConfigState.DefaultModelDirectory;
        Temperature = 0.8;
        TryLoadModel();
    }

    [ObservableProperty]
    private string _modelDirectory = string.Empty;

    [ObservableProperty]
    private string _prefix = string.Empty;

    [ObservableProperty]
    private double _temperature = 0.8;

    [ObservableProperty]
    private string _generatedWord = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isModelLoaded;

    public ObservableCollection<string> History { get; } = [];

    partial void OnModelDirectoryChanged(string value) => TryLoadModel();

    [RelayCommand]
    private async Task BrowseModelDirectoryAsync()
    {
        var path = await _fileDialogs.PickDirectoryAsync("Select trained model directory");
        if (path is not null)
            ModelDirectory = path;
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private void Generate()
    {
        if (_service is null)
        {
            StatusMessage = "No trained model loaded.";
            return;
        }

        try
        {
            GeneratedWord = _service.Generate(Prefix, Temperature);
            History.Insert(0, GeneratedWord);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private bool CanGenerate() => IsModelLoaded;

    partial void OnIsModelLoadedChanged(bool value) => GenerateCommand.NotifyCanExecuteChanged();

    private void TryLoadModel()
    {
        DisposeService();

        if (!ModelCheckpoint.Exists(ModelDirectory))
        {
            IsModelLoaded = false;
            StatusMessage = $"No trained model found in '{ModelDirectory}'.";
            return;
        }

        try
        {
            _service = WordGenerationService.Load(ModelDirectory);
            IsModelLoaded = true;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            IsModelLoaded = false;
            StatusMessage = ex.Message;
        }
    }

    public void Dispose()
    {
        DisposeService();
        GC.SuppressFinalize(this);
    }

    private void DisposeService()
    {
        _service?.Dispose();
        _service = null;
    }
}
