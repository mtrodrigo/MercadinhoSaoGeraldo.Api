using DotNetEnv;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Serilog;
using MercadinhoSaoGeraldo.Api.Infrastructure;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

ServiceRegistration.AddAppServices(builder.Services, builder.Configuration);

var app = builder.Build();

AppPipeline.Use(app, builder.Configuration);

app.MapGet("/ping", () =>
        Results.Json(new { status = "ok", service = "Mercadinho API" }))
    .WithName("Ping")
    .WithTags("Health")
    .WithOpenApi();

app.Run();
