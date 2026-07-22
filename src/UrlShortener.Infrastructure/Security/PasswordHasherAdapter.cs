using Microsoft.AspNetCore.Identity;
using UrlShortener.Core.Security;

namespace UrlShortener.Infrastructure.Security;

/// <summary>
/// Adapts ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2,
/// versioned, random salt per hash) to the framework-agnostic
/// <see cref="IPasswordHasher"/> abstraction used by Core. The generic
/// TUser parameter isn't relevant to us, so a throwaway object stands in.
/// </summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Placeholder = new();

    public string Hash(string password) => Hasher.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(Placeholder, hash, password) != PasswordVerificationResult.Failed;
}
