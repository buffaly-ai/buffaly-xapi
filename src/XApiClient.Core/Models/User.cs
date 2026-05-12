using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class User
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("public_metrics")]
    public UserPublicMetrics? PublicMetrics { get; set; }
}
