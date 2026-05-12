using System.Text.Json.Serialization;

namespace XApiClient.Core.Models;

public sealed class Media
{
    [JsonPropertyName("media_key")]
    public string? MediaKey { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("preview_image_url")]
    public string? PreviewImageUrl { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }
}
