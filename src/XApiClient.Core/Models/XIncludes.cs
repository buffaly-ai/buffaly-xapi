using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class XIncludes
{
    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new List<User>();

    [JsonPropertyName("tweets")]
    public List<Tweet> Tweets { get; set; } = new List<Tweet>();

    [JsonPropertyName("media")]
    public List<Media> Media { get; set; } = new List<Media>();
}
