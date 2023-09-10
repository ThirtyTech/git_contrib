using System.CommandLine;
using System.Diagnostics;

var FromDate = new Option<string>("--fromDate", description: "Starting date for commits to be considered");
var ToDate = new Option<string>("--toDate", description: "Ending date for commits to be considered");
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
	ToDate,
	Folder,
	Mailmap

};

root.SetHandler((folder, fromDate, toDate, mailmap) =>
{
	var formattedFromDate = Utils.TryParseHumanReadableDateTimeOffset(fromDate, out var _fromDate) ? _fromDate : DateTimeOffset.MinValue;
	var formattedToDate = Utils.TryParseHumanReadableDateTimeOffset(toDate, out var _toDate) ? _toDate : DateTimeOffset.Now;
	Work.DoWork(folder, formattedFromDate, formattedToDate, mailmap);
}, Folder, FromDate, ToDate, Mailmap);

await root.InvokeAsync(args);
Console.WriteLine("Done");
return;

