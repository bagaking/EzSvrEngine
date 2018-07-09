using System;
using System.Numerics;

namespace EzSvrEngine.Extension {

    public static class DateTimeExtension {

        public static DateTime ToUtcDate(this DateTime time) {
            return time.ToUniversalTime().Date;
        }

        public static string ToTimeKey(this DateTime time, string prefix = "") {
            var time_key = prefix + "-" + time.ToString("yyyyMMddHHmmss");
            return time_key;
        }

        public static string ToHourKey(this DateTime time, string prefix = "") {
            var time_key = prefix + "-" + time.ToString("yyyyMMddHH");
            return time_key;
        }

        public static long UnixTimestampFromDateTime(this DateTime date) {
            long unixTimestamp = date.Ticks - new DateTime(1970, 1, 1).Ticks;
            unixTimestamp /= TimeSpan.TicksPerSecond;
            return unixTimestamp;
        }

        public static DateTime TimeFromUnixTimestamp(this int unixTimestamp) {
            DateTime unixYear = new DateTime(1970, 1, 1);
            long unixTimeStampInTicks = unixTimestamp * TimeSpan.TicksPerSecond;
            DateTime dtUnix = new DateTime(unixYear.Ticks + unixTimeStampInTicks);
            return dtUnix;
        }

        public static DateTime TimeFromUnixTimestamp(this BigInteger unixTimestamp) {
            return TimeFromUnixTimestamp((int)unixTimestamp);
        }

        public static bool IsSameDay(this DateTime date, DateTime that_date) {
            return (date.Year == that_date.Year && date.Month == that_date.Month && date.Day == that_date.Day);
        }
    }
}
