using System.Security.Claims;

namespace MaskingDemo.Api.Masking;

/// <summary>
/// Decides whether the current caller may see unmasked values.
/// DEMO ONLY: driven by a request header so the behaviour is easy to show locally.
/// In CPF this must read a claim from the validated JWT - see CanViewUnmaskedProduction.
/// </summary>
public static class MaskingPolicy
{
    public const string DemoRoleHeader = "X-Demo-Role";
    public const string PrivilegedRole = "officer";

    public const string DemoRevealHeader = "X-Demo-Reveal";
    public const string EncryptedReveal = "encrypted";

    public static bool CanViewUnmasked(HttpContext context)
    {
        var role = context.Request.Headers[DemoRoleHeader].ToString();
        return string.Equals(role, PrivilegedRole, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Masking is about WHO may see a value; reveal mode is about HOW it's sent to someone
    /// already authorised to see it. Encrypted still requires the same privilege as plaintext.
    /// </summary>
    public static RevealMode GetRevealMode(HttpContext context)
    {
        if (!CanViewUnmasked(context))
        {
            return RevealMode.Masked;
        }

        var reveal = context.Request.Headers[DemoRevealHeader].ToString();
        return string.Equals(reveal, EncryptedReveal, StringComparison.OrdinalIgnoreCase)
            ? RevealMode.Encrypted
            : RevealMode.Plaintext;
    }

    // Production shape - swap CanViewUnmasked for this once JWT auth is wired up.
    public static bool CanViewUnmaskedProduction(ClaimsPrincipal user) =>
        user.HasClaim("permission", "member.pii.read");
}

/// <summary>How a masked field is written to the response for the current request.</summary>
public enum RevealMode
{
    Masked,
    Plaintext,
    Encrypted
}
