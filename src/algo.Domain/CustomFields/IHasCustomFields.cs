using System.Text.Json;

namespace algo.Domain.CustomFields;

public interface IHasCustomFields
{
    JsonDocument? CustomFields { get; set; }
}
