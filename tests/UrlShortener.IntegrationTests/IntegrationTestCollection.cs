using Xunit;

namespace UrlShortener.IntegrationTests;

/// <summary>
/// One Postgres container and one Api host for the whole run, shared by
/// every test class in this collection (and run sequentially - xUnit never
/// parallelizes classes within the same collection - so they can't race
/// against the shared database or the shared per-IP rate limiter window).
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<ApiFactoryFixture>
{
    public const string Name = "Integration";
}
