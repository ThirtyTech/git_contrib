using LibGit2Sharp;

// Get current directory
using (var repo = new Repository(Directory.GetCurrentDirectory()))
{
	var branches = repo.Branches;
	foreach (var branch in branches)
	{
		Console.WriteLine(branch.FriendlyName);
	}
}