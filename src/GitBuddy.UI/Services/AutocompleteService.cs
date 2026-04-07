using GitBuddy.Core.Models;

namespace GitBuddy.UI.Services;

public class AutocompleteService
{
    private readonly NLCommandService _nlService;

    public AutocompleteService(NLCommandService nlService) => _nlService = nlService;

    public async Task<List<AutocompleteSuggestion>> GetSuggestionsAsync(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial) || partial.Length < 2)
            return new List<AutocompleteSuggestion>();

        var result = await _nlService.GetSuggestionsAsync(partial);
        return result ?? new List<AutocompleteSuggestion>();
    }
}
