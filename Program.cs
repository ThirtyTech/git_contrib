using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Diagnostics;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var Folder = new Argument<string>("--folder", description: "Folder to search for git repositories", getDefaultValue: () =>
		{
			// var directory = !Debugger.IsAttached
			// 	? Directory.GetCurrentDirectory()
			// 	: Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
			return Directory.GetCurrentDirectory();
		});
		var FromDate = new Option<DateTimeOffset>("--fromDate", description: "Starting date for commits to be considered", parseArgument: (result) =>
		{
			var input = result.Tokens.Single().Value;
			return Utils.TryParseHumanReadableDateTimeOffset(input, out var _fromDate) ? _fromDate : DateTimeOffset.MinValue;

		});
		var ToDate = new Option<DateTimeOffset>("--toDate", description: "Ending date for commits to be considered", parseArgument: (result) =>
		{
			var input = result.Tokens.Single().Value;
			return Utils.TryParseHumanReadableDateTimeOffset(input, out var _toDate) ? _toDate : DateTimeOffset.MinValue;

		});
		var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
		var Format = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => global::Format.Table);

		var root = new RootCommand {
			FromDate,
			ToDate,
			Folder,
			Mailmap,
			Format
		};

		root.Handler = CommandHandler.Create<Options>(Work.DoWork);

		return await root.InvokeAsync(args);
	}
}