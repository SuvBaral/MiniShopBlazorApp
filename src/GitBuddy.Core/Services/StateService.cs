using GitBuddy.Core.Models;

namespace GitBuddy.Core.Services;

public class StateService
{
    public string RepoName { get; set; } = string.Empty;
    public string CurrentBranch { get; set; } = string.Empty;
    public List<Branch> LocalBranches { get; set; } = new();
    public Dictionary<string, List<Branch>> RemoteBranches { get; set; } = new();
    public List<StashEntry> Stashes { get; set; } = new();
    public SyncInfo? Sync { get; set; }
    public bool IsLoading { get; set; }

    public event Action? OnStateChanged;

    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}
