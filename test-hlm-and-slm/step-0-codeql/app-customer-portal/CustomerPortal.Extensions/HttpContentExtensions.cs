using System.Net.Http.Json;
using System.Text.Json;

namespace CustomerPortal.Extensions;

public static class HttpContentExtensions
{
    public static async Task<(string? StringValue, T? Value)> ReadFromJsonSafeAsync<T>(
        this HttpContent content
    )
        where T : class
    {
        var stringValue = await content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(stringValue))
            return (null, null);

        try
        {
            var value = JsonSerializer.Deserialize<T>(stringValue);
            return (stringValue, value);
        }
        catch (JsonException)
        {
            return (stringValue, null);
        }
    }
}
