namespace SmartSolar.Api.Middleware;

/// <summary>Thrown by services when a business rule (BR-*) is violated.</summary>
public class DomainException : Exception
{
    public string Code { get; }
    public string? Field { get; }
    public int StatusCode { get; }

    // Construct a domain exception from a documented error code, the user-facing
    // message and the HTTP status the contract assigns to that code.
    public DomainException(string code, string message, int statusCode = 409, string? field = null)
        : base(message)
    {
        Code = code;
        Field = field;
        StatusCode = statusCode;
    }
}
