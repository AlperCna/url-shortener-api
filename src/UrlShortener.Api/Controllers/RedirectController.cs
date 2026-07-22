using Microsoft.AspNetCore.Mvc;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController(IShortLinkService shortLinkService) : ControllerBase
{
    // 302, not 301: a permanent redirect gets cached by the browser, so
    // repeat visits never reach the server again and the click counter
    // (added later) would stop working.
    [HttpGet("/{code:regex(^[0-9a-zA-Z]{{7}}$)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToOriginalUrl(string code, CancellationToken cancellationToken)
    {
        var shortLink = await shortLinkService.GetByCodeAsync(code, cancellationToken);

        if (shortLink is null)
        {
            return NotFound();
        }

        return Redirect(shortLink.OriginalUrl);
    }
}
