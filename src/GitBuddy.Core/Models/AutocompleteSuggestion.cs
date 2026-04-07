using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class AutocompleteSuggestion
{
    [JsonConstructor]
    public AutocompleteSuggestion() { }

    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}
