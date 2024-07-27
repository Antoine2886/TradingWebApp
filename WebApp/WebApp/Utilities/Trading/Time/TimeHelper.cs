namespace WebApp.Utilities.Trading.Time
{
    /// <summary>
    /// Author: Antoine Bélanger
    /// Description: Provides utility methods for handling and manipulating time related to trading operations.
    /// (in the database time is New-York) but I try to use utc otherwise. (not my best code ngl)
    /// </summary>

    public static class TimeHelper
    {

        /// <summary>
        /// Gets the current Eastern Time.
        /// </summary>
        /// <returns>The current Eastern Time.</returns>
        public static DateTime GetEasternTime()
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime utcNow = DateTime.UtcNow;
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, easternZone);
            return easternTime;
        }

        /// <summary>
        /// Floors the given time to the nearest minute.
        /// </summary>
        /// <param name="time">The time to floor.</param>
        /// <returns>The floored time to the nearest minute.</returns>
        public static DateTime FloorToNearestMinute(DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        }

        /// <summary>
        /// Floors the given time to the nearest specified interval.
        /// </summary>
        /// <param name="time">The time to floor.</param>
        /// <param name="interval">The interval to floor to.</param>
        /// <returns>The floored time to the nearest interval.</returns>
        public static DateTime FloorToNearestInterval(DateTime time, string interval)
        {
            return interval switch
            {
                "1min" => FloorToNearestMinute(time),
                "3min" => FloorToNearestThreeMinutes(time),
                "5min" => FloorToNearestFiveMinutes(time),
                "15min" => FloorToNearestFifteenMinutes(time),
                "30min" => FloorToNearestThirtyMinutes(time),
                "45min" => FloorToNearestFortyFiveMinutes(time),
                "1h" => FloorToNearestHour(time),
                "2h" => FloorToNearestTwoHours(time),
                "3h" => FloorToNearestThreeHours(time),
                "4h" => FloorToNearestFourHours(time),
                "8h" => FloorToNearestEightHours(time),
                "1D" => FloorToNearestDay(time),
                "1W" => FloorToNearestWeek(time),
                "1M" => FloorToNearestMonth(time),
                _ => throw new ArgumentException("Unsupported interval")
            };
        }

        /// <summary>
        /// Floors the given time to the nearest 5 minutes.
        /// </summary>
        /// <param name="time">The time to floor.</param>
        /// <returns>The floored time to the nearest 5 minutes.</returns>
        public static DateTime FloorToNearestFiveMinutes(DateTime time)
        {
            int minutes = (time.Minute / 5) * 5;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }

        /// <summary>
        /// Floors the given time to the nearest 15 minutes. ETC....
        /// </summary>
        /// <param name="time">The time to floor.</param>
        /// <returns>The floored time to the nearest 15 minutes.</returns>
        public static DateTime FloorToNearestFifteenMinutes(DateTime time)
        {
            int minutes = (time.Minute / 15) * 15;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }

        public static DateTime FloorToNearestThreeMinutes(DateTime time)
        {
            int minutes = (time.Minute / 3) * 3;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }

        public static DateTime FloorToNearestThirtyMinutes(DateTime time)
        {
            int minutes = (time.Minute / 30) * 30;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }

        public static DateTime FloorToNearestFortyFiveMinutes(DateTime time)
        {
            int minutes = (time.Minute / 45) * 45;
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, minutes, 0);
        }

        public static DateTime FloorToNearestHour(DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0);
        }

        public static DateTime FloorToNearestTwoHours(DateTime time)
        {
            int hours = (time.Hour / 2) * 2;
            return new DateTime(time.Year, time.Month, time.Day, hours, 0, 0);
        }

        public static DateTime FloorToNearestThreeHours(DateTime time)
        {
            int hours = (time.Hour / 3) * 3;
            return new DateTime(time.Year, time.Month, time.Day, hours, 0, 0);
        }

        public static DateTime FloorToNearestFourHours(DateTime time)
        {
            int hours = (time.Hour / 4) * 4;
            return new DateTime(time.Year, time.Month, time.Day, hours, 0, 0);
        }

        public static DateTime FloorToNearestEightHours(DateTime time)
        {
            // Adjust to UTC-4
            var adjustedTime = time.AddHours(-4);
            int hours = (adjustedTime.Hour / 8) * 8;
            var flooredTime = new DateTime(adjustedTime.Year, adjustedTime.Month, adjustedTime.Day, hours, 0, 0);

            // Convert back to the original time zone
            return flooredTime.AddHours(4);
        }


        public static DateTime FloorToNearestDay(DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, 0, 0, 0);
        }

        public static DateTime FloorToNearestWeek(DateTime time)
        {
            int daysToSubtract = (int)time.DayOfWeek;
            DateTime flooredWeek = time.AddDays(-daysToSubtract).Date;
            return new DateTime(flooredWeek.Year, flooredWeek.Month, flooredWeek.Day, 0, 0, 0);
        }

        public static DateTime FloorToNearestMonth(DateTime time)
        {
            return new DateTime(time.Year, time.Month, 1, 0, 0, 0);
        }


        /// <summary>
        /// Calculates the start date based on the given interval. (This isnt useful)
        /// </summary>
        /// <param name="interval">The interval to calculate the start date for.</param>
        /// <returns>The calculated start date.</returns>
        public static DateTime CalculateStartDate(string interval)
        {
            DateTime now = GetEasternTime();
            return interval switch
            {
                "1min" => now.AddMinutes(-3),
                "5min" => now.AddMinutes(-10),
                "15min" => now.AddMinutes(-30),
                "30min" => now.AddMinutes(-100),
                "1h" => now.AddHours(-3),
                "1day" => now.AddDays(-2),
                _ => now
            };
        }


        /// <summary>
        /// Gets the time until the next interval in milliseconds.
        /// </summary>
        /// <param name="intervalMinutes">The interval in minutes.</param>
        /// <returns>The time until the next interval in milliseconds.</returns>
        public static double GetTimeUntilNextInterval(int intervalMinutes)
        {
            DateTime now = GetEasternTime();
            int minutesPastHour = now.Minute % intervalMinutes;
            int minutesToNextInterval = intervalMinutes - minutesPastHour;
            DateTime nextInterval = now.AddMinutes(minutesToNextInterval)
                                       .AddSeconds(-now.Second)
                                       .AddMilliseconds(-now.Millisecond);

            if (nextInterval <= now)
            {
                nextInterval = nextInterval.AddMinutes(intervalMinutes);
            }

            return (nextInterval - now).TotalMilliseconds;
        }
    }

}
