using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentChat;

public record ChatMessage(
    string Type,
    string Nick,
    string Text,
    DateTime Time,
    string? To = null,
    List<string>? Users = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static ChatMessage? FromJson(string line)
    {
        return JsonSerializer.Deserialize<ChatMessage>(line, JsonOptions);
    }
}
