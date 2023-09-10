using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Data.HashFunction;
using LibGit2Sharp;
using System.Data.HashFunction.xxHash;
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


	public static readonly IxxHash _hasher = xxHashFactory.Instance.Create();
	public static int MaxConcurrency = Environment.ProcessorCount - 1;

	public static async Task DoWorkAsync(string directory, DateTimeOffset fromDate, DateTimeOffset toDate, string? mailmapDirectory)
	{

		Console.WriteLine("Processing directory: " + directory);
		// Making commit for the same of it here
		using (var repo = new Repository(directory))
		{
			if (repo.Network.Remotes.Count() > 0)
			{
				Console.WriteLine("Fetching remote: " + repo.Network.Remotes.First().Name);
				var psi = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "fetch",
					WorkingDirectory = directory,
					UseShellExecute = false,
				};

				var process = Process.Start(psi);
				process?.WaitForExit();
			}
			var mailmap = new Mailmap(mailmapDirectory ?? directory);
			var filter = new CommitFilter
			{
				IncludeReachableFrom = repo.Refs,
				SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
			};

			// Filters out merged branch commits;	
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			var commits = repo.Commits.QueryBy(filter).Where(c => c.Parents.Count() == 1).Where(c => c.Committer.When >= fromDate).Where(c => c.Committer.When <= toDate);
			var uniqueCommitsEarly = commits.AsParallel().WithDegreeOfParallelism(MaxConcurrency).Select(c =>
			{
				var message = c.Message;
				var authorDate = c.Author.When;
				var hash = (message + authorDate).GetHashCode();

				return new
				{
					// UniqueHash = _hasher.ComputeHash(c.Message + repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree).Content),
					UniqueHash = hash,
					Commit = c
				};
			}).ToList().DistinctBy(c => c.UniqueHash).ToList();

			var uniqueCommits = uniqueCommitsEarly.Select(c => c.Commit);

			// Console.WriteLine(stopWatch.ToString() + "ms to filter commits");

			var uniqueCommitsGroupedByAuthor = uniqueCommits.GroupBy(c => c.Author.ToString());

			// Loop through each group of commits by author
			var pbar = new ProgressBar(uniqueCommitsGroupedByAuthor.Count(), "Processing commits by author", new ProgressBarOptions
			{
				ForegroundColor = ConsoleColor.Yellow,
				ForegroundColorDone = ConsoleColor.DarkGreen,
				ProgressCharacter = '─',
				ProgressBarOnBottom = true
			});
			var authorContribs = uniqueCommitsGroupedByAuthor.AsParallel().WithDegreeOfParallelism(MaxConcurrency)
			.Select(author =>
			{
				var totals = author.Select(c =>
				{
					var patch = repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree);
					var Files = patch.Select(p => p.Path);
					var Lines = patch.Where(p => !ExcludeExtensions.Any(e => p.Path.EndsWith(e))).Sum(p => p.LinesAdded + p.LinesDeleted);
					return (Files, Lines);

				}).ToList();
				pbar.Tick();

				return new AuthorContrib
				{
					Author = author.Key,
					Project = directory,
					Commits = author.ToList(),
					Totals = new Totals
					{
						Commits = author.Count(),
						Files = totals.SelectMany(f => f.Files).Distinct().Count(),
						Lines = totals.Select(p => p.Lines).Sum(p => p)
					}
				};
			}).ToList();
			pbar.Dispose();
			// Console.WriteLine(stopWatch.ToString() + "ms to group commits by author");

			// Merge author records where name matches mailmap.validate
			var mergedAuthorContribs = authorContribs.GroupBy(a => mailmap.Validate(a.Author)).Select(g => new AuthorContrib
			{
				Author = g.Key,
				Project = directory,
				Commits = g.SelectMany(a => a.Commits).ToList(),
				Totals = new Totals
				{
					Commits = g.Sum(a => a.Totals.Commits),
					Files = g.Sum(a => a.Totals.Files),
					Lines = g.Sum(a => a.Totals.Lines)
				}
			}).OrderByDescending(a => a.Totals.Lines);

			Console.WriteLine("############## Authors ##############");
			foreach (var author in mergedAuthorContribs)
			{
				Console.WriteLine(author.Author.PadRight(50) + "\t[Files: " + author.Totals.Files + "\tCommits: " + author.Totals.Commits + "\tLines:" + author.Totals.Lines.ToString("N0") + "]");
				// var counter = 0;
				// foreach (var commit in author.Commits)
				// {
				// 	Console.WriteLine("\t" + (counter++) + " " + commit.MessageShort);

				// }
			}
			Console.WriteLine("#####################################\n");

		}
	}

}