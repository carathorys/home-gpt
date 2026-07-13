namespace home_gpt.Training;

public enum CheckpointPromptKind
{
    None,
    ArchitectureChanged,
    ExistingCheckpoint
}

public enum TrainingStartChoice
{
    ContinueFromCheckpoint,
    OverwriteFromScratch,
    Cancel
}

public static class TrainingStartResolver
{
    public static CheckpointPromptKind GetPromptKind(TrainingConfigState state)
    {
        if (state.LoadedCheckpoint is null)
            return CheckpointPromptKind.None;

        return state.IsArchitectureChanged()
            ? CheckpointPromptKind.ArchitectureChanged
            : CheckpointPromptKind.ExistingCheckpoint;
    }

    public static bool TryApplyChoice(TrainingConfigState state, TrainingStartChoice choice)
    {
        switch (choice)
        {
            case TrainingStartChoice.ContinueFromCheckpoint:
                state.EnableResume();
                return true;
            case TrainingStartChoice.OverwriteFromScratch:
                state.ClearResume();
                return true;
            case TrainingStartChoice.Cancel:
                return false;
            default:
                return false;
        }
    }
}
