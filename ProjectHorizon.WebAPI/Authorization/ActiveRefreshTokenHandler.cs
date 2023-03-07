using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Authorization
{
    public class ActiveRefreshTokenHandler : AuthorizationHandler<ActiveRefreshTokenRequirement>
    {
        private readonly IAuthService _authService;

        public ActiveRefreshTokenHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveRefreshTokenRequirement requirement)
        {
            if (context.Resource is AuthorizationFilterContext filterContext)
            {
                Microsoft.AspNetCore.Http.HttpContext? httpContext = filterContext.HttpContext;
                ClaimsIdentity? claims = httpContext.User.Identity as ClaimsIdentity;
                string? userId = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId is null)
                {
                    context.Fail();
                    return;
                }

                string? refreshToken = httpContext.Request.Cookies[AuthConstants.RefreshTokenCookieName];

                if (await _authService.IsRefreshTokenActiveAsync(userId, refreshToken))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
        }
    }
}
