using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MercadinhoSaoGeraldo.Api.Infrastructure;

public static class AppPipeline
{
    public static void Use(WebApplication app, IConfiguration cfg)
    {
        app.UseSerilogRequestLogging();

        var useHttpsRedirect = cfg.GetValue<bool?>("USE_HTTPS_REDIRECT") ?? false;
        if (useHttpsRedirect) app.UseHttpsRedirection();

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            await next();
        });

        app.UseCors("Default");
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
    }
}
