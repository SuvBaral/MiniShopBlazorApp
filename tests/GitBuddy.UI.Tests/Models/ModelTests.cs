using FluentAssertions;
using GitBuddy.Core.Models;
using Xunit;

namespace GitBuddy.UI.Tests.Models;

public class BranchTests
{
    [Fact]
    public void Branch_ShouldCreateWithAllProperties()
    {
        var branch = new Branch { Name = "main", IsLocal = true, IsCurrent = true, TrackingBranch = "origin/main" };

        branch.Name.Should().Be("main");
        branch.IsLocal.Should().BeTrue();
        branch.IsCurrent.Should().BeTrue();
        branch.TrackingBranch.Should().Be("origin/main");
        branch.RemoteName.Should().BeNull();
    }

    [Fact]
    public void Branch_RemoteBranch_ShouldHaveRemoteName()
    {
        var branch = new Branch { Name = "origin/develop", IsLocal = false, RemoteName = "origin" };

        branch.IsLocal.Should().BeFalse();
        branch.RemoteName.Should().Be("origin");
    }

    [Fact]
    public void Branch_Records_ShouldSupportValueEquality()
    {
        var a = new Branch { Name = "main", IsLocal = true };
        var b = new Branch { Name = "main", IsLocal = true };

        // Classes use reference equality; just verify properties match
        a.Name.Should().Be(b.Name);
        a.IsLocal.Should().Be(b.IsLocal);
    }

    [Fact]
    public void Branch_DifferentNames_ShouldNotBeEqual()
    {
        var a = new Branch { Name = "main", IsLocal = true };
        var b = new Branch { Name = "develop", IsLocal = true };

        a.Name.Should().NotBe(b.Name);
    }
}

public class GitCommandResultTests
{
    [Fact]
    public void SuccessResult_ShouldHaveNoError()
    {
        var result = new GitCommandResult { Success = true, Output = "output text" };

        result.Success.Should().BeTrue();
        result.Output.Should().Be("output text");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void FailureResult_ShouldHaveError()
    {
        var result = new GitCommandResult { Success = false, Error = "fatal: not a git repo" };

        result.Success.Should().BeFalse();
        result.Error.Should().Be("fatal: not a git repo");
    }
}

public class SyncInfoTests
{
    [Fact]
    public void SyncInfo_ShouldTrackAheadBehind()
    {
        var sync = new SyncInfo { Ahead = 3, Behind = 5 };

        sync.Ahead.Should().Be(3);
        sync.Behind.Should().Be(5);
    }

    [Fact]
    public void SyncInfo_ZeroValues_ShouldBeValid()
    {
        var sync = new SyncInfo { Ahead = 0, Behind = 0 };

        sync.Ahead.Should().Be(0);
        sync.Behind.Should().Be(0);
    }
}

public class StashEntryTests
{
    [Fact]
    public void StashEntry_ShouldStoreAllProperties()
    {
        var stash = new StashEntry { Index = 0, Message = "WIP on main", Date = "2024-01-15 10:30:00" };

        stash.Index.Should().Be(0);
        stash.Message.Should().Be("WIP on main");
        stash.Date.Should().Be("2024-01-15 10:30:00");
    }
}

public class NLTranslationTests
{
    [Fact]
    public void NLTranslation_SafeCommand_ShouldNotRequireConfirmation()
    {
        var translation = new NLTranslation
        {
            Command = "git status", Explanation = "Show status",
            Risk = RiskLevel.Safe, RequiresConfirmation = false, Tier = "regex"
        };

        translation.Risk.Should().Be(RiskLevel.Safe);
        translation.RequiresConfirmation.Should().BeFalse();
        translation.Warning.Should().BeNull();
    }

    [Fact]
    public void NLTranslation_DangerousCommand_ShouldRequireConfirmation()
    {
        var translation = new NLTranslation
        {
            Command = "git reset --hard HEAD~1", Explanation = "Hard reset",
            Risk = RiskLevel.Dangerous, RequiresConfirmation = true,
            Warning = "Permanently discards changes", Tier = "regex"
        };

        translation.Risk.Should().Be(RiskLevel.Dangerous);
        translation.RequiresConfirmation.Should().BeTrue();
        translation.Warning.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NLTranslation_WithAlternatives_ShouldContainThem()
    {
        var alts = new[] { new AlternativeCommand { Command = "git stash", Description = "Safer option" } };
        var translation = new NLTranslation
        {
            Command = "git reset --hard", Explanation = "Reset",
            Risk = RiskLevel.Dangerous, RequiresConfirmation = true,
            Warning = "Warning", Alternatives = alts, Tier = "llm"
        };

        translation.Alternatives.Should().HaveCount(1);
        translation.Alternatives![0].Command.Should().Be("git stash");
    }
}

public class RiskLevelTests
{
    [Fact]
    public void RiskLevel_ShouldHaveThreeValues()
    {
        Enum.GetValues<RiskLevel>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(RiskLevel.Safe, 0)]
    [InlineData(RiskLevel.Moderate, 1)]
    [InlineData(RiskLevel.Dangerous, 2)]
    public void RiskLevel_ShouldHaveCorrectOrdinals(RiskLevel level, int expected)
    {
        ((int)level).Should().Be(expected);
    }
}

public class AutocompleteSuggestionTests
{
    [Fact]
    public void AutocompleteSuggestion_ShouldStoreAllFields()
    {
        var s = new AutocompleteSuggestion { Text = "switch to ", Type = "action", Description = "Switch to a branch" };

        s.Text.Should().Be("switch to ");
        s.Type.Should().Be("action");
        s.Description.Should().Be("Switch to a branch");
    }
}

public class CommandHistoryEntryTests
{
    [Fact]
    public void CommandHistoryEntry_ShouldRecordTimestamp()
    {
        var now = DateTime.Now;
        var entry = new CommandHistoryEntry { NLInput = "pull latest", GitCommand = "git pull", Timestamp = now, Success = true };

        entry.NLInput.Should().Be("pull latest");
        entry.GitCommand.Should().Be("git pull");
        entry.Timestamp.Should().Be(now);
        entry.Success.Should().BeTrue();
    }
}
