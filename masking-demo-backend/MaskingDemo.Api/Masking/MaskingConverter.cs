using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaskingDemo.Api.Masking;

/// <summary>
/// Writes a masked, plaintext or encrypted value on the way out depending on the caller's
/// reveal mode. Read is a plain pass-through: request DTOs should not carry masked properties
/// at all, and RejectMaskedValuesFilter blocks anything that slips through.
/// </summary>
public sealed class MaskingConverter : JsonConverter<string>
{
    private readonly MaskKind _kind;
    private readonly IHttpContextAccessor _accessor;
    private readonly IFieldCipher _cipher;

    public MaskingConverter(MaskKind kind, IHttpContextAccessor accessor, IFieldCipher cipher)
    {
        _kind = kind;
        _accessor = accessor;
        _cipher = cipher;
    }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var context = _accessor.HttpContext;
        var mode = context is null ? RevealMode.Masked : MaskingPolicy.GetRevealMode(context);

        var output = mode switch
        {
            RevealMode.Plaintext => value,
            RevealMode.Encrypted => _cipher.Encrypt(value),
            _ => Mask.Apply(value, _kind)
        };

        writer.WriteStringValue(output);
    }
}
