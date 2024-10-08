﻿namespace OrcaHello.Web.Shared.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class DateTimeExtensions
    {
        public static string UTCToPDT(this DateTime datetime, bool timeOnly = false)
        {
            TimeZoneInfo pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            datetime = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
            DateTime pstTime = TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.Utc, pst);
            var zoneString = pst.IsDaylightSavingTime(pstTime) ? "PDT" : "PST";
            var format = timeOnly ? $"HH:mm:ss" : $"dd MMM HH:mm:ss '{zoneString}'";
            return $"{pstTime.ToString(format)}";
        }

        public static string UTCToPDTFull(this DateTime datetime)
        {
            TimeZoneInfo pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            datetime = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
            DateTime pstTime = TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.Utc, pst);
            var zoneString = pst.IsDaylightSavingTime(pstTime) ? "PDT" : "PST";
            var format = $"dd MMM yyyy HH:mm:ss '{zoneString}'";
            return $"{pstTime.ToString(format)}";
        }

        public static string UTCToPDTCompact(this DateTime datetime)
        {
            TimeZoneInfo pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            datetime = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
            DateTime pstTime = TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.Utc, pst);
            var zoneString = pst.IsDaylightSavingTime(pstTime) ? "PDT" : "PST";
            var format = $"dd MMM yy HH:mm{zoneString}";
            return $"{pstTime.ToString(format)}";
        }
    }
}
