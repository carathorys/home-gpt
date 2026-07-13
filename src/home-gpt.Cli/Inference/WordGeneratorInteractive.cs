using System.Diagnostics.CodeAnalysis;
using home_gpt.Inference;
using Spectre.Console;

namespace home_gpt.Cli.Inference;

[ExcludeFromCodeCoverage]
public sealed class WordGeneratorInteractive : IDisposable
{
    private readonly WordGenerationService _service;

    private WordGeneratorInteractive(WordGenerationService service) => _service = service;

    public static WordGeneratorInteractive Load(string modelDirectory) =>
        new(WordGenerationService.Load(modelDirectory));

    public void RunInteractive()
    {
        AnsiConsole.MarkupLine("[dim]Enter prefixes to complete words. Press [yellow]Esc[/] for options.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var prefix = ReadPrefixOrOpenMenu();
            if (prefix is null)
                break;

            try
            {
                var word = _service.Generate(prefix);
                AnsiConsole.MarkupLine($"[green]Generated:[/] [bold cyan]{Markup.Escape(word)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }

            AnsiConsole.WriteLine();
        }
    }

    private static string? ReadPrefixOrOpenMenu()
    {
        while (true)
        {
            AnsiConsole.Markup("[bold]Prefix[/]: ");
            var builder = new System.Text.StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return ShowEscapeMenu() ? string.Empty : null;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return builder.ToString().Trim();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length == 0)
                        continue;

                    builder.Length--;
                    Console.Write("\b \b");
                    continue;
                }

                if (char.IsControl(key.KeyChar))
                    continue;

                builder.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }
    }

    private static bool ShowEscapeMenu()
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Generate words[/]")
                .AddChoices("Continue entering prefix", "Back to main menu"));

        return action == "Continue entering prefix";
    }

    public void Dispose() => _service.Dispose();
}
