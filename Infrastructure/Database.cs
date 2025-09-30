using Npgsql;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace MercadinhoSaoGeraldo.Api.Infrastructure;

public static class Database
{
    public static NpgsqlDataSource BuildDataSource(IConfiguration cfg)
    {
        var raw = cfg.GetConnectionString("DefaultConnection")
                  ?? cfg["SUPABASE_DB_CONNECTION"]
                  ?? Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
                  ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection or SUPABASE_DB_CONNECTION is required");
        var conn = NormalizePgConnection(raw);
        var ds = BuildConfiguredDataSource(conn, out var dsb);
        if (!TryOpenConnection(ds, out var firstError))
        {
            ds.Dispose();
            if (TryDisableSupabasePooler(dsb.ConnectionStringBuilder.ConnectionString, out var directConn))
            {
                var directBuilder = new NpgsqlConnectionStringBuilder(directConn);
                Log.Warning("Supabase pooler connection refused; retrying with direct connection to {Host}:{Port}",
                    directBuilder.Host,
                    directBuilder.Port);
                ds = BuildConfiguredDataSource(directConn, out dsb);
                if (!TryOpenConnection(ds, out var secondError))
                {
                    ds.Dispose();
                    throw new InvalidOperationException("Unable to establish database connection using Supabase pooler or direct connection.", secondError ?? firstError);
                }
            }
            else
            {
                throw new InvalidOperationException("Unable to establish database connection using the configured connection string.", firstError);
            }
        }

        var masked = Regex.Replace(dsb.ConnectionStringBuilder.ConnectionString, @"Password=[^;]+", "Password=***", RegexOptions.IgnoreCase);
        Log.Information("Using connection: {Conn}", masked);
        return ds;
    }

    static string NormalizePgConnection(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        var r = raw.Trim();
        if (r.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || r.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(r);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.IsDefaultPort ? 5432 : uri.Port;
            var db = uri.AbsolutePath.Trim('/');
            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Include Error Detail=true";
        }
        return r;
    }

    static NpgsqlDataSource BuildConfiguredDataSource(string conn, out NpgsqlDataSourceBuilder dsb)
    {
        dsb = new NpgsqlDataSourceBuilder(conn);
        if (dsb.ConnectionStringBuilder.SslMode == SslMode.Disable) dsb.ConnectionStringBuilder.SslMode = SslMode.Require;
        dsb.ConnectionStringBuilder.Multiplexing = false;
        dsb.ConnectionStringBuilder.MaxAutoPrepare = 0;
        dsb.ConnectionStringBuilder.NoResetOnClose = true;
        if (dsb.ConnectionStringBuilder.KeepAlive <= 0) dsb.ConnectionStringBuilder.KeepAlive = 30;
        if (dsb.ConnectionStringBuilder.Timeout <= 0) dsb.ConnectionStringBuilder.Timeout = 15;
        if (dsb.ConnectionStringBuilder.CommandTimeout <= 0) dsb.ConnectionStringBuilder.CommandTimeout = 120;
        if (dsb.ConnectionStringBuilder.MaxPoolSize <= 0) dsb.ConnectionStringBuilder.MaxPoolSize = 20;
        dsb.ConnectionStringBuilder.IPAddressPreference = IPAddressPreference.IPv4;
        return dsb.Build();
    }

    static bool TryOpenConnection(NpgsqlDataSource dataSource, out Exception? exception)
    {
        try
        {
            using var conn = dataSource.OpenConnection();
            exception = null;
            return true;
        }
        catch (NpgsqlException ex) when (ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
        {
            exception = ex;
            return false;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    static bool TryDisableSupabasePooler(string connectionString, out string directConnection)
    {
        directConnection = string.Empty;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host)) return false;
        if (!builder.Host.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase)) return false;

        if (!TryResolveDirectSupabaseEndpoint(builder.Host, out var directHost, out var directPort)) return false;

        if (string.Equals(builder.Host, directHost, StringComparison.OrdinalIgnoreCase)) return false;

        builder.Host = directHost;
        if (directPort.HasValue && directPort.Value > 0)
        {
            builder.Port = directPort.Value;
        }
        else if (builder.Port == 6543)
        {
            builder.Port = 5432;
        }

        directConnection = builder.ConnectionString;
        return true;
    }

    static bool TryResolveDirectSupabaseEndpoint(string poolingHost, out string host, out int? port)
    {
        host = string.Empty;
        port = null;

        var directOverride = Environment.GetEnvironmentVariable("SUPABASE_DIRECT_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(directOverride))
        {
            if (TryExtractHostAndPort(directOverride, out host, out port) && !IsPoolingHost(host))
            {
                Log.Information("Resolved Supabase direct host from {Source}: {Host}", "SUPABASE_DIRECT_DB_CONNECTION", host);
                return true;
            }
        }

        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
        if (!string.IsNullOrWhiteSpace(supabaseUrl) && Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri))
        {
            if (TryBuildDirectHostFromSupabaseHost(supabaseUri.Host, out var supabaseHost))
            {
                host = supabaseHost;
                Log.Information("Resolved Supabase direct host from {Source}: {Host}", "SUPABASE_URL", host);
                return true;
            }
            if (!string.IsNullOrWhiteSpace(supabaseUri.Host) && !IsPoolingHost(supabaseUri.Host))
            {
                host = supabaseUri.Host;
                Log.Information("Resolved Supabase host from {Source} without rewrite: {Host}", "SUPABASE_URL", host);
                return true;
            }
        }

        const string poolingSuffix = ".pooler.supabase.com";
        if (TryBuildDirectHostFromPoolingHost(poolingHost, poolingSuffix, out var poolingDirectHost))
        {
            host = poolingDirectHost;
            Log.Information("Resolved Supabase direct host from {Source}: {Host}", "Pooler", host);
            return true;
        }

        return false;
    }

    static bool TryBuildDirectHostFromSupabaseHost(string? host, out string directHost)
    {
        directHost = string.Empty;
        if (string.IsNullOrWhiteSpace(host)) return false;

        var trimmedHost = host.Trim().TrimEnd('.');
        if (trimmedHost.StartsWith("db.", StringComparison.OrdinalIgnoreCase))
        {
            directHost = trimmedHost;
            return true;
        }

        const string supabaseSuffix = ".supabase.co";
        if (!trimmedHost.EndsWith(supabaseSuffix, StringComparison.OrdinalIgnoreCase)) return false;

        var prefix = trimmedHost[..^supabaseSuffix.Length];
        var projectRef = ExtractProjectRef(prefix);
        if (string.IsNullOrWhiteSpace(projectRef)) return false;

        directHost = $"db.{projectRef}.supabase.co";
        return true;
    }

    static bool TryBuildDirectHostFromPoolingHost(string poolingHost, string poolingSuffix, out string directHost)
    {
        directHost = string.Empty;
        if (!poolingHost.EndsWith(poolingSuffix, StringComparison.OrdinalIgnoreCase)) return false;

        var prefix = poolingHost[..^poolingSuffix.Length];
        var projectRef = ExtractProjectRef(prefix);
        if (string.IsNullOrWhiteSpace(projectRef)) return false;

        directHost = $"db.{projectRef}.supabase.co";
        return true;
    }

    static string ExtractProjectRef(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("db.", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[3..];
        }

        var parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    static bool TryExtractHostAndPort(string value, out string host, out int? port)
    {
        host = string.Empty;
        port = null;
        if (string.IsNullOrWhiteSpace(value)) return false;

        if (value.Contains('='))
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(value);
                if (!string.IsNullOrWhiteSpace(builder.Host))
                {
                    host = builder.Host;
                    if (builder.Port > 0) port = builder.Port;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to parse SUPABASE_DIRECT_DB_CONNECTION as connection string: {Message}", ex.Message);
            }
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            host = uri.Host;
            if (!uri.IsDefaultPort) port = uri.Port;
            return true;
        }

        host = value.Trim();
        return !string.IsNullOrWhiteSpace(host);
    }

    static bool IsPoolingHost(string host)
        => host.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase);
}
