using System.Collections;

namespace XApiClient.CLI.Configuration;

public static class EnvironmentReader
{
    public static IReadOnlyDictionary<string, string?> ReadAll()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        IDictionary environmentVariables = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry entry in environmentVariables)
        {
            string key = entry.Key.ToString() ?? string.Empty;
            string? value = entry.Value?.ToString();
            values[key] = value;
        }

        return values;
    }
}
