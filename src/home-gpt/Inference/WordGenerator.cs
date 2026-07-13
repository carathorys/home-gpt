using home_gpt.Data;
using home_gpt.Models;
using home_gpt.Persistence;
using Spectre.Console;
using TorchSharp;
using static TorchSharp.torch;

namespace home_gpt.Inference;

public sealed class WordGenerator : IDisposable
{
    private readonly CharLanguageModel _model;
    private readonly CharVocab _vocab;
    private readonly Device _device;
    private readonly int _maxLength;

    private WordGenerator(CharLanguageModel model, CharVocab vocab, Device device, int maxLength)
    {
        _model = model;
        _vocab = vocab;
        _device = device;
        _maxLength = maxLength;
    }

    public static WordGenerator Load(string modelDirectory)
    {
        if (!ModelCheckpoint.Exists(modelDirectory))
            throw new FileNotFoundException($"No trained model found in '{modelDirectory}'.");

        var metadata = ModelCheckpoint.LoadMetadata(modelDirectory);
        var vocab = CharVocab.FromJson(metadata.VocabJson);
        var device = cuda.is_available() ? CUDA : CPU;

        var model = new CharLanguageModel(metadata.VocabSize, metadata.EmbedSize, metadata.HiddenSize);
        model.load(ModelCheckpoint.WeightsPath(modelDirectory));
        model.to(device);
        model.eval();

        return new WordGenerator(model, vocab, device, metadata.SequenceLength - 1);
    }

    public string Generate(string prefix, double temperature = 0.8)
    {
        prefix = prefix.Trim();
        ValidatePrefix(prefix);

        var indices = prefix.Length == 0
            ? new List<long> { CharVocab.PadIndex }
            : new List<long>(_vocab.EncodeWord(prefix));
        var generated = new System.Text.StringBuilder(prefix);

        using var _ = no_grad();
        for (var step = prefix.Length; step < _maxLength; step++)
        {
            using var input = tensor(indices.ToArray(), new long[] { 1, indices.Count }, dtype: ScalarType.Int64)
                .to(_device);
            using var logits = _model.call(input);
            using var nextLogits = logits[0, indices.Count - 1];

            var nextIndex = SampleIndex(nextLogits, temperature);
            if (nextIndex is CharVocab.EosIndex or CharVocab.PadIndex)
                break;

            indices.Add(nextIndex);
            generated.Append(_vocab.Decode((int)nextIndex));
        }

        return generated.ToString();
    }

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
                var word = Generate(prefix);
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

    private void ValidatePrefix(string prefix)
    {
        foreach (var ch in prefix)
        {
            if (!_vocab.Contains(ch))
                throw new ArgumentException($"Character '{ch}' was not seen during training.");
        }
    }

    private static long SampleIndex(Tensor logits, double temperature)
    {
        if (temperature <= 0)
            return logits.argmax().item<long>();

        using var scaled = logits / temperature;
        using var probs = scaled.softmax(dim: 0);
        return probs.multinomial(1, replacement: true).item<long>();
    }

    public void Dispose() => _model.Dispose();
}
