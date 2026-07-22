namespace UrlShortener.Core.Exceptions;

/// <summary>
/// Thrown when a unique short code could not be generated after the allowed
/// number of collision retries. With a 62^7 code space this should only
/// happen if the table is enormous or something is wrong with generation.
/// </summary>
public class ShortCodeGenerationException(int attempts)
    : Exception($"Failed to generate a unique short code after {attempts} attempts.")
{
    public int Attempts { get; } = attempts;
}
