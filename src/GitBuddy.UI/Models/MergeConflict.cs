namespace GitBuddy.UI.Models;

public record MergeConflict(
    string FilePath,
    string FileName,
    List<ConflictHunk> Hunks
);

public record ConflictHunk(
    int StartLine,
    string CurrentContent,
    string IncomingContent,
    string? BaseContent,
    ConflictResolution Resolution
);

public enum ConflictResolution
{
    Unresolved,
    AcceptCurrent,
    AcceptIncoming,
    AcceptBoth,
    Custom
}
