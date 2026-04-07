namespace GitBuddy.UI.Models;

public record FileChange(
    string Path,
    string FileName,
    FileChangeStatus Status,
    int Additions,
    int Deletions,
    bool IsStaged,
    List<DiffHunk>? Hunks
);

public enum FileChangeStatus
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied,
    Untracked,
    Conflicted
}

public record DiffHunk(
    int OldStart,
    int OldCount,
    int NewStart,
    int NewCount,
    string Header,
    List<DiffLine> Lines
);

public record DiffLine(
    DiffLineType Type,
    string Content,
    int? OldLineNumber,
    int? NewLineNumber,
    bool IsSelected
);

public enum DiffLineType
{
    Context,
    Added,
    Removed,
    Header
}
