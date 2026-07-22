using UrlShortener.Core.Codes;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Repositories;
using UrlShortener.Core.Security;

namespace UrlShortener.Core.Services;

public class ShortLinkService(
    IShortLinkRepository repository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IShortLinkService
{
    private const int MaxCodeGenerationAttempts = 3;

    public async Task<ShortLink> CreateAsync(
        string originalUrl,
        DateTimeOffset? expiresAt,
        bool isOneTime,
        string? password,
        CancellationToken cancellationToken = default)
    {
        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var passwordHash = password is null ? null : passwordHasher.Hash(password);

        var shortLink = ShortLink.Create(
            code,
            originalUrl,
            timeProvider.GetUtcNow(),
            expiresAt,
            isOneTime,
            passwordHash);

        await repository.AddAsync(shortLink, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return shortLink;
    }

    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        repository.GetByCodeAsync(code, cancellationToken);

    public bool VerifyPassword(ShortLink shortLink, string password) =>
        shortLink.PasswordHash is not null && passwordHasher.Verify(shortLink.PasswordHash, password);

    public async Task RegisterClickAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        shortLink.RegisterClick(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }

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
