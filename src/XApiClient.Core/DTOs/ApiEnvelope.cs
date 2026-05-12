using System.Text.Json.Serialization;
using XApiClient.Core.Models;

namespace XApiClient.Core.DTOs;

internal sealed class ApiEnvelope<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; set; }

    [JsonPropertyName("meta")]
    public XMeta? Meta { get; set; }

    [JsonPropertyName("errors")]
    public List<XApiError>? Errors { get; set; }
}
