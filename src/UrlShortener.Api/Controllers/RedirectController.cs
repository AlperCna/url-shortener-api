using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController(IShortLinkService shortLinkService, TimeProvider timeProvider) : ControllerBase
{
    // 302, not 301: a permanent redirect gets cached by the browser, so
    // repeat visits never reach the server again and the click counter
    // (added later) would stop working.
    [HttpGet("/{code:regex(^[0-9a-zA-Z]{{7}}$)}")]
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

        await shortLinkService.RegisterClickAsync(shortLink, cancellationToken);

        return Redirect(shortLink.OriginalUrl);
    }
}
