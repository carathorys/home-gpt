using Spectre.Console;

namespace home_gpt.Ui;

public static class FilePicker
{
    private const string ParentLabel = ".. (parent directory)";
    private const string SelectHereLabel = "Select this directory";
    private const string CancelLabel = "Cancel";

    private enum EntryKind { Cancel, Parent, SelectHere, Directory, File }

    private sealed record DirEntry(string Name, EntryKind Kind);

    public static string? PromptForFile(string title = "Select file")
    {
        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{title}[/]")
                .AddChoices("Browse directory", "Enter path", "Cancel"));

        return mode switch
        {
            "Browse directory" => BrowseForFile(),
            "Enter path" => PromptForPath(),
            _ => null
        };
    }

    public static string? PromptForDirectory(string title = "Select directory")
    {
        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{title}[/]")
                .AddChoices("Browse directory", "Enter path", "Cancel"));

        return mode switch
        {
            "Browse directory" => BrowseForDirectory(),
            "Enter path" => PromptForDirectoryPath(),
            _ => null
        };
    }

    private static string? BrowseForFile()
    {
        var startDir = Directory.GetCurrentDirectory();

        while (true)
        {
            var entries = Directory.GetFileSystemEntries(startDir)
                .OrderBy(Path.GetFileName)
                .ToArray();

            var items = new List<DirEntry>
            {
                new(CancelLabel, EntryKind.Cancel),
                new(ParentLabel, EntryKind.Parent)
            };
            items.AddRange(entries.Select(e =>
            {
                var name = Path.GetFileName(e) ?? e;
                return new DirEntry(name, Directory.Exists(e) ? EntryKind.Directory : EntryKind.File);
            }));

            var selection = PromptForEntry(startDir, items);

            if (selection.Kind == EntryKind.Cancel)
                return null;

            if (selection.Kind == EntryKind.Parent)
            {
                var parent = Directory.GetParent(startDir)?.FullName;
                if (parent is not null)
                    startDir = parent;
                continue;
            }

            var fullPath = Path.Combine(startDir, selection.Name);

            if (selection.Kind == EntryKind.Directory)
            {
                startDir = fullPath;
                continue;
            }

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLine("[red]File not found.[/]");
                continue;
            }

            AnsiConsole.MarkupLine($"[green]Selected:[/] {Markup.Escape(fullPath)}");
            return fullPath;
        }
    }

    private static string? BrowseForDirectory()
    {
        var startDir = Directory.GetCurrentDirectory();

        while (true)
        {
            var entries = Directory.GetDirectories(startDir)
                .OrderBy(Path.GetFileName)
                .ToArray();

            var items = new List<DirEntry>
            {
                new(CancelLabel, EntryKind.Cancel),
                new(ParentLabel, EntryKind.Parent),
                new(SelectHereLabel, EntryKind.SelectHere)
            };
            items.AddRange(entries.Select(d =>
                new DirEntry(Path.GetFileName(d) ?? d, EntryKind.Directory)));

            var selection = PromptForEntry(startDir, items);

            if (selection.Kind == EntryKind.Cancel)
                return null;

            if (selection.Kind == EntryKind.Parent)
            {
                var parent = Directory.GetParent(startDir)?.FullName;
                if (parent is not null)
                    startDir = parent;
                continue;
            }

            if (selection.Kind == EntryKind.SelectHere)
            {
                AnsiConsole.MarkupLine($"[green]Selected:[/] {Markup.Escape(startDir)}");
                return startDir;
            }

            startDir = Path.Combine(startDir, selection.Name);
        }
    }

    private static DirEntry PromptForEntry(string directory, IReadOnlyList<DirEntry> items) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<DirEntry>()
                .Title($"[bold]Directory:[/] [dim]{Markup.Escape(directory)}[/]")
                .PageSize(15)
                .UseConverter(FormatEntry)
                .AddChoices(items));

    private static string FormatEntry(DirEntry entry) => entry.Kind switch
    {
        EntryKind.Cancel => $"[red]{CancelLabel}[/]",
        EntryKind.Parent => $"[dim]{ParentLabel}[/]",
        EntryKind.SelectHere => $"[green]{SelectHereLabel}[/]",
        EntryKind.Directory => $"[blue]{Markup.Escape(entry.Name)}/[/]",
        _ => Markup.Escape(entry.Name)
    };

    private static string? PromptForPath()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Path to file[/]")
                .Validate(p =>
                    File.Exists(p)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]File does not exist.[/]")));
    }

    private static string? PromptForDirectoryPath()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Path to directory[/]")
                .Validate(p =>
                    Directory.Exists(p)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Directory does not exist.[/]")));
    }
}
