using System.CommandLine;
using System.Diagnostics;

var FromDate = new Option<string>("--fromDate", description: "Starting date for commits to be considered");
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var Folder = new Option<string>("--folder", description: "Folder to search for git repositories", getDefaultValue: () =>
{
	var directory = !Debugger.IsAttached
		? Directory.GetCurrentDirectory()
		: Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
	return directory ?? "";
});

var root = new RootCommand {
	FromDate,
	Folder,
	Mailmap

};
//One line change for testing

root.SetHandler(async (folder, fromDate, mailmap) =>
{
	if (Utils.TryParseHumanReadableDateTimeOffset(fromDate, out var date))
	{
		Work.DoWork(folder, date, mailmap);
	}
	else
	{

		Work.DoWork(folder, DateTime.MinValue, mailmap);
	}
}, Folder, FromDate, Mailmap);

await root.InvokeAsync(args);
Console.WriteLine("Done");
return;

