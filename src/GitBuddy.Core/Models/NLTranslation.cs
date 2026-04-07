using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class NLTranslation
{
    [JsonConstructor]
    public NLTranslation() { }

    public string Command { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? Warning { get; set; }
    public AlternativeCommand[]? Alternatives { get; set; }
    public string Tier { get; set; } = string.Empty;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class AlternativeCommand
{
    [JsonConstructor]
    public AlternativeCommand() { }

    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
