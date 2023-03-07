using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProjectHorizon.ApplicationCore.Constants
{
    public abstract class BaseEnum<TEnum> where TEnum : BaseEnum<TEnum>
    {
        public static List<string> Values { get; } =
            typeof(TEnum)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetField)
                .Select(field => field.GetRawConstantValue().ToString())
                .ToList();
    }
}