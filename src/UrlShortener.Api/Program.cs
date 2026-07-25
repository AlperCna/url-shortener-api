using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api;
using UrlShortener.Api.BackgroundProcessing;
using UrlShortener.Api.ErrorHandling;
using UrlShortener.Core.Services;
using UrlShortener.Infrastructure;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "URL Shortener API";
        document.Info.Description =
            "Shortens URLs, with optional expiration, one-time use, and password protection.";
        return Task.CompletedTask;
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IShortLinkService, ShortLinkService>();

builder.Services.AddSingleton<ClickTrackingQueue>();
builder.Services.AddSingleton<IClickTrackingQueue>(sp => sp.GetRequiredService<ClickTrackingQueue>());
builder.Services.AddHostedService<ClickTrackingBackgroundService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.LinkCreation, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many links created from this address. Try again in a minute.",
        }, cancellationToken);
    };
});

var app = builder.Build();

// Apply pending migrations on startup so `docker compose up` produces a
// working app with no manual database step. A separately-run migration
// step would be the production-grade approach; this is a deliberate
// simplification for a demo-sized project.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.

// Must run first: behind a reverse proxy (Render, or any real deployment),
// Kestrel only ever sees plain HTTP from the proxy, and the client's real IP
// shows up as the proxy's IP. Without this, Request.Scheme is always "http"
// (breaking the shortUrl the create endpoint builds) and the per-IP rate
// limiter partitions everyone together under the proxy's address. Trusting
// forwarded headers from any proxy is fine here specifically because the
// container is never reachable except through Render's edge network.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Exposed unconditionally, not just in Development: this is a demo/portfolio
// project meant to be inspected by whoever runs `docker compose up`, so the
// docs should be reachable without setting ASPNETCORE_ENVIRONMENT.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "URL Shortener API v1");
    options.RoutePrefix = "swagger";
});

app.UseExceptionHandler();

// No UseHttpsRedirection(): the container only ever listens on plain HTTP
// (see docker-compose.yml). TLS termination is a reverse proxy's job in
// front of this, not something the app itself should assume or enforce.

// Serves wwwroot/index.html at the root URL as a landing/demo page. Must
// run before MapControllers so "/" resolves to the static file instead of
// falling through; doesn't collide with the redirect endpoint's "/{code}"
// route since that requires a 7-character Base62 match.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> in the
// integration test project.
public partial class Program;
