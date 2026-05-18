using System.Text.Json;

namespace algo.Application.Common.CustomFields;

public static class JsonDocumentHelpers
{
    public static JsonDocument? CloneToDocument(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return JsonDocument.Parse(element.Value.GetRawText());
    }

    public static JsonElement? CloneToElement(JsonDocument? document)
    {
        return document is null ? null : document.RootElement.Clone();
    }

    public static object? ToPlainValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ToPlainValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ToPlainValue(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => null
        };
    }
}
