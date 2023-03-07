using Microsoft.AspNetCore.Http;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class LoggedInUserProvider : ILoggedInUserProvider
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public LoggedInUserProvider(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Gets the currently logged in user
        /// </summary>
        /// <returns>A dto representing the user that is currently logged in the application</returns>
        public UserDto? GetLoggedInUser()
        {
            ClaimsIdentity? claims = _contextAccessor.HttpContext.User.Identity as ClaimsIdentity;
            string? sourceIp = GetRemoteIp();
            string? userRole = claims?.FindFirst(ClaimTypes.Role)?.Value;
            string? id = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? subscriptionIdString = claims?.FindFirst(HorizonClaimTypes.SubscriptionId)?.Value;

            if (userRole is null || id is null || subscriptionIdString is null)
            {
                return null;
            }

            UserDto? loggedInUser = new UserDto
            {
                Id = id,
                UserRole = userRole,
                SourceIP = userRole == UserRole.SuperAdmin ? string.Empty : sourceIp,
                SubscriptionId = Guid.Parse(subscriptionIdString),
            };

            return loggedInUser;
        }

        /// <summary>
        /// Gets the remote ip of the user
        /// </summary>
        /// <returns>A string representing the IP address of the machine the user is currently logged in the application</returns>
        private string GetRemoteIp()
        {
            string remoteIp = _contextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(remoteIp))
            {
                remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            return !string.IsNullOrEmpty(remoteIp) ? remoteIp : "N/A";
        }

    }
}