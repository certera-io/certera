using System;

namespace Certera.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToFriendlyString(this DateTime d)
        {
            // 1.
            // Get time span elapsed since the date.
            TimeSpan s = DateTime.Now.Subtract(d);

            // 2.
            // Get total number of days elapsed.
            int dayDiff = (int)s.TotalDays;

            // 3.
            // Get total number of seconds elapsed.
            int secDiff = (int)s.TotalSeconds;

            // 4.
            // Don't allow out of range values.
            if (dayDiff < 0 || dayDiff >= 31)
            {
                return null;
            }

            // 5.
            // Handle same-day times.
            if (dayDiff == 0)
            {
                // A.
                // Less than one minute ago.
                if (secDiff < 60)
                {
                    return "just now";
                }
                // B.
                // Less than 2 minutes ago.
                if (secDiff < 120)
                {
                    return "1m ago";
                }
                // C.
                // Less than one hour ago.
                if (secDiff < 3600)
                {
                    return string.Format("{0}m ago",
                        Math.Floor((double)secDiff / 60));
                }
                // D.
                // Less than 2 hours ago.
                if (secDiff < 7200)
                {
                    return "1h ago";
                }
                // E.
                // Less than one day ago.
                if (secDiff < 86400)
                {
                    return string.Format("{0}h ago",
                        Math.Floor((double)secDiff / 3600));
                }
            }
            // 6.
            // Handle previous days.
            if (dayDiff == 1)
            {
                return "1d ago";
            }
            if (dayDiff < 31)
            {
                return string.Format("{0}d ago", dayDiff);
            }
            if (dayDiff < 365)
            {
                int months = (int)Math.Floor((double)dayDiff / 30);
                return months <= 1 ? "1mth ago" : months + "mths ago";
            }
            int years = (int)Math.Floor((double)dayDiff / 365);
            return years <= 1 ? "1yr ago" : years + "yrs ago";
        }
    }
}
