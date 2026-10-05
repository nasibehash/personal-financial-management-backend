using System.Text;
using System.Text.RegularExpressions;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

internal static class PersianText
{
    private static readonly Regex ThousandsSeparator = new(@"(?<=\d)[,\u066C\u060C](?=\d{3})", RegexOptions.Compiled);

    // Lower-cases, converts Persian/Arabic digits to ASCII, unifies Arabic and Persian letter forms,
    // turns half-spaces (ZWNJ) into spaces and drops thousands separators ("1,500,000" -> "1500000").
    public static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)('0' + (c - '\u06F0')),
                >= '\u0660' and <= '\u0669' => (char)('0' + (c - '\u0660')),
                '\u064A' => '\u06CC', // Arabic yeh -> Persian yeh
                '\u0643' => '\u06A9', // Arabic kaf -> Persian kaf
                '\u200C' => ' ',
                _ => char.ToLowerInvariant(c)
            });
        }

        return ThousandsSeparator.Replace(builder.ToString(), string.Empty).Trim();
    }
}
