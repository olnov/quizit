using Backend.Data;
using Npgsql;
using Xunit;

namespace Backend.Tests;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_ReturnsAdoNetConnectionStringsUnchanged()
    {
        const string connectionString = "Host=db;Port=5432;Database=quizit;Username=quizit;Password=secret";

        var normalized = PostgresConnectionString.Normalize(connectionString);

        Assert.Equal(connectionString, normalized);
    }

    [Fact]
    public void Normalize_ConvertsPostgreSqlUriToNpgsqlConnectionString()
    {
        var normalized = PostgresConnectionString.Normalize(
            "postgresql://quizit:secret%21@db.example.com:5433/quizit?sslmode=require&application_name=Quizz");
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("quizit", builder.Database);
        Assert.Equal("quizit", builder.Username);
        Assert.Equal("secret!", builder.Password);
        Assert.Equal("Require", builder["SSL Mode"]?.ToString());
        Assert.Equal("Quizz", builder.ApplicationName);
    }
}
