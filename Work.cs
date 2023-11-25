using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;

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

    public static async Task<IEnumerable<AuthorData>?> DoWork(Options options)
    {

        if (options.Format != global::Format.None)
        {
            Console.WriteLine("Processing directory: " + options.Path);
        }

        if (!await Utils.IsGitDirectoryAsync(options.Path))
        {
            Console.WriteLine("Not a git directory: " + options.Path);
            return null;
        }

        int maxDates = (int)Math.Round((DateTime.Now - options.FromDate).TotalDays);

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
        var gitArgs = new List<string> { "--no-pager", "log", "--branches", "--remotes", "--summary", "--numstat", "--mailmap", "--no-merges", "--since", options.FromDate.AddDays(-1).ToString("yyyy-MM-dd"), "--format=^%h|%aI|%aN|<%aE>" };

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
                    var author = parts[2];
                    var email = parts[3].Trim('<', '>');
                    var authorDate = DateTimeOffset.Parse(date);
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
                    if (options.IgnoreFiles.Any(parts[2].Contains))
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
                        changeSet.Files++;
                    }
                }
            }
        }

        if (options.ByDay != null)
        {
            TablePrinter.PrintTableByDaySelector(options.ByDay.Value, totals, options.FromDate, options.ShowSummary);
        }
        else
        {
            TablePrinter.PrintTableTotalsSelector(totals);

        }

        return totals.Select(x => x.Value).ToList();
    }
}
