using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Contracts;
using UrlShortener.Core.Services;
using UrlShortener.Core.Validation;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/links")]
public class LinksController(IShortLinkService shortLinkService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateShortLinkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateShortLinkResponse>> Create(
        [FromBody] CreateShortLinkRequest request,
        CancellationToken cancellationToken)
    {
        var validation = UrlValidator.Validate(request.Url);

        if (!validation.IsValid)
        {
            ModelState.AddModelError(nameof(request.Url), DescribeError(validation.Error!.Value));
            return ValidationProblem(ModelState);
        }

        var shortLink = await shortLinkService.CreateAsync(request.Url, cancellationToken);

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortLink.Code}";
        var response = new CreateShortLinkResponse(shortLink.Code, shortUrl, shortLink.OriginalUrl, shortLink.CreatedAt);

        return Created(shortUrl, response);
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
