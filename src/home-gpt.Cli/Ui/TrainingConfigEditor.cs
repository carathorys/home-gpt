
using System.Diagnostics.CodeAnalysis;
using home_gpt.Cli.Training;
using home_gpt.Data;
using home_gpt.Persistence;
using home_gpt.Training;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace home_gpt.Cli.Ui;

public enum TrainingEditorResult
{
    StartTraining,
    BackToMainMenu
}

[ExcludeFromCodeCoverage]
public sealed class TrainingConfigEditor
{
    private const string DefaultModelDirectory = "models/word-model";

    private enum ConfigField
    {
        DataPath,
        OutputDirectory,
        Epochs,
        BatchSize,
        LearningRate,
        HiddenSize,
        EmbedSize,
        Start,
        Back
    }

    private enum PendingAction
    {
        None,
        PickDataPath,
        PickOutputDirectory
    }

    private static readonly ConfigField[] Fields =
    [
        ConfigField.DataPath,
        ConfigField.OutputDirectory,
        ConfigField.Epochs,
        ConfigField.BatchSize,
        ConfigField.LearningRate,
        ConfigField.HiddenSize,
        ConfigField.EmbedSize,
        ConfigField.Start,
        ConfigField.Back
    ];

    private bool _resumeFromCheckpoint;
    private ModelMetadata? _loadedCheckpoint;

    public string? DataPath { get; private set; }
    public string OutputDirectory { get; private set; } = DefaultModelDirectory;
    public int Epochs { get; private set; } = 100;
    public int BatchSize { get; private set; } = 32;
    public double LearningRate { get; private set; } = 0.003;
    public int HiddenSize { get; private set; } = 128;
    public int EmbedSize { get; private set; } = 64;
    public int CompletedEpochs { get; private set; }

    public TrainingConfigEditor()
    {
        TryLoadCheckpointFromOutputDirectory();
    }

    public TrainingEditorResult Run()
    {
        while (true)
        {
            if (!RunLiveSession(out var startRequested))
                return TrainingEditorResult.BackToMainMenu;

            if (!startRequested)
                continue;

            if (!TryResolveCheckpointAction(out var error))
            {
                if (error.Length > 0)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
                    AnsiConsole.WriteLine();
                }

                continue;
            }

            return TrainingEditorResult.StartTraining;
        }
    }

    public WordTrainingConfig ToConfig() =>
        new(
            DataPath!,
            OutputDirectory,
            Epochs,
            BatchSize,
            LearningRate,
            HiddenSize,
            EmbedSize,
            _resumeFromCheckpoint,
            CompletedEpochs);

    private string EpochsLabel =>
        _resumeFromCheckpoint ? "Additional epochs" : "Epochs";

    private bool RunLiveSession(out bool startRequested)
    {
        startRequested = false;
        var selectedIndex = 0;
        var editBuffer = string.Empty;
        var isEditing = false;
        string? validationError = null;
        var startTraining = false;

        AnsiConsole.Clear();

        while (true)
        {
            var pendingAction = PendingAction.None;
            var backToMenu = false;
            startTraining = false;

            AnsiConsole.Live(new Markup(string.Empty))
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Start(ctx =>
                {
                    while (true)
                    {
                        var preview = TrainingPreview.Compute(
                            DataPath,
                            OutputDirectory,
                            Epochs,
                            BatchSize,
                            LearningRate,
                            HiddenSize,
                            EmbedSize);

                        ctx.UpdateTarget(BuildView(
                            preview,
                            selectedIndex,
                            editBuffer,
                            isEditing,
                            validationError));

                        var key = Console.ReadKey(intercept: true);

                        if (isEditing)
                        {
                            if (TryHandleEditingKey(
                                    Fields[selectedIndex],
                                    key,
                                    ref editBuffer,
                                    ref isEditing,
                                    ref validationError))
                                continue;
                        }

                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                CommitEditBuffer(Fields[selectedIndex], editBuffer, ref isEditing, ref editBuffer);
                                selectedIndex = (selectedIndex - 1 + Fields.Length) % Fields.Length;
                                validationError = null;
                                break;

                            case ConsoleKey.DownArrow:
                                CommitEditBuffer(Fields[selectedIndex], editBuffer, ref isEditing, ref editBuffer);
                                selectedIndex = (selectedIndex + 1) % Fields.Length;
                                validationError = null;
                                break;

                            case ConsoleKey.Tab:
                                CommitEditBuffer(Fields[selectedIndex], editBuffer, ref isEditing, ref editBuffer);
                                selectedIndex = (selectedIndex + 1) % Fields.Length;
                                validationError = null;
                                break;

                            case ConsoleKey.Escape:
                                backToMenu = true;
                                return;

                            case ConsoleKey.Enter:
                                switch (Fields[selectedIndex])
                                {
                                    case ConfigField.DataPath:
                                        pendingAction = PendingAction.PickDataPath;
                                        return;
                                    case ConfigField.OutputDirectory:
                                        pendingAction = PendingAction.PickOutputDirectory;
                                        return;
                                    case ConfigField.Start:
                                        if (!TryValidate(out validationError))
                                            break;

                                        startTraining = true;
                                        return;
                                    case ConfigField.Back:
                                        backToMenu = true;
                                        return;
                                    default:
                                        if (IsNumericField(Fields[selectedIndex]))
                                        {
                                            isEditing = true;
                                            editBuffer = GetFieldText(Fields[selectedIndex]);
                                        }

                                        break;
                                }

                                break;

                            case ConsoleKey.Add or ConsoleKey.OemPlus:
                                if (TryAdjustNumericField(Fields[selectedIndex], increment: true))
                                    validationError = null;
                                break;

                            case ConsoleKey.Subtract or ConsoleKey.OemMinus:
                                if (TryAdjustNumericField(Fields[selectedIndex], increment: false))
                                    validationError = null;
                                break;

                            default:
                                if (IsNumericField(Fields[selectedIndex]) &&
                                    TryBeginOrContinueEditing(Fields[selectedIndex], key, ref editBuffer, ref isEditing))
                                {
                                    validationError = null;
                                }
                                else if (Fields[selectedIndex] == ConfigField.Start &&
                                         (key.KeyChar == 's' || key.KeyChar == 'S'))
                                {
                                    if (!TryValidate(out validationError))
                                        break;

                                    startTraining = true;
                                    return;
                                }
                                else if (Fields[selectedIndex] == ConfigField.Back &&
                                         (key.KeyChar == 'b' || key.KeyChar == 'B'))
                                {
                                    backToMenu = true;
                                    return;
                                }
                                else if (key.KeyChar == 'f' || key.KeyChar == 'F')
                                {
                                    pendingAction = Fields[selectedIndex] switch
                                    {
                                        ConfigField.DataPath => PendingAction.PickDataPath,
                                        ConfigField.OutputDirectory => PendingAction.PickOutputDirectory,
                                        _ => PendingAction.None
                                    };

                                    if (pendingAction != PendingAction.None)
                                        return;
                                }

                                break;
                        }
                    }
                });

            if (backToMenu)
                return false;

            if (startTraining)
            {
                startRequested = true;
                return true;
            }

            switch (pendingAction)
            {
                case PendingAction.PickDataPath:
                    EditDataPath();
                    break;
                case PendingAction.PickOutputDirectory:
                    EditOutputDirectory();
                    break;
            }
        }
    }

    private IRenderable BuildView(
        TrainingPreview preview,
        int selectedIndex,
        string editBuffer,
        bool isEditing,
        string? validationError)
    {
        var parts = new List<IRenderable>
        {
            new Markup("[bold]Training configuration[/] [dim](values update live)[/]"),
            new Text(string.Empty),
            BuildPreviewTable(preview),
            new Text(string.Empty),
            BuildFieldsTable(selectedIndex, editBuffer, isEditing)
        };

        if (!string.IsNullOrWhiteSpace(validationError))
        {
            parts.Add(new Text(string.Empty));
            parts.Add(new Markup($"[red]{Markup.Escape(validationError)}[/]"));
        }

        parts.Add(new Text(string.Empty));
        parts.Add(new Markup(
            "[dim]↑↓ select  type to edit numbers  +/- adjust  F browse paths  S start  Esc back[/]"));

        return new Rows(parts);
    }

    private Table BuildPreviewTable(TrainingPreview preview)
    {
        if (preview.Summary is not null)
        {
            var table = TrainingSummaryPresenter.ToTable(preview.Summary, "[bold]Training preview[/]");
            AddCheckpointRows(table);
            return table;
        }

        var partial = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Training preview[/]")
            .AddColumn("[dim]Setting[/]")
            .AddColumn("[dim]Value[/]");

        partial.AddRow("Data file", FormatPath(DataPath));
        partial.AddRow("Output directory", Markup.Escape(OutputDirectory));
        partial.AddRow("Device", preview.Device);
        partial.AddEmptyRow();

        if (preview.DatasetError is not null)
            partial.AddRow("Data", $"[red]{Markup.Escape(preview.DatasetError)}[/]");
        else
            partial.AddRow("Words", "[dim](select a data file)[/]");

        partial.AddRow("Vocabulary size", "[dim]—[/]");
        partial.AddRow("Max word length", "[dim]—[/]");
        partial.AddRow("Sequence length", "[dim]—[/]");
        partial.AddEmptyRow();
        partial.AddRow(EpochsLabel, Epochs.ToString("N0"));
        partial.AddRow("Batch size", BatchSize.ToString("N0"));
        partial.AddRow("Batches per epoch", "[dim]—[/]");
        partial.AddRow("Total training steps", "[dim]—[/]");
        partial.AddRow("Learning rate", LearningRate.ToString("G"));
        partial.AddEmptyRow();
        partial.AddRow("Embedding size", EmbedSize.ToString("N0"));
        partial.AddRow("Hidden size", HiddenSize.ToString("N0"));
        partial.AddRow("Parameters", "[dim]—[/]");
        partial.AddRow("Model size (float32)", "[dim]—[/]");

        AddCheckpointRows(partial);
        return partial;
    }

    private void AddCheckpointRows(Table table)
    {
        if (_loadedCheckpoint is null)
            return;

        table.AddEmptyRow();
        table.AddRow("Completed epochs", CompletedEpochs.ToString("N0"));
        table.AddRow(
            "Checkpoint",
            IsCheckpointLockedConfigurationChanged()
                ? "[yellow]Loaded, but critical settings changed[/]"
                : "[cyan]Loaded and ready to continue[/]");
    }

    private Table BuildFieldsTable(int selectedIndex, string editBuffer, bool isEditing)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Edit settings[/]")
            .AddColumn("[dim][/]")
            .AddColumn("[dim]Parameter[/]")
            .AddColumn("[dim]Value[/]");

        for (var i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];
            var selected = i == selectedIndex;
            var marker = selected ? "[cyan]>[/]" : " ";
            var labelStyle = selected ? "[cyan]" : string.Empty;
            var labelEnd = selected ? "[/]" : string.Empty;
            var value = FormatFieldValue(field, selected && isEditing, editBuffer);

            table.AddRow(
                marker,
                $"{labelStyle}{GetFieldLabel(field)}{labelEnd}",
                value);
        }

        return table;
    }

    private string FormatFieldValue(ConfigField field, bool editing, string editBuffer)
    {
        if (editing && IsNumericField(field))
            return $"[yellow]{Markup.Escape(editBuffer)}[/][dim]█[/]";

        return field switch
        {
            ConfigField.DataPath => FormatPath(DataPath),
            ConfigField.OutputDirectory => Markup.Escape(OutputDirectory),
            ConfigField.Epochs => Epochs.ToString("N0"),
            ConfigField.BatchSize => BatchSize.ToString("N0"),
            ConfigField.LearningRate => LearningRate.ToString("G"),
            ConfigField.HiddenSize => HiddenSize.ToString("N0"),
            ConfigField.EmbedSize => EmbedSize.ToString("N0"),
            ConfigField.Start => "[green]Start training[/]",
            ConfigField.Back => "[dim]Back to main menu[/]",
            _ => string.Empty
        };
    }

    private string GetFieldLabel(ConfigField field) => field switch
    {
        ConfigField.DataPath => "Data file",
        ConfigField.OutputDirectory => "Output directory",
        ConfigField.Epochs => EpochsLabel,
        ConfigField.BatchSize => "Batch size",
        ConfigField.LearningRate => "Learning rate",
        ConfigField.HiddenSize => "Hidden layer size",
        ConfigField.EmbedSize => "Embedding size",
        ConfigField.Start => "Action",
        ConfigField.Back => "Action",
        _ => field.ToString()
    };

    private static bool IsNumericField(ConfigField field) =>
        field is ConfigField.Epochs
            or ConfigField.BatchSize
            or ConfigField.LearningRate
            or ConfigField.HiddenSize
            or ConfigField.EmbedSize;

    private string GetFieldText(ConfigField field) => field switch
    {
        ConfigField.Epochs => Epochs.ToString(),
        ConfigField.BatchSize => BatchSize.ToString(),
        ConfigField.LearningRate => LearningRate.ToString("G"),
        ConfigField.HiddenSize => HiddenSize.ToString(),
        ConfigField.EmbedSize => EmbedSize.ToString(),
        _ => string.Empty
    };

    private bool TryBeginOrContinueEditing(
        ConfigField field,
        ConsoleKeyInfo key,
        ref string editBuffer,
        ref bool isEditing)
    {
        if (!IsNumericField(field))
            return false;

        var ch = key.KeyChar;
        if (field == ConfigField.LearningRate)
        {
            if (!char.IsDigit(ch) && ch != '.' && ch != ',')
                return false;

            if (ch == ',')
                ch = '.';
        }
        else if (!char.IsDigit(ch))
        {
            return false;
        }

        if (!isEditing)
        {
            isEditing = true;
            editBuffer = ch.ToString();
        }
        else
        {
            editBuffer += ch;
        }

        ApplyEditBuffer(field, editBuffer);
        UpdateResumeStateFromCheckpoint();
        return true;
    }

    private bool TryHandleEditingKey(
        ConfigField field,
        ConsoleKeyInfo key,
        ref string editBuffer,
        ref bool isEditing,
        ref string? validationError)
    {
        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                if (editBuffer.Length > 0)
                {
                    editBuffer = editBuffer[..^1];
                    if (editBuffer.Length == 0)
                        RevertNumericField(field);
                    else
                        ApplyEditBuffer(field, editBuffer);

                    UpdateResumeStateFromCheckpoint();
                }

                validationError = null;
                return true;

            case ConsoleKey.Escape:
                isEditing = false;
                editBuffer = string.Empty;
                validationError = null;
                return true;

            case ConsoleKey.Enter:
                CommitEditBuffer(field, editBuffer, ref isEditing, ref editBuffer);
                validationError = null;
                return true;
        }

        if (TryBeginOrContinueEditing(field, key, ref editBuffer, ref isEditing))
        {
            validationError = null;
            return true;
        }

        return false;
    }

    private void CommitEditBuffer(
        ConfigField field,
        string editBuffer,
        ref bool isEditing,
        ref string editBufferOut)
    {
        if (isEditing && editBuffer.Length > 0)
            ApplyEditBuffer(field, editBuffer);

        isEditing = false;
        editBufferOut = string.Empty;
        UpdateResumeStateFromCheckpoint();
    }

    private void ApplyEditBuffer(ConfigField field, string buffer)
    {
        if (field == ConfigField.LearningRate)
        {
            if (double.TryParse(buffer, out var value) && value > 0)
                LearningRate = value;

            return;
        }

        if (int.TryParse(buffer, out var intValue) && intValue > 0)
        {
            switch (field)
            {
                case ConfigField.Epochs:
                    Epochs = intValue;
                    break;
                case ConfigField.BatchSize:
                    BatchSize = intValue;
                    break;
                case ConfigField.HiddenSize:
                    HiddenSize = intValue;
                    break;
                case ConfigField.EmbedSize:
                    EmbedSize = intValue;
                    break;
            }
        }
    }

    private void RevertNumericField(ConfigField field)
    {
        switch (field)
        {
            case ConfigField.Epochs:
                Epochs = Math.Max(Epochs, 1);
                break;
            case ConfigField.BatchSize:
                BatchSize = Math.Max(BatchSize, 1);
                break;
            case ConfigField.HiddenSize:
                HiddenSize = Math.Max(HiddenSize, 1);
                break;
            case ConfigField.EmbedSize:
                EmbedSize = Math.Max(EmbedSize, 1);
                break;
            case ConfigField.LearningRate:
                LearningRate = Math.Max(LearningRate, double.Epsilon);
                break;
        }
    }

    private bool TryAdjustNumericField(ConfigField field, bool increment)
    {
        if (!IsNumericField(field))
            return false;

        switch (field)
        {
            case ConfigField.Epochs:
                Epochs = increment ? Epochs + 1 : Math.Max(1, Epochs - 1);
                break;
            case ConfigField.BatchSize:
                BatchSize = increment ? BatchSize + 1 : Math.Max(1, BatchSize - 1);
                break;
            case ConfigField.HiddenSize:
                HiddenSize = increment ? HiddenSize + 1 : Math.Max(1, HiddenSize - 1);
                break;
            case ConfigField.EmbedSize:
                EmbedSize = increment ? EmbedSize + 1 : Math.Max(1, EmbedSize - 1);
                break;
            case ConfigField.LearningRate:
                LearningRate = increment
                    ? LearningRate * 1.1
                    : Math.Max(double.Epsilon, LearningRate / 1.1);
                break;
        }

        UpdateResumeStateFromCheckpoint();
        return true;
    }

    private void EditDataPath()
    {
        var path = FilePicker.PromptForFile("Select word list (one word per line)");
        if (path is not null)
        {
            DataPath = path;
            UpdateResumeStateFromCheckpoint();
        }
    }

    private void EditOutputDirectory()
    {
        var path = FilePicker.PromptForDirectory("Select model output directory");
        if (path is not null)
        {
            OutputDirectory = path;
            TryLoadCheckpointFromOutputDirectory();
        }
    }

    private bool TryResolveCheckpointAction(out string error)
    {
        error = string.Empty;

        if (_loadedCheckpoint is null)
        {
            _resumeFromCheckpoint = false;
            CompletedEpochs = 0;
            return true;
        }

        if (IsCheckpointLockedConfigurationChanged())
        {
            AnsiConsole.MarkupLine(
                $"[yellow]A trained model exists in[/] [cyan]{Markup.Escape(OutputDirectory)}[/][yellow], " +
                "but critical checkpoint settings were changed.[/]");
            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Starting training will overwrite the existing model.[/]")
                    .AddChoices(
                        "Overwrite and train from scratch",
                        "Cancel"));

            if (action == "Overwrite and train from scratch")
            {
                _resumeFromCheckpoint = false;
                CompletedEpochs = 0;
                return true;
            }

            return false;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]A trained model already exists in[/] [cyan]{Markup.Escape(OutputDirectory)}[/]");
        AnsiConsole.WriteLine();

        var resumeAction = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .AddChoices(
                    "Continue training from checkpoint",
                    "Overwrite and train from scratch",
                    "Cancel"));

        switch (resumeAction)
        {
            case "Continue training from checkpoint":
                _resumeFromCheckpoint = true;
                return true;
            case "Overwrite and train from scratch":
                _resumeFromCheckpoint = false;
                CompletedEpochs = 0;
                return true;
            default:
                return false;
        }
    }

    private void TryLoadCheckpointFromOutputDirectory()
    {
        try
        {
            if (!ModelCheckpoint.Exists(OutputDirectory))
            {
                ClearCheckpointState();
                return;
            }

            var metadata = ModelCheckpoint.LoadMetadata(OutputDirectory);
            ApplyCheckpointMetadata(metadata);
            _loadedCheckpoint = metadata;
            UpdateResumeStateFromCheckpoint();
        }
        catch
        {
            ClearCheckpointState();
        }
    }

    private void ApplyCheckpointMetadata(ModelMetadata metadata)
    {
        EmbedSize = metadata.EmbedSize;
        HiddenSize = metadata.HiddenSize;
        CompletedEpochs = metadata.CompletedEpochs;

        if (metadata.BatchSize > 0)
            BatchSize = metadata.BatchSize;

        if (metadata.LearningRate > 0)
            LearningRate = metadata.LearningRate;

        if (!string.IsNullOrWhiteSpace(metadata.DataPath) && File.Exists(metadata.DataPath))
            DataPath = metadata.DataPath;
    }

    private void UpdateResumeStateFromCheckpoint()
    {
        if (_loadedCheckpoint is null)
        {
            _resumeFromCheckpoint = false;
            CompletedEpochs = 0;
            return;
        }

        if (CanResumeFromLoadedCheckpoint())
        {
            _resumeFromCheckpoint = true;
            CompletedEpochs = _loadedCheckpoint.CompletedEpochs;
            return;
        }

        _resumeFromCheckpoint = false;
        CompletedEpochs = _loadedCheckpoint.CompletedEpochs;
    }

    private bool CanResumeFromLoadedCheckpoint()
    {
        if (_loadedCheckpoint is null || string.IsNullOrWhiteSpace(DataPath) || !File.Exists(DataPath))
            return false;

        if (IsCheckpointLockedConfigurationChanged())
            return false;

        try
        {
            var dataset = new WordDataset(DataPath);
            CheckpointCompatibility.ValidateForResume(dataset, _loadedCheckpoint);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsCheckpointLockedConfigurationChanged()
    {
        if (_loadedCheckpoint is null)
            return false;

        return HiddenSize != _loadedCheckpoint.HiddenSize ||
               EmbedSize != _loadedCheckpoint.EmbedSize;
    }

    private void ClearCheckpointState()
    {
        _loadedCheckpoint = null;
        _resumeFromCheckpoint = false;
        CompletedEpochs = 0;
    }

    private static string FormatPath(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? "[dim](not set)[/]"
            : Markup.Escape(path);

    private bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(DataPath))
        {
            error = "Select a data file before starting training.";
            return false;
        }

        if (!File.Exists(DataPath))
        {
            error = $"Data file not found: {DataPath}";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
