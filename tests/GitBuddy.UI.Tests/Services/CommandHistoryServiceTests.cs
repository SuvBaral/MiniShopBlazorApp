using FluentAssertions;
using GitBuddy.Core.Services;
using Xunit;

namespace GitBuddy.UI.Tests.Services;

public class CommandHistoryServiceTests
{
    private readonly CommandHistoryService _sut = new();

    [Fact]
    public void History_Initially_ShouldBeEmpty()
    {
        _sut.History.Should().BeEmpty();
    }

    [Fact]
    public void Add_SingleEntry_ShouldAppearInHistory()
    {
        _sut.Add("pull latest", "git pull", true);

        _sut.History.Should().HaveCount(1);
        _sut.History[0].NLInput.Should().Be("pull latest");
        _sut.History[0].GitCommand.Should().Be("git pull");
        _sut.History[0].Success.Should().BeTrue();
    }

    [Fact]
    public void Add_MultipleEntries_ShouldBeInReverseOrder()
    {
        _sut.Add("first", "git cmd1", true);
        _sut.Add("second", "git cmd2", true);
        _sut.Add("third", "git cmd3", false);

        _sut.History.Should().HaveCount(3);
        _sut.History[0].NLInput.Should().Be("third");
        _sut.History[1].NLInput.Should().Be("second");
        _sut.History[2].NLInput.Should().Be("first");
    }

    [Fact]
    public void Add_OverMaxLimit_ShouldTrimOldest()
    {
        for (int i = 0; i < 105; i++)
            _sut.Add($"cmd-{i}", $"git {i}", true);

        _sut.History.Should().HaveCount(100);
        _sut.History[0].NLInput.Should().Be("cmd-104");
    }

    [Fact]
    public void Add_FailedCommand_ShouldRecordFailure()
    {
        _sut.Add("bad command", "git xxx", false);

        _sut.History[0].Success.Should().BeFalse();
    }

    [Fact]
    public void NavigateUp_EmptyHistory_ShouldReturnNull()
    {
        _sut.NavigateUp().Should().BeNull();
    }

    [Fact]
    public void NavigateUp_WithHistory_ShouldReturnMostRecent()
    {
        _sut.Add("first", "git 1", true);
        _sut.Add("second", "git 2", true);

        _sut.NavigateUp().Should().Be("second");
    }

    [Fact]
    public void NavigateUp_Twice_ShouldReturnOlderEntry()
    {
        _sut.Add("first", "git 1", true);
        _sut.Add("second", "git 2", true);

        _sut.NavigateUp(); // second
        _sut.NavigateUp().Should().Be("first");
    }

    [Fact]
    public void NavigateUp_BeyondHistory_ShouldStayAtOldest()
    {
        _sut.Add("only", "git 1", true);

        _sut.NavigateUp(); // only
        _sut.NavigateUp().Should().Be("only"); // stays at oldest
    }

    [Fact]
    public void NavigateDown_WithoutUp_ShouldReturnNull()
    {
        _sut.Add("cmd", "git 1", true);

        _sut.NavigateDown().Should().BeNull();
    }

    [Fact]
    public void NavigateDown_AfterUp_ShouldGoForward()
    {
        _sut.Add("first", "git 1", true);
        _sut.Add("second", "git 2", true);

        _sut.NavigateUp(); // second
        _sut.NavigateUp(); // first
        _sut.NavigateDown().Should().Be("second");
    }

    [Fact]
    public void NavigateDown_ToBottom_ShouldReturnEmpty()
    {
        _sut.Add("cmd", "git 1", true);

        _sut.NavigateUp(); // cmd
        _sut.NavigateDown().Should().Be(string.Empty);
    }

    [Fact]
    public void ResetNavigation_ShouldResetIndex()
    {
        _sut.Add("first", "git 1", true);
        _sut.Add("second", "git 2", true);

        _sut.NavigateUp(); // second
        _sut.NavigateUp(); // first
        _sut.ResetNavigation();
        _sut.NavigateUp().Should().Be("second"); // starts from top again
    }

    [Fact]
    public void Add_AfterNavigation_ShouldResetIndex()
    {
        _sut.Add("first", "git 1", true);
        _sut.NavigateUp(); // first

        _sut.Add("second", "git 2", true);
        _sut.NavigateUp().Should().Be("second"); // reset, starts from new top
    }
}
