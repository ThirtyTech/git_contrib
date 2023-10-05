using System.Diagnostics;
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

            var uniqueCommits = uniqueCommitsEarly.Select(c => c.Commit).OrderBy(x => x.Author.When).ToList();
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
            .WithDegreeOfParallelism(MaxConcurrency)
            .Select(author =>
            {
                var totals = author.Select(c =>
                {
                    var patch = repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree);
                    var Files = patch.Select(p => p.Path).Where(p => !options.IgnoreFiles.Any(e => p.Contains(e))).ToList();
                    var Lines = patch.Where(p => !ExcludeExtensions.Any(e => p.Path.EndsWith(e) && !options.IgnoreFiles.Any(e => p.Path.Contains(e)))).Sum(p => p.LinesAdded + p.LinesDeleted);
                    return (Files, Lines);

                }).ToList();

                pbar?.Tick();

                return new AuthorContrib
                {
                    Author = author.Key,
                    Project = options.Path,
                    Commits = author.ToList(),
                    Totals = new Totals
                    {
                        Commits = author.Count(),
                        Files = totals.SelectMany(f => f.Files).Distinct().Count(),
                        Lines = totals.Select(p => p.Lines).Sum(p => p)
                    }
                };
            }).ToList();
            pbar?.Dispose();

            // Merge author records where name matches mailmap.validate
            var mergedAuthorContribs = authorContribs.GroupBy(a => mailmap.Validate(a.Author)).Select(g => new AuthorContrib
            {
                Author = g.Key,
                Project = options.Path,
                Commits = g.SelectMany(a => a.Commits).ToList(),
                Totals = new Totals
                {
                    Commits = g.Sum(a => a.Totals.Commits),
                    Files = g.Sum(a => a.Totals.Files),
                    Lines = g.Sum(a => a.Totals.Lines)
                }
            }).OrderByDescending(a => a.Totals.Lines);

            if (options.Format == global::Format.Table)
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
            else if (options.Format == global::Format.Json)
            {
                var result = JsonSerializer.Serialize(mergedAuthorContribs);
                Console.WriteLine(result);
            }
            return mergedAuthorContribs;

        }
    }

}
