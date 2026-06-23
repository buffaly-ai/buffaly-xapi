using System.Text.Json.Serialization;

namespace XApiClient.Core.DTOs;

internal sealed class PostTweetRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("media")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PostTweetMediaRequest? Media { get; set; }

    [JsonPropertyName("reply")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PostTweetReplyRequest? Reply { get; set; }
}

internal sealed class PostTweetMediaRequest
{
    [JsonPropertyName("media_ids")]
    public IReadOnlyList<string> MediaIds { get; set; } = Array.Empty<string>();
}

internal sealed class PostTweetReplyRequest
{
    [JsonPropertyName("in_reply_to_tweet_id")]
    public string InReplyToTweetId { get; set; } = string.Empty;
}
