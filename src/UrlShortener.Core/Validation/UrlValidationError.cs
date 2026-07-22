namespace UrlShortener.Core.Validation;

public enum UrlValidationError
{
    Empty,
    TooLong,
    InvalidFormat,
    UnsupportedScheme,
    ForbiddenHost,
}
