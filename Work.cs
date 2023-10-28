using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleTables;
using LibGit2Sharp;
using ShellProgressBar;

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


    public static readonly int MaxConcurrency = Environment.ProcessorCount - 1;

    public static IEnumerable<AuthorContrib> DoWork(Options options)
    {

        if (options.Format != global::Format.None)
        {
            Console.WriteLine("Processing directory: " + options.Path);
        }
        // Making commit for the same of it here
        using (var repo = new Repository(options.Path))
        {
            if (options.Fetch && repo.Network.Remotes.Count() > 0)
            {
                if (options.Format != global::Format.None)
                {
                    Console.WriteLine("Fetching remote: " + repo.Network.Remotes.First().Name);
                }
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "fetch",
                    WorkingDirectory = options.Path,
                    UseShellExecute = false,
                };

                var process = Process.Start(psi);
                process?.WaitForExit();
            }
            var mailmap = new Mailmap(!string.IsNullOrWhiteSpace(options.Mailmap) ? options.Mailmap : options.Path);
            var filter = new CommitFilter
            {
                IncludeReachableFrom = repo.Refs,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
            };

            // Filters out merged branch commits;
            var commits = repo.Commits.QueryBy(filter)
                .Where(c => c.Parents.Count() == 1)
                .Where(c => c.Committer.When >= options.FromDate)
                .Where(c => c.Committer.When <= options.ToDate);

            var uniqueCommitsEarly = commits.AsParallel().WithDegreeOfParallelism(MaxConcurrency).Select(c =>
            {
                var message = c.Message;
                var authorDate = c.Author.When;
                var hash = (message + authorDate).GetHashCode();

                return new
                {
                    UniqueHash = hash,
                    Commit = c
                };
            }).ToList().DistinctBy(c => c.UniqueHash).ToList();

            var uniqueCommits = uniqueCommitsEarly
                .Select(c => c.Commit)
                .OrderBy(x => x.Author.When)
                .Where(x => !options.IgnoreAuthors.Any(e => x.Author.ToString().Contains(e)))
                .ToList();

            var uniqueCommitsGroupedByAuthor = uniqueCommits.GroupBy(c => c.Author.ToString());

            var pbar = options.Format != global::Format.None ? new ProgressBar(
                uniqueCommitsGroupedByAuthor.Count(),
                "Processing commits by author",
                new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.DarkGreen,
                    ProgressCharacter = '─',
                    ProgressBarOnBottom = true
                }
            ) : null;

            // Loop through each group of commits by author
            var authorContribs = uniqueCommitsGroupedByAuthor
            .AsParallel()
            .WithDegreeOfParallelism(1)
            .Select(author =>
            {
                // Group Author commits by DateOnly
                var authorCommitsByDate = author.GroupBy(c => c.Author.When.Date).Select(g =>
                {
                    var patches = g.SelectMany(c => repo.Diff.Compare<Patch>(c.Tree, c.Parents.First().Tree)).ToList();
                    var files = patches.Select(p => p.Path).Where(p => !options.IgnoreFiles.Any(e => p.Contains(e))).Distinct().ToList();
                    var filteredPatches = patches.Where(p => !ExcludeExtensions.Any(e => p.Path.EndsWith(e)) && !options.IgnoreFiles.Any(e => p.Path.Contains(e))).ToList();
                    var linesAdded = filteredPatches.Sum(p => p.LinesAdded);
                    var linesDeleted = filteredPatches.Sum(p => p.LinesDeleted);
                    return new Totals
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        Files = files.Count(),
                        Lines = linesAdded + linesDeleted,
                        LinesAdded = linesAdded,
                        LinesDeleted = linesDeleted,
                        Commits = g.Count()
                    };
                }).ToList();

                pbar?.Tick();

                return new AuthorContrib
                {
                    Author = author.Key,
                    Project = options.Path,
                    Commits = author.ToList(),
                    TotalsByDate = authorCommitsByDate,
                    Totals = new Totals
                    {
                        Commits = author.Count(),
                        Files = authorCommitsByDate.Select(f => f.Files).Distinct().Count(),
                        Lines = authorCommitsByDate.Select(p => p.Lines).Sum(p => p)
                    }
                };
            }).ToList();
            pbar?.Dispose();


            // Merge author records where name matches mailmap.validate
            var mergedAuthorContribs = authorContribs.GroupBy(a => mailmap.Validate(a.Author)).Select(g =>
            {
                return new AuthorContrib
                {
                    Author = g.Key,
                    Project = options.Path,
                    Commits = g.SelectMany(a => a.Commits).ToList(),
                    TotalsByDate = g.SelectMany(a => a.TotalsByDate).GroupBy(a => a.Date).Select(g =>
                    {
                        return new Totals
                        {
                            Date = g.Key,
                            Files = g.Sum(a => a.Files),
                            Lines = g.Sum(a => a.Lines),
                            LinesAdded = g.Sum(a => a.LinesAdded),
                            LinesDeleted = g.Sum(a => a.LinesDeleted),
                            Commits = g.Sum(a => a.Commits)
                        };
                    }).ToList(),
                    Totals = new Totals
                    {
                        Commits = g.Sum(a => a.Totals.Commits),
                        Files = g.Sum(a => a.Totals.Files),
                        Lines = g.Sum(a => a.Totals.Lines)
                    }
                };
            }).OrderByDescending(a => a.Totals.Lines);

            if (options.Format == global::Format.Table && !options.ByDay)
            {

                var table = new ConsoleTable("Author", "Files", "Commits", "Lines");
                table.Options.EnableCount = false;
                Console.WriteLine("Date Range: " + options.FromDate.ToString("MM/dd/yyyy") + " - " + options.ToDate.ToString("MM/dd/yyyy"));
                Console.WriteLine("First Commit: " + uniqueCommits.FirstOrDefault()?.Committer.When ?? "");
                Console.WriteLine("Last Commit: " + uniqueCommits.LastOrDefault()?.Committer.When ?? "");
                foreach (var author in mergedAuthorContribs)
                {
                    table.AddRow(author.Author, author.Totals.Files.ToString("N0"), author.Totals.Commits.ToString("N0"), author.Totals.Lines.ToString("N0"));
                }
                if (options.ShowSummary)
                {
                    table.AddRow("Project Totals", mergedAuthorContribs.Sum(a => a.Totals.Files).ToString("N0"), mergedAuthorContribs.Sum(a => a.Totals.Commits).ToString("N0"), mergedAuthorContribs.Sum(a => a.Totals.Lines).ToString("N0"));
                }
                table.Write();
            }
            else if (options.Format == global::Format.Table && options.ByDay)
            {
                // List of dates from uniqueCommits first and last commit dates
                Console.WriteLine("Date Range: " + options.FromDate.ToString("MM/dd/yyyy") + " - " + options.ToDate.ToString("MM/dd/yyyy"));
                Console.WriteLine("First Commit: " + uniqueCommits.FirstOrDefault()?.Committer.When ?? "");
                Console.WriteLine("Last Commit: " + uniqueCommits.LastOrDefault()?.Committer.When ?? "");
                var dateRange = Enumerable.Range(0, 1 + options.ToDate.Subtract(options.FromDate).Days).Select(offset => DateOnly.FromDateTime(options.FromDate.AddDays(offset).DateTime)).ToList();

                WriteTable(mergedAuthorContribs, dateRange, t => t.Lines, options.ShowSummary);
                WriteTable(mergedAuthorContribs, dateRange, t => t.Files, options.ShowSummary);
                WriteTable(mergedAuthorContribs, dateRange, t => t.Commits, options.ShowSummary);

            }
            else if (options.Format == global::Format.Json)
            {
                var result = JsonSerializer.Serialize(mergedAuthorContribs);
                Console.WriteLine(result);
            }
            return mergedAuthorContribs;

        }
    }

    public static void WriteTable(IOrderedEnumerable<AuthorContrib> authorContribs, List<DateOnly> dateRange, Expression<Func<Totals, int>> prop, bool showSummary)
    {
        var compiled = prop.Compile();
        string[] baseColumns = [$"Author ({((MemberExpression)prop.Body).Member.Name})"];
        var columns = baseColumns.Concat(dateRange.Select(d => d.ToString("MM/dd")).ToList()).ToList();
        columns.Add("Total");

        var table = new ConsoleTable(columns.ToArray());
        table.Options.EnableCount = false;
        foreach (var author in authorContribs)
        {
            var values = new List<string>
                    {
                        author.Author
                    }.Concat(dateRange.Select(d => author.TotalsByDate.FirstOrDefault(t => t.Date == d) ?? new Totals()).Select(x =>
                    {
                        // return $"{x.Lines:N0} / {x.Files:N0} / {x.Commits:N0}";
                        return $"{compiled(x):N0}";
                    }).ToList()).ToList();
            values.Add(compiled(author.Totals).ToString("N0"));
            table.AddRow(values.ToArray());
        }
        if (showSummary)
        {
            var values = new List<string>
                    {
                        "Project Totals"
                    }.Concat(dateRange.Select(d => authorContribs.SelectMany(a => a.TotalsByDate).Where(t => t.Date == d).Sum(t => (long?)compiled(t)) ?? 0).Select(x => x.ToString("N0"))).ToList();
            values.Add(authorContribs.Sum(a => compiled(a.Totals)).ToString("N0"));
            table.AddRow(values.ToArray());
        }
        table.Write();

    }

}
