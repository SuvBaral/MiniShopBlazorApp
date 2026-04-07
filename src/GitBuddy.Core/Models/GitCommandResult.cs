using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class GitCommandResult
{
    [JsonConstructor]
    public GitCommandResult() { }

    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
}
