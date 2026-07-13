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
        Assert.True(File.Exists(Path.Combine(outputDirectory, "model.pt")));
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
    public void EpochsFieldLabel_SwitchesWhenCheckpointLoaded()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var dataPath = CreateWordsFile("cat\n");
        home_gpt.Training.WordTrainingService.Train(new home_gpt.Training.WordTrainingConfig(
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
