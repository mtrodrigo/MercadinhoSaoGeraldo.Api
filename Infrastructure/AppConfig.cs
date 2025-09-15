using Microsoft.Extensions.Configuration;

namespace MercadinhoSaoGeraldo.Api.Infrastructure;

public static class AppConfig
{
    public static string Require(IConfiguration cfg, string key)
    {
        var v = cfg[key] ?? Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(v)) throw new InvalidOperationException($"Missing config: {key}");
        return v;
    }
}
