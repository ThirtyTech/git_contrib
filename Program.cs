using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var Path = new Argument<string>("path", description: "Path to search for git repositories", getDefaultValue: Directory.GetCurrentDirectory);
		var FromDate = new Option<DateTimeOffset>("--from", description: "Starting date for commits to be considered",
		parseArgument: (result) =>
		{
			var input = result.Tokens.Single().Value;
			return Utils.TryParseHumanReadableDateTimeOffset(input, out var _date) ? _date : DateTimeOffset.MinValue;

		});
		var ToDate = new Option<DateTimeOffset>("--to", description: "Ending date for commits to be considered",
		parseArgument: (result) =>
		{
			var input = result.Tokens.Single().Value;
			return Utils.TryParseHumanReadableDateTimeOffset(input, out var _date) ? _date : DateTimeOffset.Now;

		});
		var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
		var Format = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => global::Format.Table);
		var ShowSummary = new Option<bool>("--show-summary", description: "Show project summary details", getDefaultValue: () => false);

		var root = new RootCommand {
			FromDate,
			ToDate,
			Path,
			Mailmap,
			Format,
			ShowSummary
		};

		var ConfigArg = new Argument<ConfigOptions>("path", description: "Path to config file", parse: (result) =>
		{
			var input = result.Tokens.Single().Value;
			return new ConfigOptions(input);
		});
		var config = new Command("config", "Configure defaults for the tool")
		{
			ConfigArg
		};

		root.AddCommand(config);
		root.Handler = CommandHandler.Create<Options>(Work.DoWork);
		config.SetHandler((options) =>
		{
			Work.DoWork(Options.Convert(options.ParseResult.GetValueForArgument(ConfigArg)));
		});

		return await new CommandLineBuilder(root)
			.UseHelp()
			.UseSuggestDirective()
			.UseTypoCorrections()
			.RegisterWithDotnetSuggest()
			.Build()
			.InvokeAsync(args);
	}
}