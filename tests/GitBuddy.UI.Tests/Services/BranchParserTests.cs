using FluentAssertions;
using GitBuddy.Core.Services;
using Xunit;

namespace GitBuddy.UI.Tests.Services;

public class BranchParserTests
{
    [Fact]
    public void ParseBranches_EmptyOutput_ShouldReturnEmptyLists()
    {
        var (local, remotes) = BranchParser.ParseBranches("");

        local.Should().BeEmpty();
        remotes.Should().BeEmpty();
    }

    [Fact]
    public void ParseBranches_SingleLocalBranch_ShouldParse()
    {
        var output = "main\u001fabc1234\u001forigin/main\u001f*";
        var (local, remotes) = BranchParser.ParseBranches(output);

        local.Should().HaveCount(1);
        local[0].Name.Should().Be("main");
        local[0].IsLocal.Should().BeTrue();
        local[0].IsCurrent.Should().BeTrue();
        local[0].TrackingBranch.Should().Be("origin/main");
    }

    [Fact]
    public void ParseBranches_NonCurrentBranch_ShouldNotBeCurrent()
    {
        var output = "develop\u001fdef5678\u001forigin/develop\u001f ";
        var (local, _) = BranchParser.ParseBranches(output);

        local.Should().HaveCount(1);
        local[0].IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void ParseBranches_MultipleLocalBranches_ShouldParseAll()
    {
        var output = "main\u001fabc\u001forigin/main\u001f*\ndevelop\u001fdef\u001forigin/develop\u001f \nfeature-x\u001fghi\u001f\u001f ";
        var (local, _) = BranchParser.ParseBranches(output);

        local.Should().HaveCount(3);
        local[0].Name.Should().Be("main");
        local[1].Name.Should().Be("develop");
        local[2].Name.Should().Be("feature-x");
    }

    [Fact]
    public void ParseBranches_RemoteBranch_ShouldGroupByRemote()
    {
        var output = "origin/main\u001fabc\u001f\u001f \norigin/develop\u001fdef\u001f\u001f ";
        var (local, remotes) = BranchParser.ParseBranches(output);

        local.Should().BeEmpty();
        remotes.Should().ContainKey("origin");
        remotes["origin"].Should().HaveCount(2);
    }

    [Fact]
    public void ParseBranches_MultipleRemotes_ShouldGroupSeparately()
    {
        var output = "origin/main\u001fabc\u001f\u001f \nupstream/main\u001fdef\u001f\u001f ";
        var (_, remotes) = BranchParser.ParseBranches(output);

        remotes.Should().HaveCount(2);
        remotes.Should().ContainKey("origin");
        remotes.Should().ContainKey("upstream");
    }

    [Fact]
    public void ParseBranches_RemotesPrefix_ShouldBeStripped()
    {
        var output = "remotes/origin/main\u001fabc\u001f\u001f ";
        var (_, remotes) = BranchParser.ParseBranches(output);

        remotes.Should().ContainKey("origin");
        remotes["origin"][0].Name.Should().Be("origin/main");
    }

    [Fact]
    public void ParseBranches_MixedLocalAndRemote_ShouldSeparate()
    {
        var output = "main\u001fabc\u001forigin/main\u001f*\ndevelop\u001fdef\u001f\u001f \norigin/main\u001fabc\u001f\u001f \norigin/develop\u001fdef\u001f\u001f ";
        var (local, remotes) = BranchParser.ParseBranches(output);

        local.Should().HaveCount(2);
        remotes["origin"].Should().HaveCount(2);
    }

    [Fact]
    public void ParseBranches_InsufficientParts_ShouldSkipLine()
    {
        var output = "incomplete\u001fdata\nmain\u001fabc\u001forigin/main\u001f*";
        var (local, _) = BranchParser.ParseBranches(output);

        local.Should().HaveCount(1);
        local[0].Name.Should().Be("main");
    }

    [Fact]
    public void ParseBranches_NoTrackingBranch_ShouldBeNull()
    {
        var output = "feature-x\u001fabc\u001f\u001f ";
        var (local, _) = BranchParser.ParseBranches(output);

        local[0].TrackingBranch.Should().BeNull();
    }

    // === ParseStashes ===

    [Fact]
    public void ParseStashes_EmptyOutput_ShouldReturnEmptyList()
    {
        var stashes = BranchParser.ParseStashes("");

        stashes.Should().BeEmpty();
    }

    [Fact]
    public void ParseStashes_SingleStash_ShouldParse()
    {
        // New format: \u001fmessage\u001fdate (stash ref replaced by position index)
        var output = "\u001fWIP on main: abc1234 commit msg\u001f2024-01-15 10:30:00 +0500";
        var stashes = BranchParser.ParseStashes(output);

        stashes.Should().HaveCount(1);
        stashes[0].Index.Should().Be(0);
        stashes[0].Message.Should().Be("WIP on main: abc1234 commit msg");
        stashes[0].Date.Should().Contain("2024-01-15");
    }

    [Fact]
    public void ParseStashes_MultipleStashes_ShouldHaveSequentialIndexes()
    {
        var output = "\u001fFirst stash\u001f2024-01-15\n\u001fSecond stash\u001f2024-01-14\n\u001fThird stash\u001f2024-01-13";
        var stashes = BranchParser.ParseStashes(output);

        stashes.Should().HaveCount(3);
        stashes[0].Index.Should().Be(0);
        stashes[1].Index.Should().Be(1);
        stashes[2].Index.Should().Be(2);
    }

    [Fact]
    public void ParseStashes_NoSeparator_ShouldUseFullLineAsMessage()
    {
        // Fallback: if a line has no \u001f delimiter, use the whole line as message
        var output = "WIP on main: some work";
        var stashes = BranchParser.ParseStashes(output);

        stashes.Should().HaveCount(1);
        stashes[0].Message.Should().Be("WIP on main: some work");
    }

    // === ParseSyncInfo ===

    [Fact]
    public void ParseSyncInfo_ValidOutput_ShouldParse()
    {
        var sync = BranchParser.ParseSyncInfo("3\t5");

        sync.Should().NotBeNull();
        sync!.Ahead.Should().Be(3);
        sync.Behind.Should().Be(5);
    }

    [Fact]
    public void ParseSyncInfo_ZeroValues_ShouldParse()
    {
        var sync = BranchParser.ParseSyncInfo("0\t0");

        sync.Should().NotBeNull();
        sync!.Ahead.Should().Be(0);
        sync.Behind.Should().Be(0);
    }

    [Fact]
    public void ParseSyncInfo_InvalidOutput_ShouldReturnNull()
    {
        var sync = BranchParser.ParseSyncInfo("invalid");

        sync.Should().BeNull();
    }

    [Fact]
    public void ParseSyncInfo_EmptyOutput_ShouldReturnNull()
    {
        var sync = BranchParser.ParseSyncInfo("");

        sync.Should().BeNull();
    }

    [Fact]
    public void ParseSyncInfo_WithWhitespace_ShouldTrimAndParse()
    {
        var sync = BranchParser.ParseSyncInfo("  1\t2  ");

        sync.Should().NotBeNull();
        sync!.Ahead.Should().Be(1);
        sync.Behind.Should().Be(2);
    }

    [Fact]
    public void ParseSyncInfo_NonNumeric_ShouldReturnNull()
    {
        var sync = BranchParser.ParseSyncInfo("abc\tdef");

        sync.Should().BeNull();
    }

    // === delimiter migration (\u001f) ===

    public class DelimiterMigration
    {
        // --- ParseBranches with \u001f delimiter ---

        [Fact]
        public void ParseBranches_UnitSeparator_SingleLocalBranch_ShouldParse()
        {
            var output = "main\u001fabc1234\u001f\u001f*";
            var (local, remotes) = BranchParser.ParseBranches(output);

            local.Should().HaveCount(1);
            local[0].Name.Should().Be("main");
            local[0].IsLocal.Should().BeTrue();
            local[0].IsCurrent.Should().BeTrue();
            local[0].TrackingBranch.Should().BeNull();
            remotes.Should().BeEmpty();
        }

        [Fact]
        public void ParseBranches_UnitSeparator_BranchWithTracking_ShouldParse()
        {
            var output = "feature/auth\u001fdef5678\u001forigin/feature/auth\u001f";
            var (local, _) = BranchParser.ParseBranches(output);

            local.Should().HaveCount(1);
            local[0].Name.Should().Be("feature/auth");
            local[0].TrackingBranch.Should().Be("origin/feature/auth");
            local[0].IsCurrent.Should().BeFalse();
        }

        [Fact]
        public void ParseBranches_UnitSeparator_RemoteBranch_ShouldGroupByRemote()
        {
            var output = "remotes/origin/main\u001fabc1234\u001f\u001f";
            var (local, remotes) = BranchParser.ParseBranches(output);

            local.Should().BeEmpty();
            remotes.Should().ContainKey("origin");
            remotes["origin"].Should().HaveCount(1);
            remotes["origin"][0].Name.Should().Be("origin/main");
        }

        [Fact]
        public void ParseBranches_UnitSeparator_BranchNameContainingPipe_ShouldParseCorrectly()
        {
            // Demonstrates the fix: old '|' split would have broken a branch name containing '|'
            var output = "feat|pipe-test\u001fabc1234\u001f\u001f";
            var (local, _) = BranchParser.ParseBranches(output);

            local.Should().HaveCount(1);
            local[0].Name.Should().Be("feat|pipe-test");
        }

        [Fact]
        public void ParseBranches_UnitSeparator_MixedLocalAndRemote_ShouldSeparate()
        {
            var output = "main\u001fabc1234\u001forigin/main\u001f*\nremotes/origin/main\u001fabc1234\u001f\u001f";
            var (local, remotes) = BranchParser.ParseBranches(output);

            local.Should().HaveCount(1);
            local[0].Name.Should().Be("main");
            remotes.Should().ContainKey("origin");
            remotes["origin"].Should().HaveCount(1);
        }

        [Fact]
        public void ParseBranches_UnitSeparator_EmptyOutput_ShouldReturnEmptyLists()
        {
            var (local, remotes) = BranchParser.ParseBranches("");

            local.Should().BeEmpty();
            remotes.Should().BeEmpty();
        }

        // --- ParseStashes with \u001f format ---

        [Fact]
        public void ParseStashes_UnitSeparator_SingleStash_ShouldParse()
        {
            var output = "\u001fWIP on main: abc1234 my changes\u001f2024-01-15 10:30:00 +0000";
            var stashes = BranchParser.ParseStashes(output);

            stashes.Should().HaveCount(1);
            stashes[0].Index.Should().Be(0);
            stashes[0].Message.Should().Be("WIP on main: abc1234 my changes");
            stashes[0].Date.Should().Be("2024-01-15 10:30:00 +0000");
        }

        [Fact]
        public void ParseStashes_UnitSeparator_MultipleStashes_ShouldHaveSequentialIndexes()
        {
            var output = "\u001fFirst stash\u001f2024-01-15\n\u001fSecond stash\u001f2024-01-14";
            var stashes = BranchParser.ParseStashes(output);

            stashes.Should().HaveCount(2);
            stashes[0].Index.Should().Be(0);
            stashes[0].Message.Should().Be("First stash");
            stashes[1].Index.Should().Be(1);
            stashes[1].Message.Should().Be("Second stash");
        }

        [Fact]
        public void ParseStashes_UnitSeparator_MessageContainingPipe_ShouldParseCorrectly()
        {
            // Demonstrates the fix: old '|' split would have broken a stash message containing '|'
            var output = "\u001fWIP on main: fix foo|bar split\u001f2024-01-15 10:30:00 +0000";
            var stashes = BranchParser.ParseStashes(output);

            stashes.Should().HaveCount(1);
            stashes[0].Message.Should().Be("WIP on main: fix foo|bar split");
        }

        [Fact]
        public void ParseStashes_UnitSeparator_EmptyOutput_ShouldReturnEmptyList()
        {
            var stashes = BranchParser.ParseStashes("");

            stashes.Should().BeEmpty();
        }
    }
}
