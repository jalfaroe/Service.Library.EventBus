using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Library.EventBus.Internal
{
    internal static class Guard
    {
        public static void GuardArgumentIsNotNullOrEmpty(this string value, string paramName)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(paramName);
        }

        public static void GuardArgumentIsNotNullOrEmpty<T>(this IEnumerable<T> value, string paramName)
        {
            if (value == null || !value.Any()) throw new ArgumentNullException(paramName);
        }

        public static void GuardArgumentIsNotNull<T>(this T obj, string paramName)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(paramName);
        }
    }
}