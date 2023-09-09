using System.Collections.Concurrent;
using LibGit2Sharp;

public static class Work
{
	public static ConcurrentDictionary<string, bool> ProcessedCommits = new ConcurrentDictionary<string, bool>();
	public static void DoWork(string directory, DateTimeOffset fromDate)
	{

		Console.WriteLine("Processing directory: " + directory);
		var mailmap = new Mailmap(directory);
		// Get current directory
		using (var repo = new Repository(directory))
		{
			var branches = repo.Branches;
			var filter = new CommitFilter
			{
				SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
			};
			var commits = repo.Commits.QueryBy(filter).Where(c => c.Committer.When >= fromDate).Where(c => c.Parents.Count() == 1);
			var commitsByAuthor = commits.GroupBy(c => c.Author.Name);


			var authors = commits.Select(c => c.Author.ToString()).Distinct();
			var reducedAuthors = authors.Select(a => mailmap.Validate(a)).Distinct();
			var linesChanged = commits.Select(c => repo.Diff.Compare<Patch>(c.Parents.FirstOrDefault()?.Tree, c.Tree));
			var commitsDoneByAuthor = commitsByAuthor.Select(g => new
			{
				Author = g.Key,
				Commits = g.Count()
			});
			var filesChangedByAuthor = commitsByAuthor.Select(g => new
			{
				Author = g.Key,
				Files = g.SelectMany(c => c.Tree.Select(t => t.Path)).Distinct().Count()
			});

			foreach (var file in filesChangedByAuthor)
			{
				Console.WriteLine(file.Author + ": " + file.Files);
			}
			foreach (var commit in commitsDoneByAuthor)
			{
				Console.WriteLine(commit.Author + ": " + commit.Commits);
			}
			foreach (var line in linesChanged)
			{
				Console.WriteLine(line.LinesAdded + line.LinesDeleted);
			}
			foreach (var author in reducedAuthors)
			{
				Console.WriteLine(author);
			}
			foreach (var branch in branches)
			{
				Console.WriteLine(branch.FriendlyName);
			}
			foreach (var commit in commits)
			{
				Console.WriteLine(commit.MessageShort);
			}
		}
	}

}