using FluentAssertions;
using UrlShortener.Infrastructure.Persistence;
using Xunit;

namespace UrlShortener.UnitTests.Infrastructure;

public class ConnectionStringNormalizerTests
{
    [Theory]
    [InlineData("postgres://user:pass@dpg-abc123.oregon-postgres.render.com/urlshortener")]
    [InlineData("postgresql://user:pass@dpg-abc123.oregon-postgres.render.com/urlshortener")]
    public void ToNpgsqlConnectionString_ConvertsUriStyleToAdoStyle(string uri)
    {
        var result = ConnectionStringNormalizer.ToNpgsqlConnectionString(uri);

        result.Should().Contain("Host=dpg-abc123.oregon-postgres.render.com");
        result.Should().Contain("Port=5432");
        result.Should().Contain("Database=urlshortener");
        result.Should().Contain("Username=user");
        result.Should().Contain("Password=pass");
        result.Should().Contain("SSL Mode=Require");
    }

    [Fact]
    public void ToNpgsqlConnectionString_WithExplicitPort_UsesIt()
    {
        var result = ConnectionStringNormalizer.ToNpgsqlConnectionString("postgres://user:pass@host:5433/db");

        result.Should().Contain("Port=5433");
    }

    [Fact]
    public void ToNpgsqlConnectionString_UrlEncodedCredentials_AreDecoded()
    {
        var result = ConnectionStringNormalizer.ToNpgsqlConnectionString("postgres://us%40er:p%40ss@host/db");

        result.Should().Contain("Username=us@er");
        result.Should().Contain("Password=p@ss");
    }

    [Fact]
    public void ToNpgsqlConnectionString_AdoStyleInput_IsPassedThroughUnchanged()
    {
        const string adoStyle = "Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres";

        var result = ConnectionStringNormalizer.ToNpgsqlConnectionString(adoStyle);

        result.Should().Be(adoStyle);
    }
}
