using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class XMeta
{
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }

    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }

    [JsonPropertyName("previous_token")]
    public string? PreviousToken { get; set; }

    [JsonPropertyName("newest_id")]
    public string? NewestId { get; set; }

    [JsonPropertyName("oldest_id")]
    public string? OldestId { get; set; }
}
