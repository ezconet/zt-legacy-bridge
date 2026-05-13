namespace Zenatur.LegacyBridge.Application.Common;

public sealed record AckRequest(
    long Id,
    bool Success,
    string? ErrorLog);
