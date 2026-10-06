using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StrictDateOnlyConverter : JsonConverter<DateTime>
{
    private const string FORMAT = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!DateTime.TryParseExact(value, FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new JsonException($"Date must be in {FORMAT} format.");
        }
        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(FORMAT, CultureInfo.InvariantCulture));
}
