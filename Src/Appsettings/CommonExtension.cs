using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    internal static class CommonExtension
    {
        /// <summary>
        /// 确定此字符串是否与指定的 System.String 对象具有相同的值，忽略大小写
        /// </summary>
        /// <param name="thisValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string thisValue, string value)
        {
            return string.Equals(thisValue, value, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// 是否以value结尾，忽略大小写
        /// </summary>
        /// <param name="thisValue"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EndsWithIgnoreCase(this string thisValue, string value)
        {
            if (string.IsNullOrEmpty(thisValue)) return false;

            return thisValue.EndsWith(value, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
