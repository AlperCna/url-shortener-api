using System.Security.Cryptography;

namespace UrlShortener.Core.Codes;

/// <summary>
/// Generates random short codes drawn from the Base62 alphabet (0-9, a-z, A-Z).
/// Codes are generated from a cryptographically secure random source using
/// rejection sampling, not derived from a sequential id, so they are not
/// guessable from one another.
/// </summary>
public static class Base62CodeGenerator
{
    public const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public const int DefaultLength = 7;

    public static string Generate(int length = DefaultLength)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Code length must be positive.");
        }

        Span<char> code = length <= 64 ? stackalloc char[length] : new char[length];

        for (var i = 0; i < length; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
