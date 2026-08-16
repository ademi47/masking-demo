using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MaskingDemo.Api.Masking;

namespace MaskingDemo.Api;

/// <summary>Feature registration kept out of Program.cs per project convention.</summary>
public static class MaskingExtensions
{
    public static IServiceCollection AddFieldMasking(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // HttpContextAccessor is backed by a static AsyncLocal, so this instance
        // resolves the same context as the DI-registered one. Same reasoning applies to
        // constructing the cipher directly here rather than resolving it from a container -
        // it only needs configuration, which is available before the app is built.
        var accessor = new HttpContextAccessor();
        var cipher = new AesGcmFieldCipher(configuration);
        services.AddSingleton<IFieldCipher>(cipher);

        services.AddControllers(options =>
            {
                options.Filters.Add<RejectMaskedValuesFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { MaskingModifier.Create(accessor, cipher) }
                };
            });

        return services;
    }
}
