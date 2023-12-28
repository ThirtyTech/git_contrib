using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

public static class Work
{
    // TODO: Make this externally configurable.
    public static string[] ExcludeExtensions = [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".tiff",
        ".tif",
        ".ico",
        ".jfif",
        ".webp",
        ".svg",
        ".heif",
        ".heic",
        ".raw",
        ".indd",
        ".ai",
        ".eps",
        ".pdf"
        ];


    public static Color[] Colors = [
            Color.Red,
            Color.Blue,
            Color.Yellow,
            Color.Green,
            Color.Orange1,
            Color.Purple,
            Color.Pink1,
            Color.Teal,
            Color.Turquoise2,
            Color.Lime,
            Color.SkyBlue1,
            Color.Navy,
            Color.Olive,
            Color.Maroon,
            Color.LightCoral,
            Color.Aqua,
            Color.Violet,
            Color.SandyBrown,
            Color.Magenta1
    ];


    public static readonly int MaxConcurrency = Environment.ProcessorCount - 1;

    public static async Task<IEnumerable<AuthorData>?> DoWork(Options options) =>
        Console.IsOutputRedirected ?
        await DoWork_Internal(options) :
        await AnsiConsole.Status().StartAsync("Processing git log...", async ctx =>
    {
        ctx.SpinnerStyle(Style.Parse("yellow"));
        ctx.Spinner(Spinner.Known.Dots);
        var results = await DoWork_Internal(options);
        if (results == null)
        {
            ctx.Status("No results");
        }
        else
        {
            ctx.Status("Done");
        }
        return results;
    });

    public static async Task<IEnumerable<AuthorData>?> DoWork_Internal(Options options)
    {

        if (options.Format == global::Format.Table)
        {
            Console.WriteLine("Processing directory: " + options.Path);
        }

        if (!await Utils.IsGitDirectoryAsync(options.Path))
        {
            Console.WriteLine("Not a git directory: " + options.Path);
            return null;
        }
        int maxDates = (int)Math.Round((options.ToDate - options.FromDate).TotalDays);

        var dates = Enumerable.Range(0, maxDates)
                              .Select(i => options.FromDate.AddDays(i).ToString("yyyy-MM-dd"))
                              .ToList();

        var dateMap = dates.ToHashSet();

        await Cli.Wrap("git")
            .WithValidation(CommandResultValidation.None)
            .WithArguments(new[] { "fetch", "--all" })
            .WithWorkingDirectory(options.Path)
            .ExecuteBufferedAsync();

        var buffer = new MemoryStream();

        string gitCmd = "git";
        var gitArgs = new List<string> { "--no-pager", "log", "--branches", "--remotes", "--summary", "--numstat", "--mailmap", "--no-merges", "--format=^%h|%aI|%aN|<%aE>" };
        if (options.FromDate != DateTime.MinValue)
        {
            gitArgs.Add("--since");
            gitArgs.Add(options.FromDate.ToString("yyyy-MM-dd"));
        }

        var gitCmdResult = await Cli.Wrap(gitCmd)
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .WithStandardOutputPipe(PipeTarget.ToStream(buffer))
            .WithArguments(gitArgs)
            .WithWorkingDirectory(options.Path)
            .ExecuteBufferedAsync();

        bool skipUntilNextCommit = false;
        var totals = new Dictionary<string, AuthorData>();
        var commitMap = new HashSet<string>();
        string? currentDate = null;
        AuthorData? currentAuthor = null;

        buffer.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(buffer))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("^"))
                {
                    var parts = line.Split('|');
                    var commit = parts[0][1..];
                    var date = parts[1];
                    var author = parts[2].Replace("[", "{").Replace("]", "}");
                    var email = parts[3].Trim('<', '>');
                    var authorDateParseError = DateTimeOffset.TryParse(date, out var authorDate);
                    if (!authorDateParseError)
                    {
                        throw new Exception($"Invalid date format in line: {line}");
                    }
                    var authorDateStr = authorDate.ToString("yyyy-MM-dd");
                    if (!dateMap.Contains(authorDateStr))
                    {
                        skipUntilNextCommit = true;
                        continue;
                    }
                    if (options.IgnoreAuthors.Any(author.Contains))
                    {
                        skipUntilNextCommit = true;
                        continue;
                    }
                    if (commitMap.Contains(commit))
                    {
                        skipUntilNextCommit = true;
                        continue;
                    }

                    skipUntilNextCommit = false;
                    commitMap.Add(commit);
                    currentDate = authorDateStr;

                    if (!totals.ContainsKey(author))
                    {
                        totals[author] = new AuthorData
                        {
                            Name = author,
                            Email = email,
                            ChangeMap = new Dictionary<string, ChangeSet>() {
                                { authorDateStr, new ChangeSet {
                                    Commits = 1
                                }}
                            }
                        };
                    }
                    else
                    {
                        if (totals[author].ChangeMap.ContainsKey(authorDateStr))
                        {
                            totals[author].ChangeMap[authorDateStr].Commits++;
                        }
                        else
                        {
                            totals[author].ChangeMap[authorDateStr] = new ChangeSet
                            {
                                Commits = 1
                            };
                        }
                    }

                    currentAuthor = totals[author];
                }
                else if (char.IsDigit(line[0]) && currentAuthor != null && currentDate != null && !skipUntilNextCommit)
                {
                    var parts = line.Split("\t", StringSplitOptions.RemoveEmptyEntries);
                    if (options.IgnoreFiles.Any(parts[2].Contains) || ExcludeExtensions.Any(parts[2].EndsWith))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[0], out var additions) || !int.TryParse(parts[1], out var deletions))
                    {
                        throw new Exception($"Invalid number format in line: {line}");
                    }

                    if (currentAuthor.ChangeMap.TryGetValue(currentDate, out var changeSet))
                    {
                        changeSet.Additions += additions;
                        changeSet.Deletions += deletions;
                        changeSet.FileItems.Add(parts[2]);
                    }
                }
            }
        }

        if (options.Format == global::Format.Table)
        {
            if (options.ByDay != null)
            {
                TablePrinter.PrintTableByDaySelector(options.ByDay ?? global::ByDay.Lines, totals, options.FromDate, options.ToDate, options.HideSummary);
            }
            else
            {
                TablePrinter.PrintTableTotalsSelector(totals, options.HideSummary, options.Reverse, options.AuthorLimit);
            }
        }
        else if (options.Format == global::Format.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(totals, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        else if (options.Format == global::Format.Chart)
        {
            var chart = new BreakdownChart();
            var random = new Random();
            chart.UseValueFormatter(x => x.ToString("N0"));

            // Add all authors and line totals to the chart
            int index = 0;
            foreach (var author in totals.OrderByDescending(x => x.Value.ChangeMap.Values.Sum(x => x.Additions + x.Deletions)))
            {
                var authorName = author.Value.Name;
                var authorEmail = author.Value.Email;
                var authorData = author.Value.ChangeMap;
                var authorTotal = authorData.Values.Sum(x => x.Additions + x.Deletions);
                chart.AddItem(authorName, authorTotal, Colors[index % Colors.Length]);
                index++;
            }
            AnsiConsole.Write(chart);
        }
        else if (options.Format == global::Format.BarChart)
        {
            var chart = new BarChart();
            // chart.UseValueFormatter(x => x.ToString("N0"));
            var random = new Random();

            // Add all authors and line totals to the chart
            int index = 0;
            var sorted = totals.OrderByDescending(x => x.Value.ChangeMap.Values.Sum(x => x.Additions + x.Deletions));
            if (options.Reverse)
            {
                sorted = totals.OrderBy(x => x.Value.ChangeMap.Values.Sum(x => x.Additions + x.Deletions));
            }
            foreach (var author in sorted)
            {
                var authorName = author.Value.Name;
                var authorEmail = author.Value.Email;
                var authorData = author.Value.ChangeMap;
                var authorTotal = authorData.Values.Sum(x => x.Additions + x.Deletions);
                var item = new BarChartItem(authorName, authorTotal, Colors[index % Colors.Length]);
                // var item = new BarChartItem(authorName, authorTotal, Colors[index % 20], Colors[index % 20]);
                chart.AddItem(item);
                index++;
            }
            AnsiConsole.Write(chart);
        }

        return totals.Select(x => x.Value).ToList();
    }
}
