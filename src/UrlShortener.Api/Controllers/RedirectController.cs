using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.BackgroundProcessing;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController(
    IShortLinkService shortLinkService,
    IClickTrackingQueue clickTrackingQueue,
    TimeProvider timeProvider) : ControllerBase
{
    // 302, not 301: a permanent redirect gets cached by the browser, so
    // repeat visits never reach the server again and the click counter
    // would stop working.
    // Square brackets and braces are both escaped by doubling ([[ ]] and
    // {{ }}) - the attribute route template parser treats unescaped [...]
    // as an [area]/[controller]/[action] token to substitute, which would
    // otherwise collide with the regex character class here.
    [HttpGet("/{code:regex(^[[0-9a-zA-Z]]{{7}}$)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> RedirectToOriginalUrl(
        string code,
        [FromQuery] string? password,
        CancellationToken cancellationToken)
    {
        var shortLink = await shortLinkService.GetByCodeAsync(code, cancellationToken);

        if (shortLink is null)
        {
            return NotFound();
        }

        if (!shortLink.IsAccessible(timeProvider.GetUtcNow()))
        {
            return Problem(statusCode: StatusCodes.Status410Gone, title: "This link is no longer available.");
        }

        if (shortLink.HasPassword)
        {
            if (string.IsNullOrEmpty(password))
            {
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "A password is required to access this link.");
            }

            if (!shortLinkService.VerifyPassword(shortLink, password))
            {
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Incorrect password.");
            }
        }

        if (shortLink.IsOneTime)
        {
            // Deactivation gates future access, so it must happen before we
            // redirect - queuing it would let a second concurrent request
            // through before the first click is recorded.
            await shortLinkService.RegisterClickAsync(shortLink, cancellationToken);
        }
        else
        {
            // A plain click count is just a statistic - fine to lag behind
            // by a few milliseconds, not worth blocking the redirect for.
            clickTrackingQueue.Enqueue(shortLink.Code);
        }

        return Redirect(shortLink.OriginalUrl);
    }
}
