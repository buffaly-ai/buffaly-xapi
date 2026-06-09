using System.Text.Json.Serialization;

namespace XApiClient.Core.DTOs;

internal sealed class PostTweetRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("media")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PostTweetMediaRequest? Media { get; set; }
}

internal sealed class PostTweetMediaRequest
{
    [JsonPropertyName("media_ids")]
    public IReadOnlyList<string> MediaIds { get; set; } = Array.Empty<string>();
}
