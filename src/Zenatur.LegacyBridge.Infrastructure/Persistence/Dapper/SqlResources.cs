using System.Reflection;

namespace Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper;

internal static class SqlResources
{
    private static readonly Assembly Asm = typeof(SqlResources).Assembly;
    private const string Prefix = "Zenatur.LegacyBridge.Infrastructure.Persistence.Dapper.Sql.";

    public static string Load(string fileName)
    {
        var resourceName = Prefix + fileName;
        using var stream = Asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded SQL resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
