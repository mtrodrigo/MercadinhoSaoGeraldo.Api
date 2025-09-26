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
        if (!builder.Host.Contains(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase)) return false;

        var directHost = builder.Host.Replace(".pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (directHost.Equals(builder.Host, StringComparison.Ordinal)) return false;

        builder.Host = directHost;
        if (builder.Port == 6543) builder.Port = 5432;
        directConnection = builder.ConnectionString;
        return true;
    }
}
