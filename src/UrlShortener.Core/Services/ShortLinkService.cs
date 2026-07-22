using UrlShortener.Core.Codes;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Repositories;

namespace UrlShortener.Core.Services;

public class ShortLinkService(IShortLinkRepository repository, TimeProvider timeProvider) : IShortLinkService
{
    private const int MaxCodeGenerationAttempts = 3;

    public async Task<ShortLink> CreateAsync(
        string originalUrl,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var shortLink = ShortLink.Create(code, originalUrl, timeProvider.GetUtcNow(), expiresAt);

        await repository.AddAsync(shortLink, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return shortLink;
    }

    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        repository.GetByCodeAsync(code, cancellationToken);

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxCodeGenerationAttempts; attempt++)
        {
            var code = Base62CodeGenerator.Generate();

            if (!await repository.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new ShortCodeGenerationException(MaxCodeGenerationAttempts);
    }
}
