using home_gpt.Avalonia.Services;
using home_gpt.Avalonia.ViewModels;
using home_gpt.Training;

namespace home_gpt.Avalonia.Tests;

public sealed class TrainingViewModelTests
{
    [Fact]
    public void StartTrainingCommand_IsDisabledWhileTraining()
    {
        var vm = CreateViewModel();
        vm.IsTraining = true;

        Assert.False(vm.StartTrainingCommand.CanExecute(null));
        Assert.True(vm.CancelTrainingCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartTrainingAsync_SetsValidationErrorWhenDataMissing()
    {
        var vm = CreateViewModel(vm => vm.DataPath = null);

        await vm.StartTrainingCommand.ExecuteAsync(null);

        Assert.Contains("Select a data file", vm.ValidationError ?? string.Empty);
    }

    [Fact]
    public async Task StartTrainingAsync_CompletesSingleEpoch()
    {
        var dataPath = CreateWordsFile("cat\ndog\n");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var vm = CreateViewModel(vm =>
        {
            vm.DataPath = dataPath;
            vm.OutputDirectory = outputDirectory;
            vm.Epochs = 1;
            vm.BatchSize = 2;
            vm.HiddenSize = 8;
            vm.EmbedSize = 4;
        });

        await vm.StartTrainingCommand.ExecuteAsync(null);

        Assert.Contains("Training complete", vm.StatusMessage);
        Assert.Equal(100, vm.TrainingProgress);
        Assert.Contains("Finished", vm.TrainingStatus);
        Assert.False(vm.IsTraining);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "model.pt")));
    }

    [Fact]
    public async Task StartTrainingAsync_WithExistingCheckpoint_ContinuesTraining()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\ndog\n");
        WordTrainingService.Train(new WordTrainingConfig(
            dataPath,
            outputDirectory,
            Epochs: 1,
            BatchSize: 1,
            HiddenSize: 8,
            EmbedSize: 4));

        var vm = new TrainingViewModel(
            new FakeFileDialogService(),
            new FakeCheckpointDialogService(TrainingStartChoice.ContinueFromCheckpoint))
        {
            DataPath = dataPath,
            OutputDirectory = outputDirectory,
            Epochs = 1,
            BatchSize = 1,
            HiddenSize = 8,
            EmbedSize = 4
        };

        await vm.StartTrainingCommand.ExecuteAsync(null);

        Assert.Contains("Training complete", vm.StatusMessage);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "model.pt")));
    }

    [Fact]
    public async Task StartTrainingAsync_WhenCheckpointDialogCancelled_DoesNotTrain()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\n");
        WordTrainingService.Train(new WordTrainingConfig(
            dataPath,
            outputDirectory,
            Epochs: 1,
            BatchSize: 1,
            HiddenSize: 8,
            EmbedSize: 4));

        var vm = new TrainingViewModel(
            new FakeFileDialogService(),
            new FakeCheckpointDialogService(TrainingStartChoice.Cancel))
        {
            DataPath = dataPath,
            OutputDirectory = outputDirectory,
            Epochs = 1,
            HiddenSize = 8,
            EmbedSize = 4
        };

        await vm.StartTrainingCommand.ExecuteAsync(null);

        Assert.Contains("cancelled", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(vm.IsTraining);
    }

    [Fact]
    public async Task StartTrainingAsync_WhenCheckpointDialogReturnsNull_DoesNotTrain()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\n");
        WordTrainingService.Train(new WordTrainingConfig(
            dataPath,
            outputDirectory,
            Epochs: 1,
            BatchSize: 1,
            HiddenSize: 8,
            EmbedSize: 4));

        var vm = new TrainingViewModel(
            new FakeFileDialogService(),
            new FakeCheckpointDialogService(choice: null))
        {
            DataPath = dataPath,
            OutputDirectory = outputDirectory,
            Epochs = 1,
            HiddenSize = 8,
            EmbedSize = 4
        };

        await vm.StartTrainingCommand.ExecuteAsync(null);

        Assert.Contains("cancelled", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(vm.IsTraining);
    }

    [Fact]
    public void ApplyTrainingEpochProgress_UpdatesProgressAndStatus()
    {
        var vm = CreateViewModel();

        vm.ApplyTrainingEpochProgress(new TrainingEpochProgress(2, 10, 0.42), epochsThisRun: 5, previousCompleted: 0);

        Assert.Equal(40, vm.TrainingProgress);
        Assert.Contains("Epoch 2/10", vm.TrainingStatus);
        Assert.Contains("loss=0.42", vm.TrainingStatus);
    }

    [Fact]
    public void PreviewRows_RefreshWhenDataPathChanges()
    {
        var vm = CreateViewModel();
        var dataPath = CreateWordsFile("cat\ndog\n");

        vm.DataPath = dataPath;

        Assert.Contains(vm.PreviewRows, row => row.Label == "Words" && row.Value == "2");
    }

    [Fact]
    public void OutputDirectory_LoadsCheckpointArchitecture()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\n");
        WordTrainingService.Train(new WordTrainingConfig(
            dataPath,
            outputDirectory,
            Epochs: 1,
            BatchSize: 1,
            LearningRate: 0.01,
            HiddenSize: 32,
            EmbedSize: 16));

        var vm = CreateViewModel();
        vm.OutputDirectory = outputDirectory;

        Assert.Equal(32, vm.HiddenSize);
        Assert.Equal(16, vm.EmbedSize);
        Assert.Equal(0.01, vm.LearningRate);
        Assert.Equal(1, vm.BatchSize);
        Assert.Equal(dataPath, vm.DataPath);
        Assert.Equal("Additional epochs", vm.EpochsFieldLabel);
    }

    [Fact]
    public void EpochsFieldLabel_SwitchesWhenCheckpointLoaded()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\n");
        WordTrainingService.Train(new WordTrainingConfig(
            dataPath,
            outputDirectory,
            Epochs: 1,
            BatchSize: 1,
            HiddenSize: 8,
            EmbedSize: 4));

        var vm = CreateViewModel(vm =>
        {
            vm.OutputDirectory = outputDirectory;
            vm.DataPath = dataPath;
            vm.HiddenSize = 8;
            vm.EmbedSize = 4;
        });

        Assert.Equal("Additional epochs", vm.EpochsFieldLabel);
    }

    private static TrainingViewModel CreateViewModel(Action<TrainingViewModel>? configure = null)
    {
        var vm = new TrainingViewModel(new FakeFileDialogService(), new FakeCheckpointDialogService());
        configure?.Invoke(vm);
        return vm;
    }

    [Fact]
    public async Task BrowseDataPath_UpdatesPathWhenSelected()
    {
        var dataPath = CreateWordsFile("cat\n");
        var dialogs = new FakeFileDialogService { NextFile = dataPath };
        var vm = new TrainingViewModel(dialogs, new FakeCheckpointDialogService());

        await vm.BrowseDataPathCommand.ExecuteAsync(null);

        Assert.Equal(dataPath, vm.DataPath);
    }

    private static string CreateWordsFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, contents);
        return path;
    }
}
