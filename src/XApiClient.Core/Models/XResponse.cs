namespace XApiClient.Core.Models;

public sealed class XResponse<T>
{
    public XResponse(
        T? data,
        XIncludes? includes,
        XMeta? meta,
        IReadOnlyList<XApiError>? errors,
        string rawJson)
    {
        Data = data;
        Includes = includes;
        Meta = meta;
        Errors = errors;
        RawJson = rawJson;
    }

    public T? Data { get; }

    public XIncludes? Includes { get; }

    public XMeta? Meta { get; }

    public IReadOnlyList<XApiError>? Errors { get; }

    public string RawJson { get; }

    public string? NextToken
    {
        get
        {
            if (Meta == null)
            {
                return null;
            }

            return Meta.NextToken;
        }
    }
}
