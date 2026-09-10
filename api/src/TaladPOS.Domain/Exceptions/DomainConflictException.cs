namespace TaladPOS.Domain.Exceptions;

/// <summary>
/// Base type for domain-rule violations that depend on concurrent or existing state
/// (e.g. insufficient stock, double-void) — mapped to HTTP 409 by the API's error-handling
/// middleware per contracts/README.md conventions.
/// </summary>
public class DomainConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
