using ConsoleTables;
using Spectre.Console;

public static class TablePrinter
{
    public static void PrintTableTotalsSelector(Dictionary<string, AuthorData> totals, bool hideSummary = false) {
        if (Console.IsOutputRedirected) {
            PrintTableTotalsPiped(totals, hideSummary);
        } else {
            PrintTableTotals(totals, hideSummary);
        }
    }
    private static void PrintTableTotalsPiped(Dictionary<string, AuthorData> totals, bool hideSummary = false)
    {
        var table = new ConsoleTable("Author", "Commits", "Files", "Lines");
        table.Options.EnableCount = false;

        int totalCommits = 0, totalFiles = 0, totalLines = 0;

        foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
        {
            var authorTotalCommits = authorData.ChangeMap.Values.Sum(c => c.Commits);
            var authorTotalFiles = authorData.ChangeMap.Values.Sum(c => c.Files);
            var authorTotalLines = authorData.ChangeMap.Values.Sum(c => c.Additions + c.Deletions);

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


    private static void PrintTableTotals(Dictionary<string, AuthorData> totals, bool hideSummary = false)
    {
        var table = new Table();
        table.ShowFooters = !hideSummary;
        table.Title("Totals By Author");
        table.Border(TableBorder.Rounded);
        table.BorderStyle = Style.Parse("red");
        // table.Width(Console.WindowWidth);
        table.AddColumn(new TableColumn("Author").Footer("Summary Totals"));
        table.AddColumn(new TableColumn("Commits").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => x.ChangeMap.Sum(y => y.Value.Commits)).ToString("N0")
        ));
        table.AddColumn(new TableColumn("Files").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => x.ChangeMap.Sum(y => y.Value.Files)).ToString("N0")
        ));
        table.AddColumn(new TableColumn("Lines").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)).ToString("N0")
        ));

        foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
        {
            var authorTotalCommits = authorData.ChangeMap.Values.Sum(c => c.Commits);
            var authorTotalFiles = authorData.ChangeMap.Values.Sum(c => c.Files);
            var authorTotalLines = authorData.ChangeMap.Values.Sum(c => c.Additions + c.Deletions);

            table.AddRow(authorData.Name, authorTotalCommits.ToString("N0"), authorTotalFiles.ToString("N0"), authorTotalLines.ToString("N0"));
        }

        AnsiConsole.Write(table);
    }

    public static void PrintTableByDaySelector(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, bool hideSummary = false)
    {
        if (byDay.ToString().Contains("Flipped"))
        {
            PrintTableByDayFlipped(byDay, totals, fromDate, hideSummary);
        }
        else
        {
            if (Console.IsOutputRedirected)
            {
                PrintTableByDayPiped(byDay, totals, fromDate, hideSummary);
            }
            else
            {
                PrintTableByDay(byDay, totals, fromDate, hideSummary);
            }
        }

    }

    private static void PrintTableByDayPiped(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, bool hideSummary = false)
    {
        var table = new ConsoleTable("Author");
        table.Options.EnableCount = false;
        var days = (DateTime.Now - fromDate).Days;

        // Add Date Columns
        for (int i = 1; i <= days; i++)
        {
            table.AddColumn([fromDate.AddDays(i).ToString("MM-dd")]);
        }
        table.AddColumn(["Total"]);

        // Adding rows
        foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
        {
            List<string> row = new List<string> { authorData.Name };
            var runningTotal = 0;
            for (int i = 1; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    var total = byDay switch
                    {
                        global::ByDay.Lines => authorData.ChangeMap[date].Additions + authorData.ChangeMap[date].Deletions,
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

        // Add Footer
        if (!hideSummary)
        {
            // Add summary row
            List<string> row = new List<string> { "Summary" };
            var grandTotal = 0;
            for (int i = 1; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                var total = totals.Values.Sum(x => x.ChangeMap.ContainsKey(date) ? byDay switch
                {
                    global::ByDay.Lines => x.ChangeMap[date].Additions + x.ChangeMap[date].Deletions,
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


    public static void PrintTableByDay(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, bool hideSummary = false)
    {

        var table = new Table();
        table.Title($"{byDay} Changed by Author");
        table.Border(!Console.IsOutputRedirected ? TableBorder.Rounded : TableBorder.Ascii);
        table.BorderStyle = Style.Parse("red");
        // table.Width(Console.WindowWidth);
        table.AddColumn(new TableColumn("Author").NoWrap().Footer("Summary"));
        table.ShowFooters = !hideSummary;
        var days = (DateTime.Now - fromDate).Days;
        for (int i = 1; i <= days; i++)
        {
            table.AddColumn(new TableColumn(fromDate.AddDays(i).ToString("MM-dd")).Alignment(Justify.Right).Footer(
                totals.Values.Sum(x => x.ChangeMap.ContainsKey(fromDate.AddDays(i).ToString("yyyy-MM-dd")) ? x.ChangeMap[fromDate.AddDays(i).ToString("yyyy-MM-dd")].Additions + x.ChangeMap[fromDate.AddDays(i).ToString("yyyy-MM-dd")].Deletions : 0).ToString("N0")
            ));
        }
        table.AddColumn(new TableColumn("Total").Alignment(Justify.Right).Footer(
            totals.Values.Sum(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)).ToString("N0")
        ));

        foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
        {
            List<string> row = [authorData.Name];
            var runningTotal = 0;
            for (int i = 1; i <= days; i++)
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    var total = byDay switch
                    {
                        global::ByDay.Lines => authorData.ChangeMap[date].Additions + authorData.ChangeMap[date].Deletions,
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


    public static void PrintTableByDayFlipped(ByDay byDay, Dictionary<string, AuthorData> totals, DateTimeOffset fromDate, bool hideSummary = false)
    {

        var days = (DateTime.Now - fromDate).Days;
        var table = new Table();
        table.Title($"{byDay} Changed by Author");
        table.Border(TableBorder.Rounded);
        table.BorderStyle = Style.Parse("red");
        // table.Width(Console.WindowWidth);
        table.AddColumn(new TableColumn("Date").Footer("Summary"));
        table.ShowFooters = !hideSummary;

        var grandTotal = 0;
        foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
        {
            var column = new TableColumn(authorData.Name).Alignment(Justify.Right);
            column.Footer(authorData.ChangeMap.Sum(x =>
            {
                return byDay switch
                {
                    global::ByDay.LinesFlipped => x.Value.Additions + x.Value.Deletions,
                    global::ByDay.FilesFlipped => x.Value.Files,
                    global::ByDay.CommitsFlipped => x.Value.Commits,
                    _ => 0,
                };
            }).ToString("N0"));
            table.AddColumn(column);
        }

        table.AddColumn("Total", configure: x => x.Alignment = Justify.Right);

        for (int i = 1; i <= days; i++)
        {
            List<string> row = [fromDate.AddDays(i).ToString("MM-dd")];
            var runningTotal = 0;
            foreach (var authorData in totals.Values.OrderByDescending(x => x.ChangeMap.Sum(y => y.Value.Additions + y.Value.Deletions)))
            {
                var date = fromDate.AddDays(i).ToString("yyyy-MM-dd");
                if (authorData.ChangeMap.ContainsKey(date))
                {
                    var total = byDay switch
                    {
                        global::ByDay.LinesFlipped => authorData.ChangeMap[date].Additions + authorData.ChangeMap[date].Deletions,
                        global::ByDay.FilesFlipped => authorData.ChangeMap[date].Files,
                        global::ByDay.CommitsFlipped => authorData.ChangeMap[date].Commits,
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
