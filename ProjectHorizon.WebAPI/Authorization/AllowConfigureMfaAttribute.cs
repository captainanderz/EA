using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace ProjectHorizon.WebAPI.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowConfigureMfaAttribute : Attribute, IFilterMetadata
    {
    }
}