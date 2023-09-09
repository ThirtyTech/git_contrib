using System.CommandLine;
using System.Diagnostics;

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
	if (Utils.TryParseHumanReadableDateTimeOffset(fromDate, out var date))
	{
		Work.DoWork(folder, date);
	}
	else { 

		Work.DoWork(folder, DateTime.MinValue);
	}
}, Folder, FromDate);

await root.InvokeAsync(args);
Console.WriteLine("Done");
return;

