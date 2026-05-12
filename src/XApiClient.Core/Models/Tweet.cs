using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class Tweet
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("author_id")]
    public string? AuthorId { get; set; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("public_metrics")]
    public TweetMetrics? PublicMetrics { get; set; }

    [JsonPropertyName("referenced_tweets")]
    public List<ReferencedTweet> ReferencedTweets { get; set; } = new List<ReferencedTweet>();

    [JsonPropertyName("attachments")]
    public TweetAttachment? Attachments { get; set; }
}
