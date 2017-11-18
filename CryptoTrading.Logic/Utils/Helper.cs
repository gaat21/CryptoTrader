using System;

namespace CryptoTrading.Logic.Utils
{
    public static class Helper
    {
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local)).TotalSeconds;
        }
    }
}
