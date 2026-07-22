using UrlShortener.Core.Entities;
using UrlShortener.Core.Repositories;

namespace UrlShortener.UnitTests.TestDoubles;

/// <summary>
/// Reports the first <paramref name="collisionsBeforeSuccess"/> generated
/// codes as taken, regardless of their value, to exercise
/// ShortLinkService's retry loop without depending on actual Base62 output.
/// </summary>
public class CollisionCountingRepository(int collisionsBeforeSuccess) : IShortLinkRepository
{
    public int CodeExistsCallCount { get; private set; }

    public ShortLink? Added { get; private set; }

    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult<ShortLink?>(null);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        CodeExistsCallCount++;
        return Task.FromResult(CodeExistsCallCount <= collisionsBeforeSuccess);
    }

    public Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        Added = shortLink;
        return Task.CompletedTask;
    }

    public void Remove(ShortLink shortLink)
    {
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
