using FluentResults;

namespace Zenatur.LegacyBridge.Application.Common;

public sealed class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

public sealed class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}

public sealed class InfraError : Error
{
    public InfraError(string message) : base(message) { }
}
