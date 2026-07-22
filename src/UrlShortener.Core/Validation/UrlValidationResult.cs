namespace UrlShortener.Core.Validation;

public readonly record struct UrlValidationResult
{
    public bool IsValid { get; }

    public UrlValidationError? Error { get; }

    private UrlValidationResult(bool isValid, UrlValidationError? error)
    {
        IsValid = isValid;
        Error = error;
    }

    public static UrlValidationResult Success() => new(true, null);

    public static UrlValidationResult Failure(UrlValidationError error) => new(false, error);
}
