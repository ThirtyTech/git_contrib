using System.Collections.Concurrent;
using LibGit2Sharp;

public static class Work
{
	public static ConcurrentDictionary<string, bool> ProcessedCommits = new ConcurrentDictionary<string, bool>();
	
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

	public static void DoWork(string directory, DateTimeOffset fromDate, string? mailmapDirectory)
	{

		Console.WriteLine("Processing directory: " + directory);
		// Making commit for the same of it here
		using (var repo = new Repository(directory))
		{
			var mailmap = new Mailmap(mailmapDirectory ?? directory);
			var filter = new CommitFilter
			{
				IncludeReachableFrom = repo.Refs,
				SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
			};

			// Filters out merged branch commits;	
			var commits = repo.Commits.QueryBy(filter).Where(c => c.Committer.When >= fromDate).Where(c => c.Parents.Count() == 1);
			var uniqueCommits = commits.Select(c => new
			{
				UniqueHash = (c.Message + repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree).Content).GetHashCode(),
				Commit = c
			}).DistinctBy(c => c.UniqueHash).Select(c => c.Commit);

			var uniqueCommitsGroupedByAuthor = uniqueCommits.GroupBy(c => c.Author.ToString());

			// Loop through each group of commits by author
			var authorContribs = uniqueCommitsGroupedByAuthor.Select(author => new AuthorContrib
			{
				Author = author.Key,
				Project = directory,
				Commits = author.ToList(),
				Totals = new Totals
				{
					Commits = author.Count(),
					Files = author.Select(c =>
					{

						var patch = repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree);
						return patch.Select(p => p.Path).Distinct().Count();
					}
					).Distinct().Count(),
					Lines = author.Select(c =>
					{
						var patch = repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree);
						var filtered = patch.Where(p => !ExcludeExtensions.Any(e => p.Path.EndsWith(e)));
						return filtered;
					})
					.SelectMany(p => p)
					.Sum(p => p.LinesAdded + p.LinesDeleted)
				}
			});

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
			}
			Console.WriteLine("#####################################\n");

		}
	}

}