using Npgsql;
using PersonalFinancialManagement.Infrastructure.Persistence;

namespace PersonalFinancialManagement.Tests.Persistence;

public class PostgresConnectionStringTests
{
    private static NpgsqlConnectionStringBuilder Parse(string value)
        => new(PostgresConnectionString.Normalize(value));

    [Fact]
    public void A_neon_style_uri_is_converted()
    {
        var result = Parse("postgresql://neondb_owner:npg_secret@ep-cool-123-pooler.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require");

        Assert.Equal("ep-cool-123-pooler.eu-central-1.aws.neon.tech", result.Host);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neondb_owner", result.Username);
        Assert.Equal("npg_secret", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
        Assert.Equal(5432, result.Port);
    }

    [Fact]
    public void The_postgres_scheme_a_port_and_encoded_credentials_are_understood()
    {
        var result = Parse("postgres://my%40user:p%3Ass%2Fword@db.example.com:6543/app%20db");

        Assert.Equal("db.example.com", result.Host);
        Assert.Equal(6543, result.Port);
        Assert.Equal("app db", result.Database);
        Assert.Equal("my@user", result.Username);
        Assert.Equal("p:ss/word", result.Password);
    }

    [Theory]
    [InlineData("disable", SslMode.Disable)]
    [InlineData("prefer", SslMode.Prefer)]
    [InlineData("require", SslMode.Require)]
    [InlineData("verify-ca", SslMode.VerifyCA)]
    [InlineData("verify-full", SslMode.VerifyFull)]
    public void The_ssl_mode_of_a_uri_is_mapped(string sslmode, SslMode expected)
    {
        Assert.Equal(expected, Parse($"postgresql://u:p@host/db?sslmode={sslmode}").SslMode);
    }

    [Fact]
    public void Unknown_uri_parameters_are_ignored_but_a_bad_ssl_mode_is_an_error()
    {
        Assert.Equal("db", Parse("postgresql://u:p@host/db?options=endpoint%3Dabc&sslmode=require").Database);
        Assert.Throws<FormatException>(() => PostgresConnectionString.Normalize("postgresql://u:p@host/db?sslmode=sometimes"));
    }

    [Fact]
    public void A_key_value_connection_string_is_kept()
    {
        var result = Parse("Host=localhost;Port=5433;Database=app;Username=me;Password=pw;SSL Mode=Require");

        Assert.Equal("localhost", result.Host);
        Assert.Equal(5433, result.Port);
        Assert.Equal("app", result.Database);
        Assert.Equal("me", result.Username);
        Assert.Equal("pw", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void The_session_time_zone_defaults_to_utc_unless_set()
    {
        Assert.Equal("UTC", Parse("postgresql://u:p@host/db").Timezone);
        Assert.Equal("UTC", Parse("Host=localhost;Database=app").Timezone);
        Assert.Equal("Asia/Tehran", Parse("Host=localhost;Database=app;Timezone=Asia/Tehran").Timezone);
    }
}
