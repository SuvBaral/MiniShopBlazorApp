using GitBuddy.Core.Models;
using GitBuddy.Core.Services;
using GitBuddy.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GitBuddy.UI.Components.NaturalLanguage;

public partial class NLCommandBar : ComponentBase, IDisposable
{
    [Inject] private NLCommandService NLService { get; set; } = default!;
    [Inject] private AutocompleteService Autocomplete { get; set; } = default!;
    [Inject] private CommandHistoryService History { get; set; } = default!;
    [Inject] private VsCodeBridgeService Bridge { get; set; } = default!;

    [Parameter] public EventCallback<string> OnToast { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }
    [Parameter] public EventCallback<(string Title, string Message, string? Warning, bool IsDangerous, Func<Task> OnConfirm)> OnConfirmRequest { get; set; }

    private ElementReference _inputRef;
    private string _inputText = string.Empty;
    private NLTranslation? _translation;
    private GitCommandResult? _result;
    private bool _isTranslating;
    private List<AutocompleteSuggestion> _suggestions = new();
    private bool _showAutocomplete;
    private bool _showHistory;
    private int _autocompleteIndex = -1;
    private System.Timers.Timer? _debounceTimer;
    private Action? _focusHandler;

    protected override void OnInitialized()
    {
        _focusHandler = () => _ = InvokeAsync(StateHasChanged);
        Bridge.OnFocusNLBar += _focusHandler;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        _showHistory = false;

        switch (e.Key)
        {
            case "Enter":
                if (_showAutocomplete && _autocompleteIndex >= 0 && _autocompleteIndex < _suggestions.Count)
                {
                    await SelectSuggestion(_suggestions[_autocompleteIndex]);
                    return;
                }
                await TranslateInput();
                break;

            case "Escape":
                Clear();
                break;

            case "ArrowUp":
                if (_showAutocomplete && _suggestions.Count > 0)
                {
                    _autocompleteIndex = Math.Max(0, _autocompleteIndex - 1);
                }
                else
                {
                    var prev = History.NavigateUp();
                    if (prev is not null) _inputText = prev;
                }
                break;

            case "ArrowDown":
                if (_showAutocomplete && _suggestions.Count > 0)
                {
                    _autocompleteIndex = Math.Min(_suggestions.Count - 1, _autocompleteIndex + 1);
                }
                else
                {
                    var next = History.NavigateDown();
                    if (next is not null) _inputText = next;
                }
                break;

            default:
                _autocompleteIndex = -1;
                DebounceAutocomplete();
                break;
        }
    }

    private void HandleBlur()
    {
        // Delay to allow click on autocomplete/history items
        _ = Task.Delay(200).ContinueWith(_ => InvokeAsync(() =>
        {
            _showAutocomplete = false;
            _showHistory = false;
            StateHasChanged();
        }));
    }

    private void DebounceAutocomplete()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(300);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Dispose();
            await InvokeAsync(async () =>
            {
                if (_inputText.Length >= 2)
                {
                    _suggestions = await Autocomplete.GetSuggestionsAsync(_inputText);
                    _showAutocomplete = _suggestions.Count > 0;
                }
                else
                {
                    _showAutocomplete = false;
                }
                StateHasChanged();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private async Task SelectSuggestion(AutocompleteSuggestion s)
    {
        _inputText = s.Text;
        _showAutocomplete = false;
        _autocompleteIndex = -1;
        await TranslateInput();
    }

    private async Task SelectHistory(string text)
    {
        _inputText = text;
        _showHistory = false;
        await TranslateInput();
    }

    private async Task TranslateInput()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) return;

        _showAutocomplete = false;
        _showHistory = false;
        _isTranslating = true;
        _translation = null;
        _result = null;
        StateHasChanged();

        try
        {
            _translation = await NLService.TranslateAsync(_inputText);

            // Auto-execute safe commands
            if (_translation is not null && !string.IsNullOrEmpty(_translation.Command) && !_translation.RequiresConfirmation)
            {
                await ExecuteCommand();
            }
        }
        catch (Exception ex)
        {
            _translation = new NLTranslation { Explanation = $"Error: {ex.Message}", Tier = "error" };
        }

        _isTranslating = false;
        StateHasChanged();
    }

    private async Task ExecuteCommand()
    {
        if (_translation is null || string.IsNullOrEmpty(_translation.Command)) return;

        if (_translation.RequiresConfirmation)
        {
            var translation = _translation;
            var input = _inputText;
            await OnConfirmRequest.InvokeAsync((
                "Execute Git Command",
                $"Execute: {translation.Command}",
                translation.Warning,
                translation.Risk == RiskLevel.Dangerous,
                async () =>
                {
                    await DoExecute(translation, input);
                }
            ));
            return;
        }

        await DoExecute(_translation, _inputText);
    }

    private async Task DoExecute(NLTranslation translation, string input)
    {
        _isTranslating = true;
        StateHasChanged();

        _result = await NLService.ExecuteTranslatedAsync(translation.Command);
        History.Add(input, translation.Command, _result?.Success ?? false);

        _isTranslating = false;

        if (_result?.Success == true)
            await OnToast.InvokeAsync("Command executed successfully");
        else
            await OnError.InvokeAsync(_result?.Error ?? "Command failed");

        await OnRefresh.InvokeAsync();
        StateHasChanged();
    }

    private void Clear()
    {
        _inputText = string.Empty;
        _translation = null;
        _result = null;
        _showAutocomplete = false;
        _showHistory = false;
        _autocompleteIndex = -1;
        History.ResetNavigation();
    }

    public void Dispose()
    {
        if (_focusHandler is not null) Bridge.OnFocusNLBar -= _focusHandler;
        _debounceTimer?.Dispose();
    }
}
