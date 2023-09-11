using System.CommandLine;
using System.Diagnostics;

var FromDate = new Option<string>("--fromDate", description: "Starting date for commits to be considered");
var ToDate = new Option<string>("--toDate", description: "Ending date for commits to be considered");
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var allowedFormats = new[] { "json", "table" };
var Format = new Option<string>("--format", description: "Format to output results in", getDefaultValue: () => "table");
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
	Mailmap,
	Format

};

root.SetHandler((folder, fromDate, toDate, mailmap, format) =>
{
	if (!allowedFormats.Contains(format))
	{
		throw new ArgumentException($"Format must be one of {string.Join(", ", allowedFormats)}");
	}
	var formattedFromDate = Utils.TryParseHumanReadableDateTimeOffset(fromDate, out var _fromDate) ? _fromDate : DateTimeOffset.MinValue;
	var formattedToDate = Utils.TryParseHumanReadableDateTimeOffset(toDate, out var _toDate) ? _toDate : DateTimeOffset.Now;
	Work.DoWork(folder, formattedFromDate, formattedToDate, mailmap, format);
}, Folder, FromDate, ToDate, Mailmap, Format);

await root.InvokeAsync(args);

