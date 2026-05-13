using System.Text.RegularExpressions;

namespace Zenatur.LegacyBridge.Application.Common;

public static class PiiMask
{
    private static readonly Regex DocumentoInPath = new(
        @"(?i)(/v1/legacy/motoristas/)(\d{11,14})",
        RegexOptions.Compiled);

    public static string Cpf(string? cpf) =>
        cpf is { Length: 11 } ? $"{cpf[..3]}******{cpf[^2..]}" : "***";

    public static string Cnpj(string? cnpj) =>
        cnpj is { Length: 14 } ? $"{cnpj[..3]}******{cnpj[^2..]}" : "***";

    public static string Path(string? path) =>
        string.IsNullOrEmpty(path)
            ? string.Empty
            : DocumentoInPath.Replace(path, m =>
                m.Groups[1].Value + (m.Groups[2].Length == 14 ? Cnpj(m.Groups[2].Value) : Cpf(m.Groups[2].Value)));
}
