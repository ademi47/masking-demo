using System.Security.Cryptography;
using System.Text;

namespace MaskingDemo.Api.Masking;

public interface IFieldCipher
{
    string Encrypt(string plaintext);
}

/// <summary>
/// AES-256-GCM, one call per field with a fresh random nonce. Output is
/// "base64(nonce):base64(ciphertext+tag)" - that layout (tag appended to ciphertext, nonce
/// separate) is exactly what the browser's SubtleCrypto.decrypt('AES-GCM', ...) expects.
///
/// DEMO ONLY: the key is a single static secret shared with the Angular bundle
/// (see crypto.service.ts). That keeps PII out of the wire/logs, which is the point of this
/// demo, but it is not a boundary against the authorised viewer's own browser - anyone with
/// Sources/Console on an already-decrypted session can find it. Production should issue
/// short-lived per-session keys via an authenticated exchange (or envelope-encrypt with a KMS),
/// not bake a shared secret into the SPA bundle.
/// </summary>
public sealed class AesGcmFieldCipher : IFieldCipher
{
    private readonly byte[] _key;

    public AesGcmFieldCipher(IConfiguration configuration)
    {
        var base64Key = configuration["Masking:DemoEncryptionKeyBase64"]
            ?? throw new InvalidOperationException("Masking:DemoEncryptionKeyBase64 is not configured.");

        _key = Convert.FromBase64String(base64Key);
    }

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using (var aes = new AesGcm(_key, tag.Length))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var sealedBytes = new byte[cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(cipherBytes, 0, sealedBytes, 0, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, sealedBytes, cipherBytes.Length, tag.Length);

        return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(sealedBytes)}";
    }
}
