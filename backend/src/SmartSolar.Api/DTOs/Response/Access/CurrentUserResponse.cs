namespace SmartSolar.Api.DTOs.Response.Access;

public record CurrentUserResponse(
    // UserId or NIC — whatever is in the "sub" claim of the token.
    string SubjectId,

    // "Backoffice", "Operator", or "Prosumer".
    string Role,

    // Display name from the "name" claim.
    string DisplayName
);
