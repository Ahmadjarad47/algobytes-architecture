using System.Text.Json;
using algo.Application.Abstractions;
using algo.Domain.CustomFields;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Common.CustomFields;

public sealed class CustomFieldValueValidator(IApplicationDbContext db)
{
    public async Task<JsonDocument> ValidateAndNormalizeAsync(
        string entity,
        JsonElement? customFields,
        CancellationToken cancellationToken)
    {
        if (!CustomFieldEntities.Supported.Contains(entity))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(entity), $"Unsupported custom field entity '{entity}'.")
            });
        }

        var definitions = await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(definition => definition.Entity == entity)
            .OrderBy(definition => definition.Label)
            .ToListAsync(cancellationToken);

        var input = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (customFields is { ValueKind: JsonValueKind.Object } objectElement)
        {
            foreach (var property in objectElement.EnumerateObject())
            {
                input[property.Name] = property.Value.Clone();
            }
        }
        else if (customFields is { } invalidElement && invalidElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("customFields", "Custom fields payload must be a JSON object.")
            });
        }

        var output = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (!input.TryGetValue(definition.Key, out var value))
            {
                if (definition.DefaultValueJson is not null)
                {
                    output[definition.Key] = JsonDocumentHelpers.ToPlainValue(definition.DefaultValueJson.RootElement);
                    continue;
                }

                if (definition.Required)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure($"customFields.{definition.Key}", $"'{definition.Label}' is required.")
                    });
                }

                continue;
            }

            output[definition.Key] = ValidateValue(definition, value);
        }

        foreach (var extra in input.Keys.Except(definitions.Select(definition => definition.Key), StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure($"customFields.{extra}", $"Unknown custom field '{extra}'.")
            });
        }

        return JsonDocument.Parse(JsonSerializer.Serialize(output));
    }

    private static object? ValidateValue(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            if (definition.Required)
            {
                throw Missing(definition);
            }

            return null;
        }

        return definition.Type switch
        {
            CustomFieldType.Text => ValidateText(definition, value),
            CustomFieldType.Number => ValidateNumber(definition, value),
            CustomFieldType.Boolean => ValidateBoolean(definition, value),
            CustomFieldType.Date => ValidateDate(definition, value),
            CustomFieldType.Select => ValidateSelect(definition, value),
            CustomFieldType.MultiSelect => ValidateMultiSelect(definition, value),
            CustomFieldType.Json => JsonSerializer.Deserialize<object?>(value.GetRawText()),
            _ => throw new ValidationException(new[]
            {
                new ValidationFailure($"customFields.{definition.Key}", $"Unsupported custom field type '{definition.Type}'.")
            })
        };
    }

    private static object ValidateText(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid(definition, "must be text.");
        }

        return value.GetString() ?? string.Empty;
    }

    private static object ValidateNumber(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
        {
            throw Invalid(definition, "must be a number.");
        }

        return number;
    }

    private static object ValidateBoolean(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw Invalid(definition, "must be true or false.");
        }

        return value.GetBoolean();
    }

    private static object ValidateDate(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(value.GetString(), out var date))
        {
            throw Invalid(definition, "must be an ISO date string.");
        }

        return date.ToString("O");
    }

    private static object ValidateSelect(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid(definition, "must be one of the configured options.");
        }

        var selected = value.GetString() ?? string.Empty;
        var options = ReadStringOptions(definition);
        if (options.Count > 0 && !options.Contains(selected))
        {
            throw Invalid(definition, "must match one of the configured options.");
        }

        return selected;
    }

    private static object ValidateMultiSelect(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Invalid(definition, "must be an array of configured options.");
        }

        var options = ReadStringOptions(definition);
        var values = new List<string>();

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw Invalid(definition, "must contain only string values.");
            }

            var selected = item.GetString() ?? string.Empty;
            if (options.Count > 0 && !options.Contains(selected))
            {
                throw Invalid(definition, $"contains an unsupported option '{selected}'.");
            }

            values.Add(selected);
        }

        return values;
    }

    private static HashSet<string> ReadStringOptions(CustomFieldDefinition definition)
    {
        if (definition.OptionsJson?.RootElement.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return definition.OptionsJson.RootElement
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ValidationException Missing(CustomFieldDefinition definition) => new(new[]
    {
        new ValidationFailure($"customFields.{definition.Key}", $"'{definition.Label}' is required.")
    });

    private static ValidationException Invalid(CustomFieldDefinition definition, string message) => new(new[]
    {
        new ValidationFailure($"customFields.{definition.Key}", $"'{definition.Label}' {message}")
    });
}
