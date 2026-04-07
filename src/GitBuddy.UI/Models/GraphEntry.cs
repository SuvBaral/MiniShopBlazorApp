namespace GitBuddy.UI.Models;

public record GraphEntry(
    CommitInfo Commit,
    int Column,
    List<GraphConnection> Connections,
    string BranchColor
);

public record GraphConnection(
    int FromColumn,
    int ToColumn,
    string Color,
    bool IsMerge
);
