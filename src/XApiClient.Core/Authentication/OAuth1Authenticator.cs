using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace XApiClient.Core.Authentication;

public sealed class OAuth1Authenticator
{
    private readonly Func<string> _nonceFactory;
    private readonly Func<DateTimeOffset> _timestampFactory;

    public OAuth1Authenticator(
        Func<string>? nonceFactory = null,
        Func<DateTimeOffset>? timestampFactory = null)
    {
        _nonceFactory = nonceFactory ?? CreateNonce;
        _timestampFactory = timestampFactory ?? CreateTimestamp;
    }

    public string CreateAuthorizationHeader(
        HttpMethod httpMethod,
        Uri requestUri,
        IReadOnlyDictionary<string, string?>? queryParameters,
        IReadOnlyDictionary<string, string?>? bodyParameters,
        XCredentials credentials)
    {
        if (!credentials.HasOAuth1UserContext())
        {
            throw new InvalidOperationException("OAuth 1.0a requires consumer key/secret and access token/secret.");
        }

        string nonce = _nonceFactory.Invoke();
        string timestamp = _timestampFactory.Invoke().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        SortedDictionary<string, string> oauthParameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["oauth_consumer_key"] = credentials.ConsumerKey!,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = timestamp,
            ["oauth_token"] = credentials.AccessToken!,
            ["oauth_version"] = "1.0",
        };

        List<KeyValuePair<string, string>> signatureParameters = new List<KeyValuePair<string, string>>();
        AddParameterRangeNonNullable(signatureParameters, oauthParameters);
        AddParameterRange(signatureParameters, queryParameters);
        AddParameterRange(signatureParameters, bodyParameters);

        string normalizedUrl = NormalizeBaseUrl(requestUri);
        string normalizedParameters = NormalizeParameters(signatureParameters);
        string signatureBaseString = string.Concat(
            httpMethod.Method.ToUpperInvariant(),
            "&",
            PercentEncode(normalizedUrl),
            "&",
            PercentEncode(normalizedParameters));

        string signingKey = string.Concat(
            PercentEncode(credentials.ConsumerSecret!),
            "&",
            PercentEncode(credentials.AccessTokenSecret!));

        string signature = ComputeHmacSha1(signatureBaseString, signingKey);
        oauthParameters["oauth_signature"] = signature;

        StringBuilder header = new StringBuilder();
        header.Append("OAuth ");

        int index = 0;
        foreach (KeyValuePair<string, string> kvp in oauthParameters)
        {
            if (index > 0)
            {
                header.Append(", ");
            }

            header.Append(kvp.Key);
            header.Append("=\"");
            header.Append(PercentEncode(kvp.Value));
            header.Append('"');
            index++;
        }

        return header.ToString();
    }

    private static void AddParameterRangeNonNullable(
        ICollection<KeyValuePair<string, string>> target,
        IReadOnlyDictionary<string, string> values)
    {
        foreach (KeyValuePair<string, string> kvp in values)
        {
            target.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
        }
    }

    private static void AddParameterRange(
        ICollection<KeyValuePair<string, string>> target,
        IReadOnlyDictionary<string, string?>? values)
    {
        if (values == null)
        {
            return;
        }

        foreach (KeyValuePair<string, string?> kvp in values)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            if (kvp.Value == null)
            {
                continue;
            }

            target.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
        }
    }

    private static string NormalizeBaseUrl(Uri uri)
    {
        string scheme = uri.Scheme.ToLowerInvariant();
        string host = uri.Host.ToLowerInvariant();

        bool isDefaultPort =
            (scheme == Uri.UriSchemeHttp && uri.Port == 80) ||
            (scheme == Uri.UriSchemeHttps && uri.Port == 443);

        if (isDefaultPort)
        {
            return string.Concat(scheme, "://", host, uri.AbsolutePath);
        }

        return string.Concat(scheme, "://", host, ":", uri.Port.ToString(CultureInfo.InvariantCulture), uri.AbsolutePath);
    }

    private static string NormalizeParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        List<KeyValuePair<string, string>> ordered = parameters
            .OrderBy(pair => PercentEncode(pair.Key), StringComparer.Ordinal)
            .ThenBy(pair => PercentEncode(pair.Value), StringComparer.Ordinal)
            .ToList();

        StringBuilder normalized = new StringBuilder();
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
            {
                normalized.Append('&');
            }

            normalized.Append(PercentEncode(ordered[i].Key));
            normalized.Append('=');
            normalized.Append(PercentEncode(ordered[i].Value));
        }

        return normalized.ToString();
    }

    private static string ComputeHmacSha1(string baseString, string key)
    {
        byte[] keyBytes = Encoding.ASCII.GetBytes(key);
        byte[] baseBytes = Encoding.ASCII.GetBytes(baseString);
        using HMACSHA1 hmac = new HMACSHA1(keyBytes);
        byte[] hash = hmac.ComputeHash(baseBytes);
        return Convert.ToBase64String(hash);
    }

    private static string PercentEncode(string value)
    {
        const string unreserved = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        StringBuilder encoded = new StringBuilder();
        foreach (byte b in bytes)
        {
            char c = (char)b;
            if (unreserved.IndexOf(c) >= 0)
            {
                encoded.Append(c);
            }
            else
            {
                encoded.Append('%');
                encoded.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return encoded.ToString();
    }

    private static string CreateNonce()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTimeOffset CreateTimestamp()
    {
        return DateTimeOffset.UtcNow;
    }
}
