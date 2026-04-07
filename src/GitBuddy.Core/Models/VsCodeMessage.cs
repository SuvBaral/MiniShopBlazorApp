using System.Text.Json.Serialization;

namespace GitSimple.Core.Models;

public class VsCodeMessage
{
    [JsonConstructor]
    public VsCodeMessage() { }

    public string RequestId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public object? Payload { get; set; }
}
