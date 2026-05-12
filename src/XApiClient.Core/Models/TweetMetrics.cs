using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class TweetMetrics
{
    [JsonPropertyName("retweet_count")]
    public int RetweetCount { get; set; }

    [JsonPropertyName("reply_count")]
    public int ReplyCount { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("quote_count")]
    public int QuoteCount { get; set; }

    [JsonPropertyName("bookmark_count")]
    public int BookmarkCount { get; set; }

    [JsonPropertyName("impression_count")]
    public int ImpressionCount { get; set; }
}
