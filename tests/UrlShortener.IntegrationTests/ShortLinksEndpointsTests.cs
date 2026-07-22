using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Contracts;
using Xunit;

namespace UrlShortener.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ShortLinksEndpointsTests(ApiFactoryFixture factory)
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    public async Task CreateLink_WithValidUrl_ReturnsCreatedWithShortUrl()
    {
        var response = await _client.PostAsJsonAsync("/api/links", new CreateShortLinkRequest("https://example.com/a/b/c"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateShortLinkResponse>();
        body.Should().NotBeNull();
        body!.Code.Should().HaveLength(7);
        body.OriginalUrl.Should().Be("https://example.com/a/b/c");
        body.ShortUrl.Should().EndWith(body.Code);
    }

    [Fact]
    public async Task CreateLink_WithSsrfTarget_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/links", new CreateShortLinkRequest("http://169.254.169.254/"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Redirect_ForExistingCode_ReturnsFoundWithOriginalUrl()
    {
        var created = await CreateLinkAsync("https://example.com/redirect-target");

        var response = await _client.GetAsync($"/{created.Code}");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be("https://example.com/redirect-target");
    }

    [Fact]
    public async Task Redirect_ForUnknownCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/zzzzzzz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Redirect_ForExpiredLink_ReturnsGone()
    {
        var created = await CreateLinkAsync(
            "https://example.com/expires-soon",
            new CreateShortLinkRequest("https://example.com/expires-soon", DateTimeOffset.UtcNow.AddSeconds(1)));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var response = await _client.GetAsync($"/{created.Code}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task OneTimeLink_SecondClick_ReturnsGone()
    {
        var created = await CreateLinkAsync(
            "https://example.com/one-time",
            new CreateShortLinkRequest("https://example.com/one-time", IsOneTime: true));

        var first = await _client.GetAsync($"/{created.Code}");
        var second = await _client.GetAsync($"/{created.Code}");

        first.StatusCode.Should().Be(HttpStatusCode.Found);
        second.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task OneTimeLink_AfterUse_StatsShowInactiveWithOneClick()
    {
        var created = await CreateLinkAsync(
            "https://example.com/one-time-stats",
            new CreateShortLinkRequest("https://example.com/one-time-stats", IsOneTime: true));
        await _client.GetAsync($"/{created.Code}");

        // One-time deactivation is registered synchronously (see
        // RedirectController), so no polling needed here.
        var stats = await _client.GetFromJsonAsync<ShortLinkStatsResponse>($"/api/links/{created.Code}/stats");

        stats.Should().NotBeNull();
        stats!.ClickCount.Should().Be(1);
        stats.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task PasswordProtectedLink_WithoutPassword_ReturnsUnauthorized()
    {
        var created = await CreateLinkAsync(
            "https://example.com/secret",
            new CreateShortLinkRequest("https://example.com/secret", Password: "hunter2"));

        var response = await _client.GetAsync($"/{created.Code}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PasswordProtectedLink_WithCorrectPassword_ReturnsFound()
    {
        var created = await CreateLinkAsync(
            "https://example.com/secret-2",
            new CreateShortLinkRequest("https://example.com/secret-2", Password: "hunter2"));

        var response = await _client.GetAsync($"/{created.Code}?password=hunter2");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
    }

    [Fact]
    public async Task Delete_RemovesLink_SubsequentRedirectReturnsNotFound()
    {
        var created = await CreateLinkAsync("https://example.com/to-delete");

        var deleteResponse = await _client.DeleteAsync($"/api/links/{created.Code}");
        var redirectResponse = await _client.GetAsync($"/{created.Code}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_UnknownCode_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/links/zzzzzzz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stats_ForRegularLink_EventuallyReflectsAsyncClickCount()
    {
        var created = await CreateLinkAsync("https://example.com/click-counted");
        await _client.GetAsync($"/{created.Code}");

        // The click count for a non-one-time link is incremented off the
        // request path (see ClickTrackingBackgroundService), so give it a
        // short, bounded window to catch up instead of asserting instantly.
        ShortLinkStatsResponse? stats = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            stats = await _client.GetFromJsonAsync<ShortLinkStatsResponse>($"/api/links/{created.Code}/stats");
            if (stats is { ClickCount: > 0 })
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        stats.Should().NotBeNull();
        stats!.ClickCount.Should().Be(1);
    }

    private async Task<CreateShortLinkResponse> CreateLinkAsync(string url, CreateShortLinkRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/api/links", request ?? new CreateShortLinkRequest(url));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateShortLinkResponse>();
        return body!;
    }
}
