using System.Text;
using ConsoleTables;
using Spectre.Console;

public static class TablePrinter
{
    public static void PrintTableTotalsSelector(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null)
    {
        if (Console.IsOutputRedirected)
        {
            PrintTableTotalsPiped(totals, hideSummary, reverse, limit);
        }
        else
        {
            PrintTableTotals(totals, hideSummary, reverse, limit);
        }
    }
    private static void PrintTableTotalsPiped(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null)
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

            table.AddRow(authorData.Name, authorTotalCommits.ToString("N0"), authorTotalFiles.ToString("N0"), authorTotalLines.ToString("N0"));

            if (!hideSummary)
            {
                totalCommits += authorTotalCommits;
                totalFiles += authorTotalFiles;
                totalLines += authorTotalLines;
            }
        }

        if (!hideSummary)
        {
            table.AddRow("Summary Totals", totalCommits.ToString("N0"), totalFiles.ToString("N0"), totalLines.ToString("N0"));
        }

        table.Write();
    }


    private static void PrintTableTotals(Dictionary<string, AuthorData> totals, bool hideSummary = false, bool reverse = false, int? limit = null)
    {
        var table = new Table
        {
            ShowFooters = !hideSummary
        };
        table.Title("Totals By Author");
        table.Border(TableBorder.Rounded);
        table.BorderStyle = Style.Parse("red");
        table.AddColumn(new TableColumn("Author").Footer("Summary Totals"));
        table.AddColumn(new TableColumn("Commits").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.TotalCommits).ToString("N0")
        ));
        table.AddColumn(new TableColumn("Files").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.UniqueFiles).ToString("N0")
        ));
        table.AddColumn(new TableColumn("Lines").Alignment(Justify.Right).Footer(
            totals.Values.Take(limit ?? int.MaxValue).Sum(x => x.TotalLines).ToString("N0")
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

            table.AddRow(authorData.Name, authorTotalCommits.ToString("N0"), authorTotalFiles.ToString("N0"), authorTotalLines.ToString("N0"));
        }

        AnsiConsole.Write(table);
    }

    public static void PrintTableByDaySelector(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false)
    {
        if (byDay.ToString().Contains("Flipped"))
        {
            PrintTableByDayFlipped(byDay, totals, fromDate, toDate, hideSummary, reverse);
        }
        else
        {
            if (Console.IsOutputRedirected)
            {
                PrintTableByDayPiped(byDay, totals, fromDate, toDate, hideSummary, reverse);
            }
            else
            {
                PrintTableByDay(byDay, totals, fromDate, toDate, hideSummary, reverse);
            }
        }

    }

    private static void PrintTableByDayPiped(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false)
    {
        var table = new ConsoleTable("Author");
        table.Options.EnableCount = false;
        var days = (toDate - fromDate).Days;

        for (int i = 0; i <= days; i++)
        {
            table.AddColumn([fromDate.AddDays(i).ToString("MM-dd")]);
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
            for (int i = 0; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    var total = byDay switch
                    {
                        global::ByDay.Lines => authorData.ChangeMap[date].Lines,
                        global::ByDay.Files => authorData.ChangeMap[date].Files,
                        global::ByDay.Commits => authorData.ChangeMap[date].Commits,
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
            row.Add(runningTotal.ToString("N0"));
            table.AddRow(row.ToArray());
        }

        table.Configure(o => o.NumberAlignment = Alignment.Right);

        if (!hideSummary)
        {
            List<string> row = new List<string> { "Summary" };
            var grandTotal = 0;
            for (int i = 0; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                var total = totals.Values.Sum(x => x.ChangeMap.ContainsKey(date) ? byDay switch
                {
                    global::ByDay.Lines => x.ChangeMap[date].Lines,
                    global::ByDay.Files => x.ChangeMap[date].Files,
                    global::ByDay.Commits => x.ChangeMap[date].Commits,
                    _ => 0,
                } : 0);
                grandTotal += total;
                row.Add(total.ToString("N0"));
            }

            row.Add(grandTotal.ToString("N0"));
            table.AddRow(row.ToArray());
        }
        table.Write();
    }


    public static void PrintTableByDay(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false)
    {
        var table = new Table();
        table.Title($"{byDay} by Author");
        table.Border(!Console.IsOutputRedirected ? TableBorder.Rounded : TableBorder.Ascii);
        table.BorderStyle = Style.Parse("red");
        table.AddColumn(new TableColumn("Author").NoWrap().Footer("Summary"));
        table.ShowFooters = !hideSummary;
        var days = (toDate - fromDate).Days;
        for (int i = 0; i <= days; i++)
        {
            var date = fromDate.AddDays(i).ToString("MM-dd");
            var dayOfWeek = fromDate.AddDays(i).DayOfWeek.ToString().Substring(0, 2);
            table.AddColumn(new TableColumn($"{date}\n{dayOfWeek}").Alignment(Justify.Right).Footer(
                totals.Values.Sum(x => x.ChangeMap.ContainsKey(fromDate.AddDays(i).ToString("yyyy-MM-dd")) ? byDay switch
                {
                    global::ByDay.Lines => x.ChangeMap[fromDate.AddDays(i).ToString("yyyy-MM-dd")].Lines,
                    global::ByDay.Files => x.ChangeMap[fromDate.AddDays(i).ToString("yyyy-MM-dd")].Files,
                    global::ByDay.Commits => x.ChangeMap[fromDate.AddDays(i).ToString("yyyy-MM-dd")].Commits,
                    _ => 0,
                } : 0).ToString("N0")
            ));
        }
        table.AddColumn(new TableColumn("Total").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => byDay switch
            {
                global::ByDay.Lines => x.TotalLines,
                global::ByDay.Files => x.ChangeMap.Sum(x => x.Value.Files),
                global::ByDay.Commits => x.TotalCommits,
                _ => 0,
            }).ToString("N0")
        ));

        IEnumerable<AuthorData> sorted = totals.Values.OrderByDescending(x => byDay switch
        {
            global::ByDay.Lines => x.TotalLines,
            global::ByDay.Files => x.UniqueFiles,
            global::ByDay.Commits => x.TotalCommits,
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
            for (int i = 0; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    var total = byDay switch
                    {
                        global::ByDay.Lines => authorData.ChangeMap[date].Lines,
                        global::ByDay.Files => authorData.ChangeMap[date].Files,
                        global::ByDay.Commits => authorData.ChangeMap[date].Commits,
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
            row.Add(runningTotal.ToString("N0"));
            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }


    public static void PrintTableByDayFlipped(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, DateTimeOffset toDate, bool hideSummary = false, bool reverse = false)
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
            var column = new TableColumn(authorData.Name).Alignment(Justify.Right);
            column.Footer(authorData.ChangeMap.Sum(x =>
            {
                return byDay switch
                {
                    global::ByDay.LinesFlipped => x.Value.Lines,
                    global::ByDay.FilesFlipped => x.Value.Files,
                    global::ByDay.CommitsFlipped => x.Value.Commits,
                    _ => 0,
                };
            }).ToString("N0"));
            table.AddColumn(column);
        }

        table.AddColumn("Total", configure: x => x.Alignment = Justify.Right);

        foreach (int i in reverse ? Enumerable.Range(0, days + 1).Reverse() : Enumerable.Range(0, days + 1))
        {
            var date = fromDate.AddDays(i).ToString("MM-dd");
            var dayOfWeek = fromDate.AddDays(i).DayOfWeek.ToString().Substring(0, 2);
            List<string> row = [$"{date} {dayOfWeek}"];
            var runningTotal = 0;
            foreach (var authorData in totals.Values)
            {
                var authorDate = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(authorDate))
                {
                    var total = byDay switch
                    {
                        global::ByDay.LinesFlipped => authorData.ChangeMap[authorDate].Lines,
                        global::ByDay.FilesFlipped => authorData.ChangeMap[authorDate].Files,
                        global::ByDay.CommitsFlipped => authorData.ChangeMap[authorDate].Commits,
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

        table.Columns.Last().Footer(grandTotal.ToString("N0"));

        AnsiConsole.Write(table);
    }
}
