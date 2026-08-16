namespace MaskingDemo.Api.Masking;

/// <summary>
/// Pure masking functions. Public so non-serialiser paths (CSV export, logging,
/// notification templates) reuse the exact same formats.
/// </summary>
public static class Mask
{
    public const char MaskChar = '*';

    public static string Apply(string? value, MaskKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        return kind switch
        {
            MaskKind.Nric     => Nric(value),
            MaskKind.Account  => KeepLast(value, keep: 4, fixedStars: 6),
            MaskKind.Phone    => KeepLast(value, keep: 4, fixedStars: 4),
            MaskKind.Email    => Email(value),
            MaskKind.YearOnly => value.Length >= 4 ? value[..4] : value,
            _                 => value
        };
    }

    /// <summary>S1234567D =&gt; S****567D. Prefix letter kept, last 4 kept.</summary>
    private static string Nric(string value)
    {
        if (value.Length < 5)
        {
            return new string(MaskChar, value.Length);
        }

        var prefix = value[0];
        var tail = value[^4..];
        var stars = new string(MaskChar, value.Length - 5);
        return $"{prefix}{stars}{tail}";
    }

    private static string KeepLast(string value, int keep, int fixedStars)
    {
        if (value.Length <= keep)
        {
            return new string(MaskChar, value.Length);
        }

        return new string(MaskChar, fixedStars) + value[^keep..];
    }

    /// <summary>thiek.tan@example.com =&gt; t********n@example.com</summary>
    private static string Email(string value)
    {
        var at = value.IndexOf('@');
        if (at <= 1)
        {
            return new string(MaskChar, Math.Max(value.Length, 1));
        }

        var local = value[..at];
        var domain = value[at..];
        var masked = local.Length <= 2
            ? new string(MaskChar, local.Length)
            : $"{local[0]}{new string(MaskChar, local.Length - 2)}{local[^1]}";

        return masked + domain;
    }
}
