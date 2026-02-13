using System.Globalization;
using CliWrap;
using CliWrap.Buffered;

namespace git_contrib;

public static class Utils
{

    public static string? FindNearestGitContrib(string path)
    {
        var directory = new DirectoryInfo(path);
        string[] possibleExtensions = [".gitcontrib", ".gitcontrib.json", ".gitcontrib.yaml", ".gitcontrib.yml"];

        while (directory != null)
        {
            foreach (var extension in possibleExtensions)
            {
                var gitContribFile = new FileInfo(System.IO.Path.Combine(directory.FullName, extension));
                if (gitContribFile.Exists)
                {
                    return gitContribFile.FullName;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }
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
                .WithArguments(["-C", path, "status"])
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
        DirectoryInfo? dir = new(startPath);

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
        if (input.Contains('/'))
        {
            throw new Exception("Date format not supported. Please use the format: yyyy-MM-dd");
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

        if (input.Contains('-'))
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
            if (DateTime.TryParseExact(input, "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _date))
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

    private static readonly Dictionary<string, DayOfWeek> DayAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["su"] = DayOfWeek.Sunday,
        ["sun"] = DayOfWeek.Sunday,
        ["sunday"] = DayOfWeek.Sunday,
        ["mo"] = DayOfWeek.Monday,
        ["mon"] = DayOfWeek.Monday,
        ["monday"] = DayOfWeek.Monday,
        ["tu"] = DayOfWeek.Tuesday,
        ["tue"] = DayOfWeek.Tuesday,
        ["tues"] = DayOfWeek.Tuesday,
        ["tuesday"] = DayOfWeek.Tuesday,
        ["we"] = DayOfWeek.Wednesday,
        ["wed"] = DayOfWeek.Wednesday,
        ["wednesday"] = DayOfWeek.Wednesday,
        ["th"] = DayOfWeek.Thursday,
        ["thu"] = DayOfWeek.Thursday,
        ["thur"] = DayOfWeek.Thursday,
        ["thurs"] = DayOfWeek.Thursday,
        ["thursday"] = DayOfWeek.Thursday,
        ["fr"] = DayOfWeek.Friday,
        ["fri"] = DayOfWeek.Friday,
        ["friday"] = DayOfWeek.Friday,
        ["sa"] = DayOfWeek.Saturday,
        ["sat"] = DayOfWeek.Saturday,
        ["saturday"] = DayOfWeek.Saturday,
    };

    public static bool TryParseDayOfWeek(string input, out DayOfWeek day)
    {
        return DayAliases.TryGetValue(input.Trim(), out day);
    }

    public static DayOfWeek[] ParseDaysList(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var days = new List<DayOfWeek>();
        foreach (var part in parts)
        {
            if (TryParseDayOfWeek(part, out var day))
            {
                if (!days.Contains(day)) days.Add(day);
            }
            else
            {
                throw new ArgumentException($"Unknown day name: '{part}'. Use names like su, mon, tues, wednesday, etc.");
            }
        }
        return days.ToArray();
    }
}
