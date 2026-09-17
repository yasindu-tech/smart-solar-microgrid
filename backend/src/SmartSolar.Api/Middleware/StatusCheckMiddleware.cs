using System.Security.Claims;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Middleware;

public class StatusCheckMiddleware
{
    private readonly RequestDelegate _next;

    // Constructor receives only the next delegate; repositories are resolved
    // per-request from the scoped DI container.
    public StatusCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Re-check the account's active status on every authenticated request.
    public async Task InvokeAsync(HttpContext context,
        UserRepository userRepo, ProsumerRepository prosumerRepo)
    {
        // Only check authenticated requests — skip anonymous endpoints.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subjectId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.FindFirstValue("sub");
            var role = context.User.FindFirstValue(ClaimTypes.Role);

            if (subjectId != null && role != null)
            {
                bool isAccountActive = await CheckAccountActiveAsync(
                    subjectId, role, userRepo, prosumerRepo);

                if (!isAccountActive)
                {
                    // Return the documented error envelope — account deactivated
                    // since token was issued (REQ-AUTH EC-6, BR-17).
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        code = "E4302",
                        message = "Your account has been deactivated. Contact Backoffice to reactivate.",
                        field = (string?)null,
                        traceId = context.TraceIdentifier
                    });
                    return;
                }
            }
        }

        await _next(context);
    }

    // Resolve the correct collection based on the role claim and check status.
    private static async Task<bool> CheckAccountActiveAsync(
        string subjectId, string role,
        UserRepository userRepo, ProsumerRepository prosumerRepo)
    {
        // Prosumers are keyed by NIC; web users by UserId.
        if (role == "Prosumer")
        {
            var prosumer = await prosumerRepo.GetByNicAsync(subjectId);
            return prosumer?.Status == "Active";
        }
        else
        {
            // Backoffice and Operator.
            var user = await userRepo.GetByUserIdAsync(subjectId);
            return user?.IsActive == true;
        }
    }
}
