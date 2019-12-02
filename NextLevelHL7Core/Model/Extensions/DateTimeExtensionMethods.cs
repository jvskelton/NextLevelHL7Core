using System;
using System.Globalization;

namespace NextLevelHL7.Extensions
{
    public static class DateTimeExtensionMethods
    {
        /// <summary>
        /// Parses an HL7 date using common date time styles.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DateTime? ParseDate(this string input)
        {
            DateTime t;
            if (DateTime.TryParseExact(input, "yyyyMMddHHmmsszzz", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out t))
                return t;
            else if (DateTime.TryParseExact(input, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out t))
                return t;
            else if (DateTime.TryParseExact(input, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out t))
                return t;
            return null;
        }
    }
}
