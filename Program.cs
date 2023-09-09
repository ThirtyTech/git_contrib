using System.CommandLine;
using System.Diagnostics;
using LibGit2Sharp;

var FromDate = new Option<string>("--fromDate", description: "Starting date for commits to be considered");
var Folder = new Option<string>("--folder", description: "Folder to search for git repositories", getDefaultValue: () =>
{
	var directory = !Debugger.IsAttached
		? Directory.GetCurrentDirectory()
		: Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
	return directory ?? "";
});

var root = new RootCommand {
	FromDate,
	Folder

};
//One line change for testing

root.SetHandler(async (folder, fromDate) =>
{
	if (TryParseHumanReadableTimeSpan(fromDate, out var timeSpan))
	{
		DoWork(folder, DateTime.Now - timeSpan);
	}
}, Folder, FromDate);

await root.InvokeAsync(args);
Console.WriteLine("Done");
return;

void DoWork(string directory, DateTime fromDate)
{

	Console.WriteLine("Processing directory: " + directory);

	// Get current directory
	using (var repo = new Repository(directory))
	{
		var branches = repo.Branches;
		var authors = repo.Commits.Select(c => c.Author).Distinct();
		var linesChanged = repo.Commits.Where(c => c.Committer.When >= fromDate).Select(c => repo.Diff.Compare<Patch>(c.Parents.FirstOrDefault()?.Tree, c.Tree));
		foreach (var line in linesChanged)
		{
			Console.WriteLine(line.LinesAdded);
		}
		foreach (var author in authors)
		{
			Console.WriteLine(author.Name);
		}
		foreach (var branch in branches)
		{
			Console.WriteLine(branch.FriendlyName);
		}
	}
}

bool TryParseHumanReadableTimeSpan(string input, out TimeSpan timeSpan)
{
	timeSpan = default;

	if (string.IsNullOrWhiteSpace(input)) return false;

	var parts = input.Split(' ');
	if (parts.Length != 2) return false;

	if (!int.TryParse(parts[0], out var quantity)) return false;

	switch (parts[1].ToLower())
	{
		case "second":
		case "seconds":
			timeSpan = TimeSpan.FromSeconds(quantity);
			break;

		case "minute":
		case "minutes":
			timeSpan = TimeSpan.FromMinutes(quantity);
			break;

		case "hour":
		case "hours":
			timeSpan = TimeSpan.FromHours(quantity);
			break;

		case "day":
		case "days":
			timeSpan = TimeSpan.FromDays(quantity);
			break;

		case "week":
		case "weeks":
			timeSpan = TimeSpan.FromDays(quantity * 7);
			break;

		default:
			return false;
	}

	return true;
}

// Use parent directory during debug. Check for if DEBUG