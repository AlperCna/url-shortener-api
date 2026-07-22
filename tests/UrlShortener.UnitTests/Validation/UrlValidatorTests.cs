using FluentAssertions;
using UrlShortener.Core.Validation;
using Xunit;

namespace UrlShortener.UnitTests.Validation;

public class UrlValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyUrl_FailsWithEmpty(string? url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.Empty);
    }

    [Fact]
    public void Validate_LongerThanMaxLength_FailsWithTooLong()
    {
        var url = "https://example.com/" + new string('a', UrlValidator.MaxUrlLength);

        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.TooLong);
    }

    [Fact]
    public void Validate_AtMaxLength_Passes()
    {
        var prefix = "https://example.com/";
        var url = prefix + new string('a', UrlValidator.MaxUrlLength - prefix.Length);
        url.Length.Should().Be(UrlValidator.MaxUrlLength);

        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    public void Validate_MalformedUrl_FailsWithInvalidFormat(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.InvalidFormat);
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("gopher://example.com")]
    public void Validate_UnsupportedScheme_FailsWithUnsupportedScheme(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.UnsupportedScheme);
    }

    [Theory]
    [InlineData("http://localhost/")]
    [InlineData("http://LOCALHOST/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://127.255.255.255/")]
    [InlineData("http://[::1]/")]
    public void Validate_LoopbackHost_FailsWithForbiddenHost(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.ForbiddenHost);
    }

    [Theory]
    [InlineData("http://10.0.0.1/")]
    [InlineData("http://10.255.255.255/")]
    [InlineData("http://172.16.0.1/")]
    [InlineData("http://172.31.255.255/")]
    [InlineData("http://192.168.0.1/")]
    [InlineData("http://192.168.255.255/")]
    public void Validate_PrivateIPv4Host_FailsWithForbiddenHost(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.ForbiddenHost);
    }

    [Theory]
    [InlineData("http://169.254.169.254/")] // cloud metadata endpoint
    [InlineData("http://169.254.0.1/")]
    public void Validate_LinkLocalIPv4Host_FailsWithForbiddenHost(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.ForbiddenHost);
    }

    [Theory]
    [InlineData("http://[fe80::1]/")] // link-local
    [InlineData("http://[fc00::1]/")] // unique local
    [InlineData("http://[fd12:3456:789a::1]/")] // unique local
    public void Validate_PrivateIPv6Host_FailsWithForbiddenHost(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UrlValidationError.ForbiddenHost);
    }

    [Theory]
    [InlineData("http://172.15.255.255/")] // just below 172.16.0.0/12
    [InlineData("http://172.32.0.0/")] // just above 172.16.0.0/12
    [InlineData("http://192.167.255.255/")]
    [InlineData("http://169.253.255.255/")]
    [InlineData("http://11.0.0.1/")]
    public void Validate_IPv4AddressOutsidePrivateRanges_Passes(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?query=1")]
    [InlineData("https://sub.example.com:8443/a/b/c")]
    [InlineData("http://8.8.8.8/")]
    public void Validate_LegitimatePublicUrl_Passes(string url)
    {
        var result = UrlValidator.Validate(url);

        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}
