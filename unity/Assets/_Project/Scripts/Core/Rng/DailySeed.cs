using System;
using System.Security.Cryptography;
using System.Text;

namespace FlyingFox.Core
{
    /// <summary>
    /// Daily seed: SHA-256(UTF-8 "FlyingFoxDaily|yyyy-MM-dd") → first 4 bytes little-endian int32.
    /// Date is UTC calendar day only.
    /// </summary>
    public static class DailySeed
    {
        public const string Prefix = "FlyingFoxDaily|";

        public static int FromUtcDate(DateTime utcDate)
        {
            var d = utcDate.Kind == DateTimeKind.Utc
                ? utcDate.Date
                : utcDate.ToUniversalTime().Date;
            string key = Prefix + d.ToString("yyyy-MM-dd");
            return FromKey(key);
        }

        public static int FromKey(string key)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            byte[] hash;
            using (var sha = SHA256.Create())
                hash = sha.ComputeHash(bytes);
            // First 4 bytes little-endian int32 (BitConverter is LE on Steam targets)
            return BitConverter.ToInt32(hash, 0);
        }

        public static int TodayUtc() => FromUtcDate(DateTime.UtcNow);
    }
}
