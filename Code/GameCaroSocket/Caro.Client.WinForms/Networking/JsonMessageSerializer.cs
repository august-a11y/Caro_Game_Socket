using System.Text;
using System.Text.Json;

namespace Caro.Client.WinForms.Networking;

internal sealed class JsonMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public T? Deserialize<T>(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return JsonSerializer.Deserialize<T>(data, Options);
    }

    public byte[] Serialize<T>(T payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, Options));
    }
}
