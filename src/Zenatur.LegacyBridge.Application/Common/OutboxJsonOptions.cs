using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zenatur.LegacyBridge.Application.Common;

internal static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
