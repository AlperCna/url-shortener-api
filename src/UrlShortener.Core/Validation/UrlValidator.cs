using System.Net;
using System.Net.Sockets;

namespace UrlShortener.Core.Validation;

/// <summary>
/// Validates URLs submitted for shortening. Beyond basic shape checks, this
/// rejects targets that would let the server be used as an SSRF proxy into
/// its own network: localhost, loopback, and the private/link-local IPv4
/// and IPv6 ranges (including 169.254.169.254, the common cloud metadata
/// endpoint).
/// </summary>
public static class UrlValidator
{
    public const int MaxUrlLength = 2048;

    private static readonly string[] AllowedSchemes = ["http", "https"];

    public static UrlValidationResult Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Failure(UrlValidationError.Empty);
        }

        if (url.Length > MaxUrlLength)
        {
            return UrlValidationResult.Failure(UrlValidationError.TooLong);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return UrlValidationResult.Failure(UrlValidationError.InvalidFormat);
        }

        if (!AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return UrlValidationResult.Failure(UrlValidationError.UnsupportedScheme);
        }

        if (IsForbiddenHost(uri.Host))
        {
            return UrlValidationResult.Failure(UrlValidationError.ForbiddenHost);
        }

        return UrlValidationResult.Success();
    }

    private static bool IsForbiddenHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Not a literal IP address (a regular hostname) - we don't resolve DNS here,
        // so it passes; a hostname that resolves to a private range at request time
        // is a follow-up concern (DNS rebinding), not covered by this check.
        return IPAddress.TryParse(host, out var ip) && IsPrivateOrReserved(ip);
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            return IsPrivateOrReserved(ip.MapToIPv4());
        }

        return ip.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPrivateIPv4(ip),
            AddressFamily.InterNetworkV6 => IsPrivateIPv6(ip),
            _ => false,
        };
    }

    private static bool IsPrivateIPv4(IPAddress ip)
    {
        var b = ip.GetAddressBytes();

        return b[0] switch
        {
            10 => true, // 10.0.0.0/8
            172 => b[1] is >= 16 and <= 31, // 172.16.0.0/12
            192 => b[1] == 168, // 192.168.0.0/16
            169 => b[1] == 254, // 169.254.0.0/16 - link-local, includes cloud metadata (169.254.169.254)
            _ => false,
        };
    }

    private static bool IsPrivateIPv6(IPAddress ip)
    {
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
        {
            return true;
        }

        // fc00::/7 - unique local addresses (the IPv6 equivalent of RFC 1918).
        var firstByte = ip.GetAddressBytes()[0];
        return (firstByte & 0xFE) == 0xFC;
    }
}
