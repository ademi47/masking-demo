using System.Text.Json.Serialization.Metadata;

namespace MaskingDemo.Api.Masking;

/// <summary>
/// Hooks into System.Text.Json contract metadata so every property carrying
/// MaskAttribute gets a masking converter automatically. No controller, DTO or
/// Angular component has to know masking exists.
/// </summary>
public static class MaskingModifier
{
    public static Action<JsonTypeInfo> Create(IHttpContextAccessor accessor, IFieldCipher cipher) => typeInfo =>
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var attribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(MaskAttribute), inherit: true)
                .Cast<MaskAttribute>()
                .FirstOrDefault();

            if (attribute is null)
            {
                continue;
            }

            property.CustomConverter = new MaskingConverter(attribute.Kind, accessor, cipher);
        }
    };
}
