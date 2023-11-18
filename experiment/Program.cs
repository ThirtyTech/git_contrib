using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

class Program
{
    class ChangeSet
    {
        public int Additions { get; set; }
        public int Deletions { get; set; }
    }

    class AuthorData
    {
        public string Name { get; set; }
        public Dictionary<string, ChangeSet> ChangeMap { get; set; } = new Dictionary<string, ChangeSet>();
    }

    static void Main(string[] args)
    {
        var maxDates = 0;
        if (args.Length < 1 || !int.TryParse(args[0], out maxDates) || maxDates <= 0)
        {
            Console.Error.WriteLine("Usage: <executable> <max_dates>");
            Environment.Exit(2);
        }

        try
        {
            Run(maxDates).Wait();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }

    static async Task Run(int maxDates)
    {
        var gitCmd = "git";
        var gitArgs = new[] { "--no-pager", "log", "--all", "--summary", "--numstat", "--format=\"^%C(yellow)%h%C(reset) %C(green)%aI%C(reset) %C(red)%an <%ae>%C(reset) %gs\"" };

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = gitCmd,
                Arguments = string.Join(" ", gitArgs),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        using var reader = process.StandardOutput;
        var commitMap = new HashSet<string>();
        var totals = new Dictionary<string, AuthorData>();

        AuthorData currentAuthor = null;
        string currentDate = "";
        bool skipUntilNextCommit = false;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (skipUntilNextCommit && !line.StartsWith("^"))
                continue;

            if (line.StartsWith("^"))
            {
                skipUntilNextCommit = false;
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    throw new Exception($"Invalid author line format: {line}");

                var commit = parts[0].Trim('^');
                if (!commitMap.Add(commit))
                {
                    skipUntilNextCommit = true;
                    continue;
                }

                var email = parts[2].Trim('<', '>');
                if (!totals.TryGetValue(email, out currentAuthor))
                {
                    currentAuthor = new AuthorData { Name = parts[2] };
                    totals[email] = currentAuthor;
                }

                currentDate = parts[1].Split('T')[0];
            }
            else if (char.IsDigit(line[0]) && currentAuthor != null)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    throw new Exception($"Invalid totals line format: {line}");

                if (!int.TryParse(parts[0], out int additions))
                    throw new Exception($"Invalid number for additions: {parts[0]}");

                if (!int.TryParse(parts[1], out int deletions))
                    throw new Exception($"Invalid number for deletions: {parts[1]}");

                if (!currentAuthor.ChangeMap.TryGetValue(currentDate, out var changeset))
                {
                    changeset = new ChangeSet();
                    currentAuthor.ChangeMap[currentDate] = changeset;
                }

                changeset.Additions += additions;
                changeset.Deletions += deletions;
            }
        }

        PrintTotals(totals, maxDates);
    }

    static void PrintTotals(Dictionary<string, AuthorData> totals, int maxDates)
    {
        Console.Write("{0,-20}", "Author's Name");

        var dates = Enumerable.Range(0, maxDates)
                              .Select(i => DateTime.UtcNow.AddDays(-maxDates).AddDays(i).Date)
                              .OrderBy(date => date)
                              .ToList();

        foreach (var date in dates)
            Console.Write($"\t{date:MM/dd}");

        Console.WriteLine("\tTotal");

        foreach (var authorData in totals.Values)
        {
            Console.Write("{0,-20}", authorData.Name);
            var total = 0;

            foreach (var date in dates.Select(d => d.ToString("yyyy-MM-dd")))
            {
                authorData.ChangeMap.TryGetValue(date, out var changes);
                var totalChanges = changes?.Additions + changes?.Deletions ?? 0;
                total += totalChanges;
                Console.Write("\t{0}", totalChanges);
            }

            Console.WriteLine("\t{0}", total);
        }
    }
}

