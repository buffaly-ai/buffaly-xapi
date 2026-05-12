using System.Text.Json.Serialization;

namespace XApiClient.Core.DTOs;

internal sealed class PostTweetRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
