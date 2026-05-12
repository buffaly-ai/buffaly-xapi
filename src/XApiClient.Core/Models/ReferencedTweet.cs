using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class ReferencedTweet
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
