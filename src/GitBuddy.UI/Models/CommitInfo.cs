namespace GitBuddy.UI.Models;

public record CommitInfo(
    string Hash,
    string ShortHash,
    string Message,
    string Author,
    string AuthorEmail,
    string Date,
    string[] Parents,
    string[] Refs
);

public record CommitDetail(
    CommitInfo Commit,
    List<FileChange> Files,
    string FullMessage
);
