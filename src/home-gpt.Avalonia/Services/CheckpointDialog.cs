using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using home_gpt.Training;

namespace home_gpt.Avalonia.Services;

[ExcludeFromCodeCoverage]
public sealed class CheckpointDialog : Window
{
    private readonly TaskCompletionSource<TrainingStartChoice?> _result = new();

    public CheckpointDialog(CheckpointPromptKind kind, string outputDirectory)
    {
        Title = "Checkpoint";
        Width = 480;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var message = kind switch
        {
            CheckpointPromptKind.ArchitectureChanged =>
                $"A trained model exists in '{outputDirectory}', but critical checkpoint settings were changed.\n\nStarting training will overwrite the existing model.",
            CheckpointPromptKind.ExistingCheckpoint =>
                $"A trained model already exists in '{outputDirectory}'.",
            _ => string.Empty
        };

        Content = new StackPanel
        {
            Margin = new global::Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
                BuildButtons(kind)
            }
        };

        Closing += (_, e) =>
        {
            if (!_result.Task.IsCompleted)
                _result.TrySetResult(TrainingStartChoice.Cancel);
        };
    }

    private Control BuildButtons(CheckpointPromptKind kind)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        if (kind == CheckpointPromptKind.ExistingCheckpoint)
        {
            panel.Children.Add(CreateButton(
                "Continue from checkpoint",
                TrainingStartChoice.ContinueFromCheckpoint));
        }

        panel.Children.Add(CreateButton(
            "Overwrite and train from scratch",
            TrainingStartChoice.OverwriteFromScratch));
        panel.Children.Add(CreateButton("Cancel", TrainingStartChoice.Cancel));

        return panel;
    }

    private Button CreateButton(string text, TrainingStartChoice choice)
    {
        var button = new Button { Content = text };
        button.Click += (_, _) =>
        {
            _result.TrySetResult(choice);
            Close();
        };
        return button;
    }
}
