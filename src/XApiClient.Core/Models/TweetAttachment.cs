using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class TweetAttachment
{
    [JsonPropertyName("media_keys")]
    public List<string> MediaKeys { get; set; } = new List<string>();
}
