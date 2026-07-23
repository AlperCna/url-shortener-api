using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UrlShortener.Api.Contracts;
using UrlShortener.Core.Services;
using UrlShortener.Core.Validation;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/links")]
public class LinksController(IShortLinkService shortLinkService, TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.LinkCreation)]
    [EndpointSummary("Create a short link")]
    [EndpointDescription(
        "Shortens a URL, optionally with an expiration, one-time use, or a password. " +
        "Rejects localhost, loopback and private-network targets to prevent SSRF. " +
        "Rate limited to 20 requests per minute per IP.")]
    [ProducesResponseType<CreateShortLinkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreateShortLinkResponse>> Create(
        [FromBody] CreateShortLinkRequest request,
        CancellationToken cancellationToken)
    {
        var validation = UrlValidator.Validate(request.Url);

        if (!validation.IsValid)
        {
            ModelState.AddModelError(nameof(request.Url), DescribeError(validation.Error!.Value));
        }

        if (request.ExpiresAt is not null && request.ExpiresAt <= timeProvider.GetUtcNow())
        {
            ModelState.AddModelError(nameof(request.ExpiresAt), "ExpiresAt must be in the future.");
        }

        if (request.Password is not null && string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(nameof(request.Password), "Password cannot be blank.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var shortLink = await shortLinkService.CreateAsync(
            request.Url, request.ExpiresAt, request.IsOneTime, request.Password, cancellationToken);

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortLink.Code}";
        var response = new CreateShortLinkResponse(
            shortLink.Code,
            shortUrl,
            shortLink.OriginalUrl,
            shortLink.CreatedAt,
            shortLink.ExpiresAt,
            shortLink.IsOneTime,
            shortLink.HasPassword);

        return Created(shortUrl, response);
    }

    [HttpGet("{code}/stats")]
    [EndpointSummary("Get click stats for a link")]
    [EndpointDescription("Returns click count and current state, regardless of whether the link is still active.")]
    [ProducesResponseType<ShortLinkStatsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShortLinkStatsResponse>> GetStats(string code, CancellationToken cancellationToken)
    {
        var shortLink = await shortLinkService.GetByCodeAsync(code, cancellationToken);

        if (shortLink is null)
        {
            return NotFound();
        }

        return Ok(new ShortLinkStatsResponse(
            shortLink.Code,
            shortLink.ClickCount,
            shortLink.CreatedAt,
            shortLink.ExpiresAt,
            shortLink.IsOneTime,
            shortLink.IsActive));
    }

    [HttpDelete("{code}")]
    [EndpointSummary("Delete a link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        var deleted = await shortLinkService.DeleteAsync(code, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    private static string DescribeError(UrlValidationError error) => error switch
    {
        UrlValidationError.Empty => "The URL is required.",
        UrlValidationError.TooLong => $"The URL must be at most {UrlValidator.MaxUrlLength} characters.",
        UrlValidationError.InvalidFormat => "The URL is not a valid absolute URL.",
        UrlValidationError.UnsupportedScheme => "Only http and https URLs are allowed.",
        UrlValidationError.ForbiddenHost =>
            "This host is not allowed (localhost, loopback and private network addresses are rejected).",
        _ => "The URL is invalid.",
    };
}
