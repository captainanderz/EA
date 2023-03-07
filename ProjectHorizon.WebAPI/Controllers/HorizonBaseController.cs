using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Security.Claims;

namespace ProjectHorizon.WebAPI.Controllers
{
    public class HorizonBaseController : ControllerBase
    {
        protected UserDto GetLoggedInUser()
        {
            ClaimsIdentity? claims = User.Identity as ClaimsIdentity;
            string sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            UserDto? loggedInUser = new UserDto
            {
                Id = claims.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserRole = claims.FindFirst(ClaimTypes.Role)?.Value,
                SourceIP = string.IsNullOrEmpty(sourceIp) ? "N/A" : sourceIp
            };

            string? subscriptionIdString = claims.FindFirst(HorizonClaimTypes.SubscriptionId)?.Value;
            if (subscriptionIdString != null)
            {
                loggedInUser.SubscriptionId = Guid.Parse(subscriptionIdString);
            }

            return loggedInUser;
        }

        protected void SetRefreshTokenCookie(string refreshToken)
        {
            CookieOptions? cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(1)
            };
            Response.Cookies.Append(AuthConstants.RefreshTokenCookieName, refreshToken, cookieOptions);
        }
    }
}
