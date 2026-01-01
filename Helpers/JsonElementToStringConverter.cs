using System;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace S3WebApi.Helpers;

public class JsonElementToStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the JSON element as raw text
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Write the string as raw JSON
        using var jsonDoc = JsonDocument.Parse(value);
        jsonDoc.RootElement.WriteTo(writer);
    }
}
