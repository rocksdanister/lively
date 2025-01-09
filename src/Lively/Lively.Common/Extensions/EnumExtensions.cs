using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Lively.Common.Extensions
{
    public static class EnumExtensions
    {
        public static string GetAttrValue<T>(this T enumVal) where T : Enum
        {
            var enumType = typeof(T);
            var memInfo = enumType.GetMember(enumVal.ToString());
            var attr = memInfo.FirstOrDefault()?.GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
            return attr?.Value;
        }
    }
}
