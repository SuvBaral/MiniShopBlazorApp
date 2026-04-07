using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class Branch
{
    [JsonConstructor]
    public Branch() { }

    public string Name { get; set; } = string.Empty;
    public bool IsLocal { get; set; }
    public bool IsCurrent { get; set; }
    public string? TrackingBranch { get; set; }
    public string? RemoteName { get; set; }
}
