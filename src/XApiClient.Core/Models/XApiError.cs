using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class XApiError
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
