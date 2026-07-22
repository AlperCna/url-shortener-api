using UrlShortener.Core.Entities;
using UrlShortener.Core.Repositories;

namespace UrlShortener.UnitTests.TestDoubles;

public class InMemoryShortLinkRepository : IShortLinkRepository
{
    private readonly List<ShortLink> _links = [];

    public IReadOnlyList<ShortLink> Links => _links;

    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_links.SingleOrDefault(l => l.Code == code));

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_links.Any(l => l.Code == code));

    public Task AddAsync(ShortLink shortLink, CancellationToken cancellationToken = default)
    {
        _links.Add(shortLink);
        return Task.CompletedTask;
    }

    public void Update(ShortLink shortLink)
    {
        if (!_links.Contains(shortLink))
        {
            _links.Add(shortLink);
        }
    }

    public void Remove(ShortLink shortLink) => _links.Remove(shortLink);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
