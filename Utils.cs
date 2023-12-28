using System.Globalization;
using CliWrap;
using CliWrap.Buffered;

public static class Utils
{
    public static async Task<bool> IsGitDirectoryAsync(string path)
    {
        string gitPath = System.IO.Path.Combine(path, ".git");

        // Check if .git directory exists
        if (Directory.Exists(gitPath))
        {
            return true;
        }

        try
        {
            // Running 'git status' command
            var result = await Cli.Wrap("git")
                .WithArguments(new[] { "-C", path, "status" })
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteBufferedAsync();

            // Process successful
            return true;
        }
        catch
        {
            // Handle errors (e.g., command failed to execute)
            return false;
        }
    }

    public static string FindNearestGitDirectory(string startPath)
    {
        DirectoryInfo? dir = new DirectoryInfo(startPath);

        while (dir != null)
        {
            if (Directory.Exists(System.IO.Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;  // Return the directory containing the .git directory
            }
            dir = dir.Parent;
        }

        return string.Empty;
    }
    public static DateTime FindLastMonday()
    {
        var today = DateTime.Today;

        int daysSinceMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;

        if (daysSinceMonday < 0)
        {
            // If today is before Monday in the current week, go back to the previous week
            daysSinceMonday += 7;
        }

        return today.AddDays(-daysSinceMonday);
    }

    public static bool TryParseHumanReadableDateTimeOffset(string input, out DateTimeOffset date)
    {
        date = DateTimeOffset.MinValue;
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var now = DateTime.Now;
        var startOfDay = now.Date;

        date = parts[0].ToLower() switch
        {
            "week" => startOfDay.AddDays(-6),
            "workweek" or "work" => FindLastMonday(),
            _ => DateTimeOffset.MinValue
        };

        if (date != DateTimeOffset.MinValue)
        {
            return true;
        }

        if (input.Contains("-"))
        {
            if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _date))
            {
                date = _date;
                return true;
            }
            if (DateTime.TryParseExact(input, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _date))
            {
                date = _date;
                return true;
            }
            return false;
        }

        if (int.TryParse(parts[0], out int quantity))
        {
            date = parts[1].ToLower() switch
            {
                "second" or "seconds" => startOfDay.AddSeconds(-quantity),
                "minute" or "minutes" => startOfDay.AddMinutes(-quantity),
                "hour" or "hours" => startOfDay.AddHours(-quantity),
                "day" or "days" => startOfDay.AddDays(-quantity),
                "week" or "weeks" => startOfDay.AddDays((-quantity) * 7).AddDays(1),
                "month" or "months" => startOfDay.AddMonths(-quantity).AddDays(1),
                "year" or "years" => startOfDay.AddYears(-quantity).AddDays(1),
                _ => DateTimeOffset.MinValue
            };
            if (date != DateTimeOffset.MinValue)
            {
                return true;
            }
        }

        return false;
    }
}
