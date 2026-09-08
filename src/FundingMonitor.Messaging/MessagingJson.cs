using System.Text.Json;
using System.Text.Json.Serialization;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.Messaging;

public static class MessagingJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static byte[] SerializeToUtf8Bytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public static T? Deserialize<T>(ReadOnlySpan<byte> json) =>
        JsonSerializer.Deserialize<T>(json, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new ExchangeIdConverter());
        options.Converters.Add(new SymbolConverter());
        return options;
    }

    private sealed class ExchangeIdConverter : JsonConverter<ExchangeId>
    {
        public override ExchangeId Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            ExchangeId.Parse(ReadValue(ref reader));

        public override void Write(Utf8JsonWriter writer, ExchangeId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }

    private sealed class SymbolConverter : JsonConverter<Symbol>
    {
        public override Symbol Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            Symbol.Create(ReadValue(ref reader));

        public override void Write(Utf8JsonWriter writer, Symbol value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }

    private static string ReadValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString() ?? throw new JsonException("Value object is empty.");
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Value object must be a string.");
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.TryGetProperty("value", out var camel) ||
            document.RootElement.TryGetProperty("Value", out camel))
            return camel.GetString() ?? throw new JsonException("Value object is empty.");
        throw new JsonException("Value object property is missing.");
    }
}
