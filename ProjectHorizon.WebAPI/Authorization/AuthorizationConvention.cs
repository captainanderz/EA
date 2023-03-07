using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using ProjectHorizon.ApplicationCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectHorizon.WebAPI.Authorization
{
    public class AuthorizationConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            bool HasAttributeOfType<TAttribute>() where TAttribute : Attribute =>
                action.Attributes.OfType<TAttribute>().Any() || action.Controller.Attributes.OfType<TAttribute>().Any();

            if (HasAzurePolicy(action.Attributes) || HasAzurePolicy(action.Controller.Attributes))
            {
                return;
            }

            if (HasSimplePolicy(action.Attributes) || HasSimplePolicy(action.Controller.Attributes))
            {
                return;
            }

            List<string>? claimValues =
                HasAttributeOfType<AllowConfigureMfaAttribute>()
                    ? new List<string> { HorizonClaimTypes.MfaClaimValues.Ok, HorizonClaimTypes.MfaClaimValues.NeedToConfigure }
                    : HasAttributeOfType<AllowEnterMfaCodeAttribute>()
                        ? new List<string> { HorizonClaimTypes.MfaClaimValues.NeedToEnterCode }
                        : new List<string> { HorizonClaimTypes.MfaClaimValues.Ok };

            AuthorizationPolicy? policy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(HorizonClaimTypes.Mfa, claimValues)
                .AddRequirements(new ActiveRefreshTokenRequirement())
                .Build();

            action.Filters.Add(new AuthorizeFilter(policy));
        }

        private bool HasAzurePolicy(IReadOnlyList<object> attributes)
        {
            AuthorizeAttribute? attribute = attributes
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault(att => att.Policy == AuthConstants.AzurePolicy);

            return attribute != null;
        }

        private bool HasSimplePolicy(IReadOnlyList<object> attributes)
        {
            AuthorizeAttribute? attribute = attributes
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault(att => att.Policy == AuthConstants.SimplePolicy);

            return attribute != null;
        }
    }
}