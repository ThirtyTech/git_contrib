using System.Collections.Concurrent;
using LibGit2Sharp;

public static class Work
{
	public static ConcurrentDictionary<string, bool> ProcessedCommits = new ConcurrentDictionary<string, bool>();
	public static void DoWork(string directory, DateTimeOffset fromDate)
	{

		Console.WriteLine("Processing directory: " + directory);
		// Making commit for the same of it here
		var mailmap = new Mailmap(directory);
		using (var repo = new Repository(directory))
		{
			var branches = repo.Branches;
			var filter = new CommitFilter
			{
				IncludeReachableFrom = repo.Refs,
				SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
			};
			var commits = repo.Commits.QueryBy(filter).Where(c => c.Committer.When >= fromDate)
			// Filters out merged branch commits;	
			.Where(c => c.Parents.Count() == 1);
			var commitsByAuthor = commits.GroupBy(c => c.Author.Name);
			var authors = commits.Select(c => c.Author.ToString()).Distinct();
			var reducedAuthors = authors.Select(a => mailmap.Validate(a)).Distinct();

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
					Files = author.SelectMany(c => c.Tree.Select(t => t.Path)).Distinct().Count(),
					Lines = author.Select(c => repo.Diff.Compare<Patch>(c.Tree, c.Parents?.First().Tree)).Sum(p => p.LinesAdded + p.LinesDeleted)
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
			});



			Console.WriteLine("############## Authors ##############");
			foreach (var author in mergedAuthorContribs)
			{
				Console.WriteLine(author.Author + " [Files: " + author.Totals.Files + "\tCommits: " + author.Totals.Commits + "\tLines:" + author.Totals.Lines + "]");
			}
			Console.WriteLine("#####################################\n");
			Console.WriteLine("############## Commits ##############");
			foreach (var commit in uniqueCommits)
			{
				Console.WriteLine(commit.MessageShort);
			}
			Console.WriteLine("#####################################\n");
		}
	}

}