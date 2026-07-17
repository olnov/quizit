using Npgsql;

namespace Backend.Data;

public static class PostgresConnectionString
{
    private static readonly IReadOnlyDictionary<string, string> UriParameterNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application_name"] = "Application Name",
            ["channel_binding"] = "Channel Binding",
            ["connect_timeout"] = "Timeout",
            ["sslmode"] = "SSL Mode",
            ["sslrootcert"] = "Root Certificate",
            ["sslcert"] = "SSL Certificate",
            ["sslkey"] = "SSL Key",
            ["sslpassword"] = "SSL Password",
            ["target_session_attrs"] = "Target Session Attributes",
        };

    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        if (string.IsNullOrWhiteSpace(uri.Host) || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
        {
            throw new ArgumentException("PostgreSQL URI must include a host and database name.", nameof(connectionString));
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
        };

        AddCredentials(uri, builder);
        AddQueryParameters(uri, builder);

        return builder.ConnectionString;
    }

    private static void AddCredentials(Uri uri, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrEmpty(uri.UserInfo))
        {
            return;
        }

        var separatorIndex = uri.UserInfo.IndexOf(':');
        var username = separatorIndex < 0 ? uri.UserInfo : uri.UserInfo[..separatorIndex];
        builder.Username = Uri.UnescapeDataString(username);

        if (separatorIndex >= 0)
        {
            builder.Password = Uri.UnescapeDataString(uri.UserInfo[(separatorIndex + 1)..]);
        }
    }

    private static void AddQueryParameters(Uri uri, NpgsqlConnectionStringBuilder builder)
    {
        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = item.IndexOf('=');
            var parameterName = Uri.UnescapeDataString(separatorIndex < 0 ? item : item[..separatorIndex]);
            var value = Uri.UnescapeDataString(separatorIndex < 0 ? string.Empty : item[(separatorIndex + 1)..]);
            var connectionStringName = UriParameterNames.TryGetValue(parameterName, out var mappedName)
                ? mappedName
                : parameterName.Replace('_', ' ');

            builder[connectionStringName] = value;
        }
    }
}
