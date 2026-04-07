using FluentAssertions;
using GitBuddy.Core.Models;
using GitBuddy.Core.Services;
using Xunit;

namespace GitBuddy.UI.Tests.Services;

public class StateServiceTests
{
    private readonly StateService _sut = new();

    [Fact]
    public void InitialState_ShouldHaveDefaults()
    {
        _sut.RepoName.Should().BeEmpty();
        _sut.CurrentBranch.Should().BeEmpty();
        _sut.LocalBranches.Should().BeEmpty();
        _sut.RemoteBranches.Should().BeEmpty();
        _sut.Stashes.Should().BeEmpty();
        _sut.Sync.Should().BeNull();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void NotifyStateChanged_ShouldFireEvent()
    {
        var fired = false;
        _sut.OnStateChanged += () => fired = true;

        _sut.NotifyStateChanged();

        fired.Should().BeTrue();
    }

    [Fact]
    public void NotifyStateChanged_NoSubscribers_ShouldNotThrow()
    {
        var act = () => _sut.NotifyStateChanged();

        act.Should().NotThrow();
    }

    [Fact]
    public void SetProperties_ShouldPersist()
    {
        _sut.RepoName = "MyRepo";
        _sut.CurrentBranch = "develop";
        _sut.IsLoading = true;
        _sut.Sync = new SyncInfo { Ahead = 2, Behind = 3 };

        _sut.RepoName.Should().Be("MyRepo");
        _sut.CurrentBranch.Should().Be("develop");
        _sut.IsLoading.Should().BeTrue();
        _sut.Sync!.Ahead.Should().Be(2);
        _sut.Sync.Behind.Should().Be(3);
    }

    [Fact]
    public void LocalBranches_CanBeModified()
    {
        _sut.LocalBranches.Add(new Branch { Name = "main", IsLocal = true, IsCurrent = true, TrackingBranch = "origin/main" });
        _sut.LocalBranches.Add(new Branch { Name = "develop", IsLocal = true });

        _sut.LocalBranches.Should().HaveCount(2);
    }

    [Fact]
    public void RemoteBranches_CanBeGrouped()
    {
        _sut.RemoteBranches["origin"] = new List<Branch>
        {
            new() { Name = "origin/main", IsLocal = false, RemoteName = "origin" },
            new() { Name = "origin/develop", IsLocal = false, RemoteName = "origin" }
        };
        _sut.RemoteBranches["upstream"] = new List<Branch>
        {
            new() { Name = "upstream/main", IsLocal = false, RemoteName = "upstream" }
        };

        _sut.RemoteBranches.Should().HaveCount(2);
        _sut.RemoteBranches["origin"].Should().HaveCount(2);
        _sut.RemoteBranches["upstream"].Should().HaveCount(1);
    }

    [Fact]
    public void MultipleSubscribers_AllShouldBeFired()
    {
        var count = 0;
        _sut.OnStateChanged += () => count++;
        _sut.OnStateChanged += () => count++;

        _sut.NotifyStateChanged();

        count.Should().Be(2);
    }
}
