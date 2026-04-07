namespace GitBuddy.UI.Models;

public record TagInfo(
    string Name,
    string Hash,
    string? Message,
    string? Tagger,
    string? Date,
    bool IsAnnotated
);
