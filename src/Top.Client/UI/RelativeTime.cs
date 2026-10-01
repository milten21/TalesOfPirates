using System;

namespace Top.Client.UI;

public static class RelativeTime
{
    private const int DaysInWeek = 7;

    private const int DaysInMonth = 30;

    public static string Describe(DateTime time, DateTime now)
    {
        int days = (now.ToLocalTime().Date - time.ToLocalTime().Date).Days;

        return days switch
        {
            <= 0 => "today",
            1 => "yesterday",
            < DaysInWeek => $"{days} days ago",
            < 2 * DaysInWeek => "last week",
            < DaysInMonth => $"{days / DaysInWeek} weeks ago",
            < 2 * DaysInMonth => "last month",
            _ => $"{days / DaysInMonth} months ago"
        };
    }
}
