using FluentAssertions;
using UrlShortener.Core.Entities;
using Xunit;

namespace UrlShortener.UnitTests.Entities;

public class ShortLinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_SetsExpectedDefaults()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now);

        link.Id.Should().NotBeEmpty();
        link.Code.Should().Be("aB3xK9c");
        link.OriginalUrl.Should().Be("https://example.com");
        link.CreatedAt.Should().Be(Now);
        link.ExpiresAt.Should().BeNull();
        link.IsOneTime.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
        link.HasPassword.Should().BeFalse();
        link.ClickCount.Should().Be(0);
        link.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_Throws(string? code)
    {
        var act = () => ShortLink.Create(code!, "https://example.com", Now);

        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUrl_Throws(string? url)
    {
        var act = () => ShortLink.Create("aB3xK9c", url!, Now);

        act.Should().Throw<ArgumentException>().WithParameterName("originalUrl");
    }

    [Fact]
    public void Create_WithExpirationInThePast_Throws()
    {
        var act = () => ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddMinutes(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("expiresAt");
    }

    [Fact]
    public void Create_WithExpirationEqualToNow_Throws()
    {
        var act = () => ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now);

        act.Should().Throw<ArgumentException>().WithParameterName("expiresAt");
    }

    [Fact]
    public void Create_WithPasswordHash_HasPasswordIsTrue()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, passwordHash: "hashed");

        link.HasPassword.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithNoExpiration_IsAlwaysFalse()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now);

        link.IsExpired(Now.AddYears(100)).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_BeforeExpiration_IsFalse()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddHours(1));

        link.IsExpired(Now.AddMinutes(30)).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_AtOrAfterExpiration_IsTrue()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddHours(1));

        link.IsExpired(Now.AddHours(1)).Should().BeTrue();
        link.IsExpired(Now.AddHours(2)).Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_WhenActiveAndNotExpired_IsTrue()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddHours(1));

        link.IsAccessible(Now.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void IsAccessible_WhenExpired_IsFalse()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddHours(1));

        link.IsAccessible(Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void RegisterClick_IncrementsClickCount()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now);

        link.RegisterClick(Now.AddMinutes(1));
        link.RegisterClick(Now.AddMinutes(2));

        link.ClickCount.Should().Be(2);
    }

    [Fact]
    public void RegisterClick_OnRegularLink_StaysActiveAfterMultipleClicks()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now);

        link.RegisterClick(Now.AddMinutes(1));

        link.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RegisterClick_OnOneTimeLink_DeactivatesAfterFirstClick()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, isOneTime: true);

        link.RegisterClick(Now.AddMinutes(1));

        link.IsActive.Should().BeFalse();
        link.ClickCount.Should().Be(1);
    }

    [Fact]
    public void RegisterClick_OnOneTimeLink_CannotBeClickedTwice()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, isOneTime: true);
        link.RegisterClick(Now.AddMinutes(1));

        var act = () => link.RegisterClick(Now.AddMinutes(2));

        act.Should().Throw<InvalidOperationException>();
        link.ClickCount.Should().Be(1);
    }

    [Fact]
    public void RegisterClick_OnExpiredLink_Throws()
    {
        var link = ShortLink.Create("aB3xK9c", "https://example.com", Now, expiresAt: Now.AddHours(1));

        var act = () => link.RegisterClick(Now.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
        link.ClickCount.Should().Be(0);
    }
}
