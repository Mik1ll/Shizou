namespace Shizou.Data.Extensions;

public static class TimeSpanExtensions
{
    public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
    {
        var format = "G" + significantDigits;
        return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
            : span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
            : span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
            : span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
            : span.TotalDays.ToString(format) + " days";
    }
}
