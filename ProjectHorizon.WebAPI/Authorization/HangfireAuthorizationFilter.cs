using Hangfire.Annotations;
using Hangfire.Dashboard;
using ProjectHorizon.ApplicationCore.Constants;

namespace ProjectHorizon.WebAPI.Authorization
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            // allow only SuperAdmin to access the Hangfire Dashboard
            var httpContext = context.GetHttpContext();
            var isSuperAdmin = httpContext.User.IsInRole(UserRole.SuperAdmin);

            if (isSuperAdmin)
            {
                httpContext.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            }

            return isSuperAdmin;
        }
    }
}