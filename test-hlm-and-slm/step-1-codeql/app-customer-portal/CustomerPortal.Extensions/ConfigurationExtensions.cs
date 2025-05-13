using Microsoft.Extensions.Configuration;

namespace CustomerPortal.Extensions;

public static class ConfigurationExtensions
{
    public static T GetValueOrThrow<T>(this IConfiguration configuration, string key) =>
        configuration.GetValue<T>(key)
        ?? throw new InvalidOperationException($"Configuration is missing value for key: {key}");
}
