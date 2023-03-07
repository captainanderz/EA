using System;

namespace ProjectHorizon.WebAPI.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowEnterMfaCodeAttribute : Attribute
    {
    }
}