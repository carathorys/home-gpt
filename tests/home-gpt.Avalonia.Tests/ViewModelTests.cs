using home_gpt.Avalonia.Services;
using home_gpt.Avalonia.ViewModels;
using home_gpt.Training;

namespace home_gpt.Avalonia.Tests;

public sealed class TrainingPreviewRowsTests
{
    [Fact]
    public void Build_IncludesSummaryValues()
    {
        var state = new TrainingConfigState
        {
            DataPath = CreateWordsFile("cat\ndog\n"),
            Epochs = 3,
            BatchSize = 2,
            HiddenSize = 8,
            EmbedSize = 4
        };

        var rows = TrainingPreviewRows.Build(state);

        Assert.Contains(rows, row => row.Label == "Words" && row.Value == "2");
        Assert.Contains(rows, row => row.Label == "Device");
    }

    private static string CreateWordsFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, contents);
        return path;
    }
}

public sealed class MainViewModelTests
{
    [Fact]
    public void Constructor_ExposesTrainingAndGenerationTabs()
    {
        var vm = CreateMainViewModel();

        Assert.NotNull(vm.Training);
        Assert.NotNull(vm.Generation);
        Assert.False(string.IsNullOrWhiteSpace(vm.DeviceStatus));
    }

    internal static MainViewModel CreateMainViewModel() =>
        new(new TrainingViewModel(new FakeFileDialogService(), new FakeCheckpointDialogService()),
            new GenerationViewModel(new FakeFileDialogService()));
}

public sealed class GenerationViewModelTests
{
    [Fact]
    public void Generate_ThrowsFriendlyMessageWhenModelMissing()
    {
        using var vm = new GenerationViewModel(new FakeFileDialogService())
        {
            ModelDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        };

        vm.GenerateCommand.Execute(null);

        Assert.False(vm.IsModelLoaded);
        Assert.Contains("No trained model", vm.StatusMessage);
    }

    [Fact]
    public async Task BrowseModelDirectory_UpdatesDirectoryWhenSelected()
    {
        var dialogs = new FakeFileDialogService { NextDirectory = "/tmp/custom-model" };
        using var vm = new GenerationViewModel(dialogs);

        await vm.BrowseModelDirectoryCommand.ExecuteAsync(null);

        Assert.Equal("/tmp/custom-model", vm.ModelDirectory);
    }

    [Fact]
    public void Prefix_CanBeSetForGeneration()
    {
        using var vm = new GenerationViewModel(new FakeFileDialogService());

        vm.Prefix = "hel";

        Assert.Equal("hel", vm.Prefix);
    }

    [Fact]
    public void Generate_ProducesWordFromSavedModel()
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

        using var vm = new GenerationViewModel(new FakeFileDialogService())
        {
            ModelDirectory = outputDirectory,
            Prefix = "c",
            Temperature = 0
        };

        vm.GenerateCommand.Execute(null);

        Assert.True(vm.IsModelLoaded);
        Assert.StartsWith("c", vm.GeneratedWord);
        Assert.NotEmpty(vm.History);
    }

    private static string CreateWordsFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"home-gpt-ui-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, contents);
        return path;
    }
}

internal sealed class FakeFileDialogService : IFileDialogService
{
    public string? NextFile { get; set; }
    public string? NextDirectory { get; set; }

    public Task<string?> PickFileAsync(string title) => Task.FromResult(NextFile);
    public Task<string?> PickDirectoryAsync(string title) => Task.FromResult(NextDirectory);
}

internal sealed class FakeCheckpointDialogService(TrainingStartChoice? choice = TrainingStartChoice.ContinueFromCheckpoint)
    : ICheckpointDialogService
{
    public Task<TrainingStartChoice?> PromptAsync(CheckpointPromptKind kind, string outputDirectory) =>
        Task.FromResult(choice);
}
