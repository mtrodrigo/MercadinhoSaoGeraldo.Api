using Microsoft.Extensions.Configuration;

namespace MercadinhoSaoGeraldo.Api.Infrastructure;

public static class AppConfig
{
    public static string Require(IConfiguration cfg, string key)
    {
        var value = cfg[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(key);
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing config: {key}");
        }

        var normalized = value.Trim();
        if ((normalized.StartsWith("\"") && normalized.EndsWith("\"")) || (normalized.StartsWith("'") && normalized.EndsWith("'")))
        {
            normalized = normalized[1..^1];
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"Missing config: {key}");
        }

        return normalized;
    }
}
