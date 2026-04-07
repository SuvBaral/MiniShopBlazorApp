using GitSimple.Core.Models;

namespace GitSimple.Core.Services;

// Delimiter: ASCII Unit Separator (\x1F, char 31) — coordinated with extension.ts --format strings.
// This character cannot appear in git refs or branch names.
public static class BranchParser
{
    public static (List<Branch> Local, Dictionary<string, List<Branch>> Remotes) ParseBranches(string output)
    {
        var local = new List<Branch>();
        var remotes = new Dictionary<string, List<Branch>>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split('\x1f');
            if (parts.Length < 4) continue;

            var rawName = parts[0].Trim();
            var tracking = string.IsNullOrEmpty(parts[2]) ? null : parts[2].Trim();
            var isCurrent = parts[3].Trim() == "*";

            // Remote-tracking branches start with "remotes/" in the raw refname:short output
            if (rawName.StartsWith("remotes/"))
            {
                var name = rawName[8..]; // strip "remotes/"
                var slashIdx = name.IndexOf('/');
                if (slashIdx < 0) continue; // malformed
                var remoteName = name[..slashIdx];
                var branchPart = name[(slashIdx + 1)..];
                if (branchPart == "HEAD") continue; // skip remote HEAD tracking refs
                if (!remotes.ContainsKey(remoteName))
                    remotes[remoteName] = new List<Branch>();
                remotes[remoteName].Add(new Branch { Name = name, IsLocal = false, IsCurrent = false, RemoteName = remoteName });
            }
            else if (rawName.Contains('/') && tracking == null)
            {
                // Plain "origin/main" style remote ref (no "remotes/" prefix, no local tracking)
                var slashIdx = rawName.IndexOf('/');
                var remoteName = rawName[..slashIdx];
                var branchPart = rawName[(slashIdx + 1)..];
                if (branchPart == "HEAD") continue; // skip remote HEAD tracking refs
                if (!remotes.ContainsKey(remoteName))
                    remotes[remoteName] = new List<Branch>();
                remotes[remoteName].Add(new Branch { Name = rawName, IsLocal = false, IsCurrent = false, RemoteName = remoteName });
            }
            else
            {
                // Local branch — may contain '/' (e.g. feature/auth) if it has a tracking branch
                local.Add(new Branch { Name = rawName, IsLocal = true, IsCurrent = isCurrent, TrackingBranch = tracking });
            }
        }

        return (local, remotes);
    }

    public static List<StashEntry> ParseStashes(string output)
    {
        var stashes = new List<StashEntry>();
        var index = 0;
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\x1f');
            // Format: \x1fmessage\x1fdate — parts[0] is empty, parts[1]=message, parts[2]=date
            var msg = parts.Length > 1 ? parts[1].Trim() : line.Trim();
            var date = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            stashes.Add(new StashEntry { Index = index++, Message = msg, Date = date });
        }
        return stashes;
    }

    public static SyncInfo? ParseSyncInfo(string output)
    {
        var parts = output.Trim().Split('\t');
        if (parts.Length == 2 && int.TryParse(parts[0], out var ahead) && int.TryParse(parts[1], out var behind))
            return new SyncInfo { Ahead = ahead, Behind = behind };
        return null;
    }
}
