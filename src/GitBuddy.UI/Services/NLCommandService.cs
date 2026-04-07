using System.Text.Json.Nodes;
using GitBuddy.Core.Models;

namespace GitBuddy.UI.Services;

public class NLCommandService
{
    private readonly VsCodeBridgeService _bridge;

    public NLCommandService(VsCodeBridgeService bridge) => _bridge = bridge;

    public Task<NLTranslation?> TranslateAsync(string naturalLanguage) =>
        _bridge.SendAsync<NLTranslation>("nl-translate", new JsonObject { ["text"] = naturalLanguage });

    public Task<GitCommandResult?> ExecuteTranslatedAsync(string gitCommand) =>
        _bridge.SendAsync<GitCommandResult>("nl-execute", new JsonObject { ["command"] = gitCommand });

    public Task<List<AutocompleteSuggestion>?> GetSuggestionsAsync(string partial) =>
        _bridge.SendAsync<List<AutocompleteSuggestion>>("nl-autocomplete", new JsonObject { ["text"] = partial });
}
