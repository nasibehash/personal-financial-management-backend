using PersonalFinancialManagement.Infrastructure.Services;

namespace PersonalFinancialManagement.Tests.Features;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_can_be_verified_with_the_same_password()
    {
        var hash = _hasher.Hash("Passw0rd123");

        Assert.True(_hasher.Verify("Passw0rd123", hash));
    }

    [Fact]
    public void Verify_rejects_a_different_password()
    {
        var hash = _hasher.Hash("Passw0rd123");

        Assert.False(_hasher.Verify("Passw0rd124", hash));
    }

    [Fact]
    public void Hashing_the_same_password_twice_gives_different_results()
    {
        Assert.NotEqual(_hasher.Hash("Passw0rd123"), _hasher.Hash("Passw0rd123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v2.1.AAAA.AAAA")]
    [InlineData("v1.1000.%%%.%%%")]
    public void Verify_returns_false_for_malformed_hashes(string hash)
    {
        Assert.False(_hasher.Verify("Passw0rd123", hash));
    }
}
