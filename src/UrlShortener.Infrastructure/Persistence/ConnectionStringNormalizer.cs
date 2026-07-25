namespace UrlShortener.Infrastructure.Persistence;

/// <summary>
/// Converts a `postgres://user:pass@host:port/db` URI - the shape most PaaS
/// providers (Render, Heroku, Railway) hand out - into the ADO-style
/// connection string Npgsql expects. Passes ADO-style strings through
/// unchanged, so local/docker-compose configuration is unaffected.
/// </summary>
public static class ConnectionStringNormalizer
{
    public static string ToNpgsqlConnectionString(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return value;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};"
            + "SSL Mode=Require;Trust Server Certificate=true";
    }
}
