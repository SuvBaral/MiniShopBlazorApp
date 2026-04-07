using GitSimple.Core.Models;

namespace GitSimple.Core.Services;

public class CommandHistoryService
{
    private readonly List<CommandHistoryEntry> _history = new();
    private int _currentIndex = -1;

    public IReadOnlyList<CommandHistoryEntry> History => _history;

    public void Add(string nlInput, string gitCommand, bool success)
    {
        _history.Insert(0, new CommandHistoryEntry { NLInput = nlInput, GitCommand = gitCommand, Timestamp = DateTime.Now, Success = success });
        if (_history.Count > 100) _history.RemoveAt(_history.Count - 1);
        _currentIndex = -1;
    }

    public string? NavigateUp()
    {
        if (_history.Count == 0) return null;
        _currentIndex = Math.Min(_currentIndex + 1, _history.Count - 1);
        return _history[_currentIndex].NLInput;
    }

    public string? NavigateDown()
    {
        if (_currentIndex < 0) return null;
        if (_currentIndex == 0)
        {
            _currentIndex = -1;
            return string.Empty;
        }
        _currentIndex--;
        return _history[_currentIndex].NLInput;
    }

    public void ResetNavigation() => _currentIndex = -1;
}
