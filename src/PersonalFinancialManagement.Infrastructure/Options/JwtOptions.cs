using System.Text;

namespace PersonalFinancialManagement.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PersonalFinancialManagement";

    public string Audience { get; set; } = "PersonalFinancialManagement.Client";

    // Must be provided through configuration (user-secrets, appsettings.Local.json or environment variables).
    public string SecretKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 120;

    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(SecretKey) || Encoding.UTF8.GetByteCount(SecretKey) < 32)
            throw new InvalidOperationException(
                $"'{SectionName}:{nameof(SecretKey)}' must be configured and at least 32 characters long.");

        if (ExpiryMinutes <= 0)
            throw new InvalidOperationException($"'{SectionName}:{nameof(ExpiryMinutes)}' must be greater than zero.");
    }
}
