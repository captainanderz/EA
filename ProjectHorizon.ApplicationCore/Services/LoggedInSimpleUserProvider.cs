using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Security.Claims;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class LoggedInSimpleUserProvider : ILoggedInSimpleUserProvider
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public LoggedInSimpleUserProvider(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Gets the currently logged in user
        /// </summary>
        /// <returns>A dto representing the user that is currently logged in the application</returns>
        public SimpleUserDto? GetLoggedInUser()
        {
            ClaimsIdentity? claims = _contextAccessor.HttpContext.User.Identity as ClaimsIdentity;

            string? id = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? subscriptionIdString = claims?.FindFirst(HorizonClaimTypes.SubscriptionId)?.Value;
            string? name = claims?.FindFirst(ClaimConstants.Name)?.Value;
            string? email = claims?.FindFirst(ClaimTypes.Upn)?.Value;

            if (id is null || subscriptionIdString is null || name is null || email is null)
            {
                return null;
            }

            return new SimpleUserDto
            {
                Id = id,
                SubscriptionId = Guid.Parse(subscriptionIdString),
                Name = name,
                Email = email
            };
        }
    }
}