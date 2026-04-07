using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class StashEntry
{
    [JsonConstructor]
    public StashEntry() { }

    public int Index { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}
