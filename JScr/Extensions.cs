using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JScr
{
    internal static class EnumExtensions
    {
        /// <summary>
        /// Converts the enums value to a lowercase string that can be used for
        /// things like keywords for example.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToStringJScr(this Enum value)
        {
            return value.ToString().ToLower();
        }
    }

    internal static class ListExtensions
    {
        /// <summary>
        /// Removes the first element from a list and returns it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The removed item.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T Shift<T>(this List<T> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("List is empty.");
            }

            T firstElement = list[0];
            list.RemoveAt(0);

            return firstElement;
        }
    }
}
