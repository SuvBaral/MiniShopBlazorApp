using GitBuddy.Core.Models;
using GitBuddy.Core.Services;
using GitBuddy.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GitBuddy.UI.Components.BranchManagement;

public partial class BranchPanel : ComponentBase, IDisposable
{
    [Inject] private GitService Git { get; set; } = default!;
    [Inject] private StateService _state { get; set; } = default!;
    [Inject] private VsCodeBridgeService Bridge { get; set; } = default!;

    [Parameter] public EventCallback<string> OnToast { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public EventCallback<(string Title, string Message, Func<Task> OnConfirm)> OnConfirmRequest { get; set; }
    [Parameter] public EventCallback<(string Title, string Placeholder, Func<string, Task> OnSubmit)> OnInputRequest { get; set; }

    private bool _showContextMenu;
    private double _menuX, _menuY;
    private Branch? _contextBranch;
    private StashEntry? _contextStash;

    internal string RepoDisplayName =>
        _state.RepoName.Contains('/') || _state.RepoName.Contains('\\')
            ? System.IO.Path.GetFileName(_state.RepoName)
            : _state.RepoName;

    protected override async Task OnInitializedAsync()
    {
        _state.OnStateChanged += StateChanged;
        // MainLayout already coordinates RefreshAll on repo change — no extra subscription needed here
        await RefreshAll();
    }

    public async Task RefreshAll()
    {
        _state.IsLoading = true;
        _state.NotifyStateChanged();

        try
        {
            var repoResult = await Git.GetRepoName();
            if (repoResult?.Success == true) _state.RepoName = repoResult.Output.Trim();

            var currentResult = await Git.GetCurrentBranch();
            if (currentResult?.Success == true) _state.CurrentBranch = currentResult.Output.Trim();

            var branchResult = await Git.GetBranches();
            if (branchResult?.Success == true) ParseBranches(branchResult.Output);

            var stashResult = await Git.StashList();
            if (stashResult?.Success == true) ParseStashes(stashResult.Output);

            var syncResult = await Git.GetSyncInfo();
            if (syncResult?.Success == true) ParseSyncInfo(syncResult.Output);
        }
        catch { /* swallow — extension host may not be ready */ }

        _state.IsLoading = false;
        _state.NotifyStateChanged();
    }

    private void ParseBranches(string output)
    {
        var (local, remotes) = BranchParser.ParseBranches(output);
        _state.LocalBranches = local;
        _state.RemoteBranches = remotes;
    }

    private void ParseStashes(string output)
    {
        _state.Stashes = BranchParser.ParseStashes(output);
    }

    private void ParseSyncInfo(string output)
    {
        _state.Sync = BranchParser.ParseSyncInfo(output);
    }

    private async Task HandleBranchSelected(Branch branch)
    {
        if (branch.IsCurrent) return;
        var result = await Git.Checkout(branch.Name);
        if (result?.Success == true)
            await OnToast.InvokeAsync($"Switched to {branch.Name}");
        else
            await OnError.InvokeAsync(result?.Error ?? "Checkout failed");
        await RefreshAll();
    }

    private void HandleContextMenu((Branch Branch, double X, double Y) args)
    {
        _contextBranch = args.Branch;
        _contextStash = null;
        _menuX = args.X;
        _menuY = args.Y;
        _showContextMenu = true;
    }

    private void HandleStashContextMenu(StashEntry stash, MouseEventArgs e)
    {
        _contextStash = stash;
        _contextBranch = null;
        _menuX = e.ClientX;
        _menuY = e.ClientY;
        _showContextMenu = true;
    }

    private void CloseContextMenu() => _showContextMenu = false;

    private async Task ContextCheckout()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var result = await Git.Checkout(_contextBranch.Name);
        if (result?.Success == true) await OnToast.InvokeAsync($"Switched to {_contextBranch.Name}");
        else await OnError.InvokeAsync(result?.Error ?? "Checkout failed");
        await RefreshAll();
    }

    private async Task ContextPull()
    {
        CloseContextMenu();
        var result = await Git.Pull();
        if (result?.Success == true) await OnToast.InvokeAsync("Pull complete");
        else await OnError.InvokeAsync(result?.Error ?? "Pull failed");
        await RefreshAll();
    }

    private async Task ContextPush()
    {
        CloseContextMenu();
        var result = await Git.Push();
        if (result?.Success == true) await OnToast.InvokeAsync("Push complete");
        else await OnError.InvokeAsync(result?.Error ?? "Push failed");
        await RefreshAll();
    }

    private async Task ContextMerge()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var branch = _contextBranch;
        await OnConfirmRequest.InvokeAsync((
            "Merge Branch",
            $"Merge {branch.Name} into {_state.CurrentBranch}?",
            async () =>
            {
                var result = await Git.Merge(branch.Name);
                if (result?.Success == true) await OnToast.InvokeAsync($"Merged {branch.Name}");
                else await OnError.InvokeAsync(result?.Error ?? "Merge failed");
                await RefreshAll();
            }
        ));
    }

    private async Task ContextRebase()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var branch = _contextBranch;
        await OnConfirmRequest.InvokeAsync((
            "Rebase Branch",
            $"Rebase {_state.CurrentBranch} onto {branch.Name}?",
            async () =>
            {
                var result = await Git.Rebase(branch.Name);
                if (result?.Success == true) await OnToast.InvokeAsync($"Rebased onto {branch.Name}");
                else await OnError.InvokeAsync(result?.Error ?? "Rebase failed");
                await RefreshAll();
            }
        ));
    }

    private async Task ShowNewBranchDialog()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var fromBranch = _contextBranch.Name;
        await OnInputRequest.InvokeAsync((
            "New Branch",
            "Enter branch name",
            async (name) =>
            {
                var result = await Git.CreateBranch(name, fromBranch);
                if (result?.Success == true) await OnToast.InvokeAsync($"Created branch {name}");
                else await OnError.InvokeAsync(result?.Error ?? "Create branch failed");
                await RefreshAll();
            }
        ));
    }

    private async Task ShowRenameDialog()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var oldName = _contextBranch.Name;
        await OnInputRequest.InvokeAsync((
            "Rename Branch",
            $"New name for {oldName}",
            async (newName) =>
            {
                var result = await Git.RenameBranch(oldName, newName);
                if (result?.Success == true) await OnToast.InvokeAsync($"Renamed to {newName}");
                else await OnError.InvokeAsync(result?.Error ?? "Rename failed");
                await RefreshAll();
            }
        ));
    }

    private async Task ContextDelete()
    {
        CloseContextMenu();
        if (_contextBranch is null) return;
        var branch = _contextBranch;
        await OnConfirmRequest.InvokeAsync((
            "Delete Branch",
            $"Delete branch {branch.Name}? This cannot be undone if not merged.",
            async () =>
            {
                var result = await Git.DeleteBranch(branch.Name);
                if (result?.Success == true) await OnToast.InvokeAsync($"Deleted {branch.Name}");
                else await OnError.InvokeAsync(result?.Error ?? "Delete failed");
                await RefreshAll();
            }
        ));
    }

    private async Task ContextStashApply()
    {
        CloseContextMenu();
        if (_contextStash is null) return;
        var result = await Git.StashApply(_contextStash.Index);
        if (result?.Success == true) await OnToast.InvokeAsync("Stash applied");
        else await OnError.InvokeAsync(result?.Error ?? "Apply failed");
        await RefreshAll();
    }

    private async Task ContextStashPop()
    {
        CloseContextMenu();
        if (_contextStash is null) return;
        var idx = _contextStash.Index;
        var apply = await Git.StashApply(idx);
        if (apply?.Success == true)
        {
            await Git.StashDrop(idx);
            await OnToast.InvokeAsync("Stash popped");
        }
        else await OnError.InvokeAsync(apply?.Error ?? "Pop failed");
        await RefreshAll();
    }

    private async Task ContextStashDrop()
    {
        CloseContextMenu();
        if (_contextStash is null) return;
        var stash = _contextStash;
        await OnConfirmRequest.InvokeAsync((
            "Drop Stash",
            $"Drop stash@{{{stash.Index}}}? This cannot be undone.",
            async () =>
            {
                var result = await Git.StashDrop(stash.Index);
                if (result?.Success == true) await OnToast.InvokeAsync("Stash dropped");
                else await OnError.InvokeAsync(result?.Error ?? "Drop failed");
                await RefreshAll();
            }
        ));
    }

    private void StateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => _state.OnStateChanged -= StateChanged;
}
