using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class SyncInfo
{
    [JsonConstructor]
    public SyncInfo() { }

    public int Ahead { get; set; }
    public int Behind { get; set; }
}
