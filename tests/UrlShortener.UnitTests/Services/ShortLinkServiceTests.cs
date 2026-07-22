using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UrlShortener.Core.Exceptions;
using UrlShortener.Core.Services;
using UrlShortener.UnitTests.TestDoubles;
using Xunit;

namespace UrlShortener.UnitTests.Services;

public class ShortLinkServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static ShortLinkService CreateService(
        InMemoryShortLinkRepository repository,
        FakeTimeProvider? timeProvider = null,
        FakePasswordHasher? passwordHasher = null) =>
        new(repository, passwordHasher ?? new FakePasswordHasher(), timeProvider ?? new FakeTimeProvider(Now));

    [Fact]
    public async Task CreateAsync_PersistsLinkWithGeneratedCode()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);

        var shortLink = await service.CreateAsync("https://example.com", expiresAt: null, isOneTime: false, password: null);

        shortLink.Code.Should().HaveLength(7);
        repository.Links.Should().ContainSingle(l => l.Code == shortLink.Code);
    }

    [Fact]
    public async Task CreateAsync_WithExpiresAt_PassesItThrough()
    {
        var repository = new InMemoryShortLinkRepository();
        var timeProvider = new FakeTimeProvider(Now);
        var service = CreateService(repository, timeProvider);
        var expiresAt = Now.AddDays(1);

        var shortLink = await service.CreateAsync("https://example.com", expiresAt, isOneTime: false, password: null);

        shortLink.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task CreateAsync_WithPassword_StoresAHashNotThePlainPassword()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);

        var shortLink = await service.CreateAsync("https://example.com", null, false, "correct horse");

        shortLink.HasPassword.Should().BeTrue();
        shortLink.PasswordHash.Should().NotBe("correct horse");
    }

    [Fact]
    public async Task CreateAsync_RetriesCodeGenerationOnCollision()
    {
        var repository = new CollisionCountingRepository(collisionsBeforeSuccess: 2);
        var service = new ShortLinkService(repository, new FakePasswordHasher(), new FakeTimeProvider(Now));

        var shortLink = await service.CreateAsync("https://example.com", null, false, null);

        repository.CodeExistsCallCount.Should().Be(3);
        repository.Added.Should().Be(shortLink);
    }

    [Fact]
    public async Task CreateAsync_ThrowsAfterExhaustingRetries()
    {
        var repository = new CollisionCountingRepository(collisionsBeforeSuccess: int.MaxValue);
        var service = new ShortLinkService(repository, new FakePasswordHasher(), new FakeTimeProvider(Now));

        var act = () => service.CreateAsync("https://example.com", null, false, null);

        await act.Should().ThrowAsync<ShortCodeGenerationException>();
        repository.CodeExistsCallCount.Should().Be(3);
    }

    [Fact]
    public async Task VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);
        var shortLink = await service.CreateAsync("https://example.com", null, false, "s3cret");

        service.VerifyPassword(shortLink, "s3cret").Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);
        var shortLink = await service.CreateAsync("https://example.com", null, false, "s3cret");

        service.VerifyPassword(shortLink, "wrong").Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPassword_WhenLinkHasNoPassword_ReturnsFalse()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);
        var shortLink = await service.CreateAsync("https://example.com", null, false, null);

        service.VerifyPassword(shortLink, "anything").Should().BeFalse();
    }

    [Fact]
    public async Task RegisterClickAsync_IncrementsClickCount()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);
        var shortLink = await service.CreateAsync("https://example.com", null, false, null);

        await service.RegisterClickAsync(shortLink);

        shortLink.ClickCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterClickAsync_OnOneTimeLink_DeactivatesAfterFirstClick()
    {
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository);
        var shortLink = await service.CreateAsync("https://example.com", null, isOneTime: true, password: null);

        await service.RegisterClickAsync(shortLink);

        shortLink.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterClickAsync_OnExpiredLink_Throws()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var repository = new InMemoryShortLinkRepository();
        var service = CreateService(repository, timeProvider);
        var shortLink = await service.CreateAsync("https://example.com", Now.AddHours(1), false, null);
        timeProvider.SetUtcNow(Now.AddHours(2));

        var act = () => service.RegisterClickAsync(shortLink);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
