using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitBuddy.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class CommandHistoryEntry
{
    [JsonConstructor]
    public CommandHistoryEntry() { }

    public string NLInput { get; set; } = string.Empty;
    public string GitCommand { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}
