using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using home_gpt.Avalonia.Services;
using home_gpt.Training;

namespace home_gpt.Avalonia.ViewModels;

public partial class TrainingViewModel : ViewModelBase
{
    private readonly TrainingConfigState _state = new();
    private readonly IFileDialogService _fileDialogs;
    private readonly ICheckpointDialogService _checkpointDialog;
    private CancellationTokenSource? _trainingCts;
    private bool _isSyncingFromState;

    public TrainingViewModel(IFileDialogService fileDialogs, ICheckpointDialogService checkpointDialog)
    {
        _fileDialogs = fileDialogs;
        _checkpointDialog = checkpointDialog;
        SyncFromState();
        RefreshPreview();
    }

    [ObservableProperty]
    private string? _dataPath;

    [ObservableProperty]
    private string _outputDirectory = TrainingConfigState.DefaultModelDirectory;

    [ObservableProperty]
    private int _epochs = 100;

    [ObservableProperty]
    private int _batchSize = 32;

    [ObservableProperty]
    private double _learningRate = 0.003;

    [ObservableProperty]
    private int _hiddenSize = 128;

    [ObservableProperty]
    private int _embedSize = 64;

    [ObservableProperty]
    private string _epochsFieldLabel = "Epochs";

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isTraining;

    [ObservableProperty]
    private double _trainingProgress;

    [ObservableProperty]
    private string _trainingStatus = string.Empty;

    public ObservableCollection<PreviewRow> PreviewRows { get; } = [];

    partial void OnDataPathChanged(string? value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        if (!_isSyncingFromState)
            UpdateState(reloadCheckpoint: true);
    }

    partial void OnEpochsChanged(int value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    partial void OnBatchSizeChanged(int value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    partial void OnLearningRateChanged(double value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    partial void OnHiddenSizeChanged(int value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    partial void OnEmbedSizeChanged(int value)
    {
        if (!_isSyncingFromState)
            UpdateState();
    }

    [RelayCommand]
    private async Task BrowseDataPathAsync()
    {
        var path = await _fileDialogs.PickFileAsync("Select word list (one word per line)");
        if (path is not null)
            DataPath = path;
    }

    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        var path = await _fileDialogs.PickDirectoryAsync("Select model output directory");
        if (path is not null)
            OutputDirectory = path;
    }

    [RelayCommand(CanExecute = nameof(CanStartTraining))]
    private async Task StartTrainingAsync()
    {
        SyncToState();

        if (!_state.TryValidate(out var error))
        {
            ValidationError = error;
            return;
        }

        ValidationError = null;

        var promptKind = TrainingStartResolver.GetPromptKind(_state);
        if (promptKind != CheckpointPromptKind.None)
        {
            var choice = await _checkpointDialog.PromptAsync(promptKind, OutputDirectory);
            if (choice is null)
            {
                StatusMessage = "Training cancelled.";
                return;
            }

            if (!TrainingStartResolver.TryApplyChoice(_state, choice.Value))
            {
                StatusMessage = "Training cancelled.";
                return;
            }
        }

        await RunTrainingAsync();
    }

    [RelayCommand(CanExecute = nameof(CanCancelTraining))]
    private void CancelTraining()
    {
        _trainingCts?.Cancel();
    }

    private bool CanStartTraining() => !IsTraining;
    private bool CanCancelTraining() => IsTraining;

    partial void OnIsTrainingChanged(bool value)
    {
        StartTrainingCommand.NotifyCanExecuteChanged();
        CancelTrainingCommand.NotifyCanExecuteChanged();
    }

    private void UpdateState(bool reloadCheckpoint = false)
    {
        SyncToState();

        if (reloadCheckpoint)
            _state.TryLoadCheckpointFromOutputDirectory();

        SyncFromState();
        RefreshPreview();
    }

    private void SyncToState()
    {
        _state.DataPath = DataPath;
        _state.OutputDirectory = OutputDirectory;
        _state.Epochs = Epochs;
        _state.BatchSize = BatchSize;
        _state.LearningRate = LearningRate;
        _state.HiddenSize = HiddenSize;
        _state.EmbedSize = EmbedSize;
        _state.ApplySessionState();
    }

    private void SyncFromState()
    {
        _isSyncingFromState = true;
        try
        {
            DataPath = _state.DataPath;
            OutputDirectory = _state.OutputDirectory;
            Epochs = _state.Epochs;
            BatchSize = _state.BatchSize;
            LearningRate = _state.LearningRate;
            HiddenSize = _state.HiddenSize;
            EmbedSize = _state.EmbedSize;
            EpochsFieldLabel = _state.EpochsLabel;
        }
        finally
        {
            _isSyncingFromState = false;
        }
    }

    private void RefreshPreview()
    {
        PreviewRows.Clear();
        foreach (var row in TrainingPreviewRows.Build(_state))
            PreviewRows.Add(row);
    }

    internal void ApplyTrainingEpochProgress(
        TrainingEpochProgress progress,
        int epochsThisRun,
        int previousCompleted)
    {
        var (percent, status) = TrainingProgressReporter.Format(progress, epochsThisRun, previousCompleted);
        TrainingProgress = percent;
        TrainingStatus = status;
    }

    private async Task RunTrainingAsync()
    {
        IsTraining = true;
        TrainingProgress = 0;
        TrainingStatus = _state.ResumeFromCheckpoint
            ? $"Resuming from epoch {_state.CompletedEpochs}, running {Epochs} more epoch(s)..."
            : "Starting training...";
        StatusMessage = string.Empty;

        _trainingCts = new CancellationTokenSource();
        var config = _state.ToConfig();
        var previousCompleted = config.PreviousCompletedEpochs;

        try
        {
            var result = await Task.Run(
                () => WordTrainingService.Train(
                    config,
                    new UiTrainingProgress(this, config.Epochs, previousCompleted),
                    _trainingCts.Token),
                _trainingCts.Token);

            TrainingProgress = 100;
            TrainingStatus = $"Finished {result.CompletedEpochs} epoch(s).";
            StatusMessage =
                $"Training complete. Model saved to {result.OutputDirectory} ({result.CompletedEpochs} total epochs).";
            _state.TryLoadCheckpointFromOutputDirectory();
            SyncFromState();
            RefreshPreview();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Training cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            _trainingCts?.Dispose();
            _trainingCts = null;
            IsTraining = false;
            if (!StatusMessage.StartsWith("Training complete", StringComparison.Ordinal))
            {
                TrainingProgress = 0;
                TrainingStatus = string.Empty;
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class UiTrainingProgress(TrainingViewModel vm, int epochsThisRun, int previousCompleted)
        : ITrainingProgress
    {
        public void OnEpochCompleted(TrainingEpochProgress progress)
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(
                () => vm.ApplyTrainingEpochProgress(progress, epochsThisRun, previousCompleted),
                global::Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
