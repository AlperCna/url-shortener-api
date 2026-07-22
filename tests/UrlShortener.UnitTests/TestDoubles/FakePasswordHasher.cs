using UrlShortener.Core.Security;

namespace UrlShortener.UnitTests.TestDoubles;

/// <summary>
/// Not a real hash - just enough to prove ShortLinkService calls through
/// IPasswordHasher rather than storing the plain-text password.
/// </summary>
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string hash, string password) => hash == Hash(password);
}
