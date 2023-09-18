
public static class Utils
{
    public static bool TryParseHumanReadableDateTimeOffset(string input, out DateTimeOffset dateTimeOffset)
    {
        dateTimeOffset = default;

        if (string.IsNullOrWhiteSpace(input)) return false;

        var parts = input.Split(' ');
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], out var quantity)) return false;

        var now = DateTimeOffset.Now;

        switch (parts[1].ToLower())
        {
            case "second":
            case "seconds":
                dateTimeOffset = now.AddSeconds(-quantity);
                break;

            case "minute":
            case "minutes":
                dateTimeOffset = now.AddMinutes(-quantity);
                break;

            case "hour":
            case "hours":
                dateTimeOffset = now.AddHours(-quantity);
                break;

            case "day":
            case "days":
                dateTimeOffset = now.AddDays(-quantity).Date;
                break;

            case "week":
            case "weeks":
                dateTimeOffset = now.AddDays(-quantity * 7).Date;
                break;

            case "month":
            case "months":
                dateTimeOffset = now.AddMonths(-quantity).Date;
                break;

            default:
                return false;
        }

        return true;
    }
}
