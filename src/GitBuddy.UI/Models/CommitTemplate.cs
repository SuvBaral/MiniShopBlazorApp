namespace GitBuddy.UI.Models;

public record CommitTemplate(
    string Name,
    string Format,
    string Description
)
{
    public static readonly CommitTemplate[] BuiltIn = new[]
    {
        new CommitTemplate("Conventional", "{type}({scope}): {description}", "feat(auth): add login validation"),
        new CommitTemplate("Simple", "{description}", "Fix navbar alignment issue"),
        new CommitTemplate("Jira", "[{ticket}] {description}", "[PROJ-123] Add user settings page"),
        new CommitTemplate("Emoji", "{emoji} {description}", "🐛 Fix race condition in data loader"),
        new CommitTemplate("Angular", "{type}({scope}): {description}\n\n{body}\n\n{footer}", "fix(core): resolve memory leak\n\nDescription of fix\n\nCloses #123")
    };
}
