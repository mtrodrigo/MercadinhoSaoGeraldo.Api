using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using MercadinhoSaoGeraldo.Api.Data;
using MercadinhoSaoGeraldo.Api.Security;
using MercadinhoSaoGeraldo.Api.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MercadinhoSaoGeraldo.Api.Infrastructure;

public static class ServiceRegistration
{
    public static void AddAppServices(IServiceCollection services, IConfiguration cfg)
    {
        var dataSource = Database.BuildDataSource(cfg);

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(
                dataSource,
                npgsql =>
                {
                    npgsql.CommandTimeout(120);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null);
                })
            .EnableDetailedErrors()
        );

        var supabaseUrl = AppConfig.Require(cfg, "SUPABASE_URL");
        var supabaseServiceKey = AppConfig.Require(cfg, "SUPABASE_SERVICE_ROLE_KEY");
        services.AddSingleton(new SupabaseStorageService(supabaseUrl, supabaseServiceKey, "product-images"));

        var issuer = AppConfig.Require(cfg, "JWT_ISSUER");
        var audience = AppConfig.Require(cfg, "JWT_AUDIENCE");
        var jwtKey = AppConfig.Require(cfg, "JWT_KEY");
        var jwtKeyByteCount = Encoding.UTF8.GetByteCount(jwtKey);
        if (jwtKeyByteCount < 16)
        {
            throw new InvalidOperationException("JWT_KEY must be at least 128 bits (16 bytes) when encoded as UTF-8. Consider using a 32-byte key similar to AES-256.");
        }
        services.AddSingleton(new JwtService(issuer, audience, jwtKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("Admin", p => p.RequireRole("Admin"));
            opt.AddPolicy("Cliente", p => p.RequireRole("Cliente", "Admin"));
        });

        var allowedOriginsRaw = cfg["ALLOWED_ORIGINS"] ?? Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        var allowedOrigins = allowedOriginsRaw?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins = new[] { "http://localhost:8081" };
        }

        services.AddCors(opt =>
        {
            opt.AddPolicy("Default", p => p
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });

        services.AddControllers().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var aesKeyB64 = AppConfig.Require(cfg, "AES_KEY_BASE64");
        var aesKey = Convert.FromBase64String(aesKeyB64);
        if (aesKey.Length != 32) throw new InvalidOperationException("AES_KEY_BASE64 must be 32 bytes (Base64).");
    }
}
