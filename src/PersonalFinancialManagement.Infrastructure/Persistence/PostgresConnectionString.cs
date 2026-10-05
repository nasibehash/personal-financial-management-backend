using Npgsql;

namespace PersonalFinancialManagement.Infrastructure.Persistence;

// Accepts a connection string in either form:
//   Npgsql key/value:  Host=...;Database=...;Username=...;Password=...;SSL Mode=Require
//   URI:               postgresql://user:password@host/database?sslmode=require   (what Neon's dashboard shows)
public static class PostgresConnectionString
{
    public static string Normalize(string value)
    {
        var trimmed = value.Trim();

        var builder = IsUri(trimmed)
            ? FromUri(new Uri(trimmed))
            : new NpgsqlConnectionStringBuilder(trimmed);

        // Dates are stored as UTC; a UTC session keeps server-side date functions (e.g. grouping by month) consistent.
        if (string.IsNullOrWhiteSpace(builder.Timezone))
            builder.Timezone = "UTC";

        return builder.ConnectionString;
    }

    private static bool IsUri(string value)
        => value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
           || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static NpgsqlConnectionStringBuilder FromUri(Uri uri)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'))
        };

        if (uri.Port > 0)
            builder.Port = uri.Port;

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var separator = uri.UserInfo.IndexOf(':');
            builder.Username = Uri.UnescapeDataString(separator < 0 ? uri.UserInfo : uri.UserInfo[..separator]);
            if (separator >= 0)
                builder.Password = Uri.UnescapeDataString(uri.UserInfo[(separator + 1)..]);
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]).ToLowerInvariant();
            var parameterValue = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            switch (key)
            {
                case "sslmode":
                    builder.SslMode = parameterValue.ToLowerInvariant() switch
                    {
                        "disable" => SslMode.Disable,
                        "allow" => SslMode.Allow,
                        "prefer" => SslMode.Prefer,
                        "require" => SslMode.Require,
                        "verify-ca" => SslMode.VerifyCA,
                        "verify-full" => SslMode.VerifyFull,
                        _ => throw new FormatException($"Unsupported sslmode '{parameterValue}' in the connection string.")
                    };
                    break;

                case "channel_binding":
                    builder.ChannelBinding = parameterValue.ToLowerInvariant() switch
                    {
                        "disable" => ChannelBinding.Disable,
                        "prefer" => ChannelBinding.Prefer,
                        "require" => ChannelBinding.Require,
                        _ => throw new FormatException($"Unsupported channel_binding '{parameterValue}' in the connection string.")
                    };
                    break;

                case "application_name":
                    builder.ApplicationName = parameterValue;
                    break;

                // Other URI parameters (for example "options") have no Npgsql equivalent and are ignored.
            }
        }

        return builder;
    }
}
