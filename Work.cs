using LibGit2Sharp;

public static class Work
{
	public static void DoWork(string directory, DateTimeOffset fromDate)
	{

		Console.WriteLine("Processing directory: " + directory);

		// Get current directory
		using (var repo = new Repository(directory))
		{
			var branches = repo.Branches;
			var authors = repo.Commits.Select(c => c.Author.Name).Distinct();
			var linesChanged = repo.Commits.Where(c => c.Committer.When >= fromDate).Select(c => repo.Diff.Compare<Patch>(c.Parents.FirstOrDefault()?.Tree, c.Tree));
			var commitsDoneByAuthor = repo.Commits.Where(c => c.Committer.When >= fromDate).GroupBy(c => c.Author.Name).Select(g => new { Author = g.Key, Commits = g.Count() });
			var filesChangedByAuthor = repo.Commits.Where(c => c.Committer.When >= fromDate).GroupBy(c => c.Author.Name).Select(g => new { Author = g.Key, Files = g.SelectMany(c => c.Tree.Select(t =>  t.Path)).Distinct().Count() });
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
			foreach (var author in authors)
			{
				Console.WriteLine(author);
			}
			foreach (var branch in branches)
			{
				Console.WriteLine(branch.FriendlyName);
			}
		}
	}

}