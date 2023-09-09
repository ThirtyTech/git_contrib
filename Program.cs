using System.Diagnostics;
using LibGit2Sharp;

// Use parent directory during debug. Check for if DEBUG
var directory = !Debugger.IsAttached
	? Directory.GetCurrentDirectory()
	: Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;


// Get current directory
using (var repo = new Repository(directory))
{
	var branches = repo.Branches;
	var authors = repo.Commits.Select(c => c.Author).Distinct();
	var linesChangedByAuthor = repo.Commits.Select(c => repo.Diff.Compare<Patch>(c.Parents.First().Tree, c.Tree));
	foreach (var author in authors)
	{
		Console.WriteLine(author.Name);
	}
	foreach (var branch in branches)
	{
		Console.WriteLine(branch.FriendlyName);
	}
}