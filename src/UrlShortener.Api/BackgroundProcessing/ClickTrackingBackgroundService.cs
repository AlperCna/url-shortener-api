using UrlShortener.Core.Services;

namespace UrlShortener.Api.BackgroundProcessing;

/// <summary>
/// Drains <see cref="ClickTrackingQueue"/> and persists click counts outside
/// the request pipeline. Each item gets its own DI scope (and DbContext) -
/// the request's scope is long gone by the time this runs.
/// </summary>
public class ClickTrackingBackgroundService(
    ClickTrackingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ClickTrackingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var code in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var shortLinkService = scope.ServiceProvider.GetRequiredService<IShortLinkService>();

                var shortLink = await shortLinkService.GetByCodeAsync(code, stoppingToken);
                if (shortLink is not null)
                {
                    await shortLinkService.RegisterClickAsync(shortLink, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A missed click count is not worth crashing the app over.
                logger.LogError(ex, "Failed to register a click for code {Code}", code);
            }
        }
    }
}
