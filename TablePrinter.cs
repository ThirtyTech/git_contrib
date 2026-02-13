using ConsoleTables;
using git_contrib.Models;
using Spectre.Console;

namespace git_contrib;

public static class TablePrinter
{
    public static void PrintTableTotalsSelector(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null, DayOfWeek[]? hideDays = null)
    {
        if (Console.IsOutputRedirected)
        {
            PrintTableTotalsPiped(totals, hideSummary, reverse, limit, hideDays);
        }
        else
        {
            PrintTableTotals(totals, hideSummary, reverse, limit, hideDays);
        }
    }
    private static void PrintTableTotalsPiped(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null, DayOfWeek[]? hideDays = null)
    {
        var table = new ConsoleTable("Author", "Commits", "Files", "Lines");
        table.Options.EnableCount = false;

        int totalCommits = 0, totalFiles = 0, totalLines = 0;
        IEnumerable<AuthorData> sorted = totals.Values.OrderByDescending(x => x.TotalLines);
        if (reverse)
        {
            sorted = sorted.Reverse();
        }
        if (limit.HasValue && !reverse)
        {
            sorted = sorted.Take(limit.Value);
        }
        if (limit.HasValue && reverse)
        {
            sorted = sorted.TakeLast(limit.Value);
        }
        foreach (var authorData in sorted)
        {
            var authorTotalCommits = authorData.TotalCommits;
            var authorTotalFiles = authorData.UniqueFiles;
            var authorTotalLines = authorData.TotalLines;
            var authorAsterisk = AuthorHasHiddenDayData(authorData, hideDays) ? "*" : "";

            table.AddRow(authorData.Name, authorTotalCommits.ToString("N0") + authorAsterisk, authorTotalFiles.ToString("N0") + authorAsterisk, authorTotalLines.ToString("N0") + authorAsterisk);

            if (!hideSummary)
            {
                totalCommits += authorTotalCommits;
                totalFiles += authorTotalFiles;
                totalLines += authorTotalLines;
            }
        }

        if (!hideSummary)
        {
            var asterisk = HasHiddenDayData(totals, hideDays) ? "*" : "";
            table.AddRow("Summary Totals", totalCommits.ToString("N0") + asterisk, totalFiles.ToString("N0") + asterisk, totalLines.ToString("N0") + asterisk);
        }

        table.Write();
    }


    private static void PrintTableTotals(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null, DayOfWeek[]? hideDays = null)
    {
        var summaryAsterisk = HasHiddenDayData(totals, hideDays) ? "*" : "";
        var table = new Table
        {
            ShowFooters = !hideSummary
        };
        table.Title("Totals By Author");
        table.Border(TableBorder.Rounded);
        table.BorderStyle = Style.Parse("red");
        table.AddColumn(new TableColumn("Author").Footer("Summary Totals"));
        table.AddColumn(new TableColumn("Commits").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.TotalCommits).ToString("N0") + summaryAsterisk
        ));
        table.AddColumn(new TableColumn("Files").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.UniqueFiles).ToString("N0") + summaryAsterisk
        ));
        table.AddColumn(new TableColumn("Lines").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.TotalLines).ToString("N0") + summaryAsterisk
        ));

        IEnumerable<AuthorData> sorted = totals.Values.OrderByDescending(x => x.TotalLines);
        if (reverse)
        {
            sorted = sorted.Reverse();
        }
        if (limit.HasValue && !reverse)
        {
            sorted = sorted.Take(limit.Value);
        }
        if (limit.HasValue && reverse)
        {
            sorted = sorted.TakeLast(limit.Value);
        }
        foreach (var authorData in sorted)
        {
            var authorTotalCommits = authorData.TotalCommits;
            var authorTotalFiles = authorData.UniqueFiles;
            var authorTotalLines = authorData.TotalLines;
            var authorAsterisk = AuthorHasHiddenDayData(authorData, hideDays) ? "*" : "";

            table.AddRow(authorData.Name, authorTotalCommits.ToString("N0") + authorAsterisk, authorTotalFiles.ToString("N0") + authorAsterisk, authorTotalLines.ToString("N0") + authorAsterisk);
        }

        AnsiConsole.Write(table);
    }

    public static void PrintTableByDaySelector(Metric byDay, bool flipped, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false, DayOfWeek[]? hideDays = null)
    {
        if (flipped)
        {
            PrintTableByDayFlipped(byDay, totals, fromDate, toDate, hideSummary, reverse, hideDays);
        }
        else
        {
            if (Console.IsOutputRedirected)
            {
                PrintTableByDayPiped(byDay, totals, fromDate, toDate, hideSummary, reverse, hideDays);
            }
            else
            {
                PrintTableByDay(byDay, totals, fromDate, toDate, hideSummary, reverse, hideDays);
            }
        }

    }

    private static void PrintTableByDayPiped(Metric byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false, DayOfWeek[]? hideDays = null)
    {
        var table = new ConsoleTable("Author");
        table.Options.EnableCount = false;
        var days = (toDate - fromDate).Days;

        for (int i = 0; i <= days; i++)
        {
            var currentDate = fromDate.AddDays(i);
            if (hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek)) continue;
            table.AddColumn([currentDate.ToString("MM-dd")]);
        }
        table.AddColumn(["Total"]);

        IEnumerable<AuthorData> sorted = totals.Values.OrderByDescending(x => x.TotalLines);
        if (reverse)
        {
            sorted = sorted.Reverse();
        }

        foreach (var authorData in sorted)
        {
            List<string> row = new List<string> { authorData.Name };
            var runningTotal = 0;
            var authorHasHidden = false;
            for (int i = 0; i <= days; i++)
            {
                var currentDate = fromDate.AddDays(i);
                var date = currentDate.ToString("yyyy-MM-dd");

                int dayValue = 0;
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    dayValue = byDay switch
                    {
                        Models.Metric.Lines => authorData.ChangeMap[date].Lines,
                        Models.Metric.Files => authorData.ChangeMap[date].Files,
                        Models.Metric.Commits => authorData.ChangeMap[date].Commits,
                        _ => 0,
                    };
                }
                runningTotal += dayValue;

                if (hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek))
                {
                    if (dayValue > 0) authorHasHidden = true;
                    continue;
                }
                row.Add(dayValue == 0 ? "0" : dayValue.ToString("N0"));
            }
            row.Add(runningTotal.ToString("N0") + (authorHasHidden ? "*" : ""));
            table.AddRow(row.ToArray());
        }

        table.Configure(o => o.NumberAlignment = Alignment.Right);

        if (!hideSummary)
        {
            List<string> row = ["Summary"];
            var grandTotal = 0;
            for (int i = 0; i <= days; i++)
            {
                var currentDate = fromDate.AddDays(i);
                var date = currentDate.ToString("yyyy-MM-dd");
                bool isHidden = hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek);
                var total = totals.Values.Sum(x => x.ChangeMap.ContainsKey(date) ? byDay switch
                {
                    Models.Metric.Lines => x.ChangeMap[date].Lines,
                    Models.Metric.Files => x.ChangeMap[date].Files,
                    Models.Metric.Commits => x.ChangeMap[date].Commits,
                    _ => 0,
                } : 0);
                grandTotal += total;

                if (isHidden) continue;
                row.Add(total.ToString("N0"));
            }

            row.Add(grandTotal.ToString("N0") + (HasHiddenDayData(totals, hideDays) ? "*" : ""));
            table.AddRow(row.ToArray());
        }
        table.Write();
    }


    public static void PrintTableByDay(Metric byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false, DayOfWeek[]? hideDays = null)
    {
        var table = new Table();
        table.Title($"{byDay} by Author");
        table.Border(!Console.IsOutputRedirected ? TableBorder.Rounded : TableBorder.Ascii);
        table.BorderStyle = Style.Parse("red");
        table.AddColumn(new TableColumn("Author").NoWrap().Footer("Summary"));
        table.ShowFooters = !hideSummary;
        var days = (toDate - fromDate).Days;
        var summaryAsterisk = HasHiddenDayData(totals, hideDays) ? "*" : "";

        for (int i = 0; i <= days; i++)
        {
            var currentDate = fromDate.AddDays(i);
            if (hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek)) continue;
            var date = currentDate.ToString("MM-dd");
            var dayOfWeek = currentDate.DayOfWeek.ToString().Substring(0, 2);
            var dateStr = currentDate.ToString("yyyy-MM-dd");
            table.AddColumn(new TableColumn($"{date}\n{dayOfWeek}").Alignment(Justify.Right).Footer(
                totals.Values.Sum(x => x.ChangeMap.ContainsKey(dateStr) ? byDay switch
                {
                    Models.Metric.Lines => x.ChangeMap[dateStr].Lines,
                    Models.Metric.Files => x.ChangeMap[dateStr].Files,
                    Models.Metric.Commits => x.ChangeMap[dateStr].Commits,
                    _ => 0,
                } : 0).ToString("N0")
            ));
        }
        table.AddColumn(new TableColumn("Total").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => byDay switch
            {
                Models.Metric.Lines => x.TotalLines,
                Models.Metric.Files => x.ChangeMap.Sum(x => x.Value.Files),
                Models.Metric.Commits => x.TotalCommits,
                _ => 0,
            }).ToString("N0") + summaryAsterisk
        ));

        IEnumerable<AuthorData> sorted = totals.Values.OrderByDescending(x => byDay switch
        {
            Models.Metric.Lines => x.TotalLines,
            Models.Metric.Files => x.UniqueFiles,
            Models.Metric.Commits => x.TotalCommits,
            _ => x.TotalLines,
        });
        if (reverse)
        {
            sorted = sorted.Reverse();
        }
        foreach (var authorData in sorted)
        {
            List<string> row = [authorData.Name];
            var runningTotal = 0;
            var authorHasHidden = false;
            for (int i = 0; i <= days; i++)
            {
                var currentDate = fromDate.AddDays(i);
                var date = currentDate.ToString("yyyy-MM-dd");
                bool isHidden = hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek);

                int dayValue = 0;
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    dayValue = byDay switch
                    {
                        Models.Metric.Lines => authorData.ChangeMap[date].Lines,
                        Models.Metric.Files => authorData.ChangeMap[date].Files,
                        Models.Metric.Commits => authorData.ChangeMap[date].Commits,
                        _ => 0,
                    };
                }
                runningTotal += dayValue;

                if (isHidden)
                {
                    if (dayValue > 0) authorHasHidden = true;
                    continue;
                }
                row.Add(dayValue == 0 ? "0" : dayValue.ToString("N0"));
            }
            row.Add(runningTotal.ToString("N0") + (authorHasHidden ? "*" : ""));
            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }

    public static void PrintTableByDayFlipped(Metric byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false, DayOfWeek[]? hideDays = null)
    {
        var days = (toDate - fromDate).Days;
        var table = new Table();
        table.Title($"{byDay} by Author");
        table.Border(TableBorder.Rounded);
        table.BorderStyle = Style.Parse("red");
        table.AddColumn(new TableColumn("Date").Footer("Summary"));
        table.ShowFooters = !hideSummary;

        var grandTotal = 0;
        foreach (var authorData in totals.Values)
        {
            var authorAsterisk = AuthorHasHiddenDayData(authorData, hideDays) ? "*" : "";
            var column = new TableColumn(authorData.Name).Alignment(Justify.Right);
            column.Footer(authorData.ChangeMap.Sum(x =>
            {
                return byDay switch
                {
                    Models.Metric.Lines => x.Value.Lines,
                    Models.Metric.Files => x.Value.Files,
                    Models.Metric.Commits => x.Value.Commits,
                    _ => 0,
                };
            }).ToString("N0") + authorAsterisk);
            table.AddColumn(column);
        }

        table.AddColumn("Total", configure: x => x.Alignment = Justify.Right);

        foreach (int i in reverse ? Enumerable.Range(0, days + 1).Reverse() : Enumerable.Range(0, days + 1))
        {
            var currentDate = fromDate.AddDays(i);
            if (hideDays is { Length: > 0 } && hideDays.Contains(currentDate.DayOfWeek)) continue;
            var date = currentDate.ToString("MM-dd");
            var dayOfWeek = currentDate.DayOfWeek.ToString().Substring(0, 2);
            List<string> row = [$"{date} {dayOfWeek}"];
            var runningTotal = 0;
            foreach (var authorData in totals.Values)
            {
                var authorDate = currentDate.ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(authorDate))
                {
                    var total = byDay switch
                    {
                        Models.Metric.Lines => authorData.ChangeMap[authorDate].Lines,
                        Models.Metric.Files => authorData.ChangeMap[authorDate].Files,
                        Models.Metric.Commits => authorData.ChangeMap[authorDate].Commits,
                        _ => 0,
                    };
                    runningTotal += total;
                    row.Add(total.ToString("N0"));
                }
                else
                {
                    row.Add("0");
                }
            }
            grandTotal += runningTotal;
            row.Add(runningTotal.ToString("N0"));
            table.AddRow(row.ToArray());
        }

        // For Hide mode, grandTotal from visible rows doesn't include hidden day data
        if (hideDays is { Length: > 0 })
        {
            grandTotal = totals.Values.Sum(a => a.ChangeMap.Sum(x => byDay switch
            {
                Models.Metric.Lines => x.Value.Lines,
                Models.Metric.Files => x.Value.Files,
                Models.Metric.Commits => x.Value.Commits,
                _ => 0,
            }));
        }

        table.Columns.Last().Footer(grandTotal.ToString("N0") + (HasHiddenDayData(totals, hideDays) ? "*" : ""));

        AnsiConsole.Write(table);
    }

    private static bool IsSkippedDay(DateTimeOffset date, DayOfWeek[]? days) =>
        days is { Length: > 0 } && days.Contains(date.DayOfWeek);

    private static bool HasHiddenDayData(Dictionary<string, AuthorData> totals, DayOfWeek[]? hideDays) =>
        hideDays is { Length: > 0 } && totals.Values.Any(a => AuthorHasHiddenDayData(a, hideDays));

    private static bool AuthorHasHiddenDayData(AuthorData author, DayOfWeek[]? hideDays) =>
        hideDays is { Length: > 0 } && author.ChangeMap.Any(kvp =>
            DateTime.TryParse(kvp.Key, out var date) && hideDays.Contains(date.DayOfWeek) &&
            (kvp.Value.Lines > 0 || kvp.Value.Commits > 0 || kvp.Value.Files > 0));
}
