using Microsoft.JSInterop;

namespace GitBuddy.UI.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public ThemeService(IJSRuntime js) => _js = js;

    public async Task<string> GetCssVariableAsync(string variableName)
    {
        return await _js.InvokeAsync<string>(
            "eval",
            $"getComputedStyle(document.documentElement).getPropertyValue('{variableName}').trim()"
        );
    }
}
