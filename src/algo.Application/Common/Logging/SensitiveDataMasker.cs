using System.Collections.ObjectModel;
using System.Reflection;

namespace algo.Application.Common.Logging;

public static class SensitiveDataMasker
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "confirmPassword",
        "token",
        "refreshToken",
        "otp",
        "authorization",
        "newPassword",
        "currentPassword",
    };

    /// <summary>Builds a shallow, masked view of public instance properties for debug logging only.</summary>
    public static IReadOnlyDictionary<string, string> ToMaskedPropertyBag(object? request)
    {
        if (request is null)
        {
            return ReadOnlyDictionary<string, string>.Empty;
        }

        var type = request.GetType();
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? rawValue;
            try
            {
                rawValue = prop.GetValue(request);
            }
            catch
            {
                dict[prop.Name] = "(unreadable)";
                continue;
            }

            var isSensitive = SensitivePropertyNames.Contains(prop.Name);
            if (isSensitive)
            {
                dict[prop.Name] = "***";
                continue;
            }

            dict[prop.Name] = FormatValue(rawValue);
        }

        return new ReadOnlyDictionary<string, string>(dict);
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "(null)";
        }

        if (value is string s)
        {
            return s.Length > 256 ? s[..256] + "…" : s;
        }

        if (value is bool or byte or short or ushort or int or uint or long or ulong or float or double or decimal
            or DateTime or DateTimeOffset or Guid or Enum)
        {
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return "(redacted)";
    }
}
