using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;

var Version = new Option<bool>("--version", "Show the version information and exit");

var Path = new Argument<string>("path", description: "Path to search for git repositories", getDefaultValue: Directory.GetCurrentDirectory);
var FromDate = new Option<DateTimeOffset?>("--from", description: "Starting date for commits to be considered",
parseArgument: (result) =>
{
    var input = result.Tokens.Single().Value;
    return Utils.TryParseHumanReadableDateTimeOffset(input, out var _date) ? _date : DateTimeOffset.MinValue;

});
var ToDate = new Option<DateTimeOffset?>("--to", description: "Ending date for commits to be considered",
parseArgument: (result) =>
{
    var input = result.Tokens.Single().Value;
    return Utils.TryParseHumanReadableDateTimeOffset(input, out var _date) ? _date : DateTimeOffset.Now;

});
var ByDay = new Option<ByDay?>("--by-day", description: "Show results by day");
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var Format = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => global::Format.Table);
var ShowSummary = new Option<bool>("--show-summary", description: "Show project summary details");
var Reverse = new Option<bool>("--reverse", description: "Reverse the order of the results");
var AuthorLimit = new Option<int?>("--limit", description: "Limit the number of authors to show");
var IgnoreAuthors = new Option<string[]>("--ignore-authors", description: "Authors to ignore") { AllowMultipleArgumentsPerToken = true };
var IgnoreFiles = new Option<string[]>("--ignore-files", description: "Files to ignore") { AllowMultipleArgumentsPerToken = true };

var root = new RootCommand($"Git Contrib v{Assembly.GetEntryAssembly()?.GetName().Version} gives statistics by authors to the project.") {
            Version,
            FromDate,
            ToDate,
            Path,
            Mailmap,
            Format,
            ShowSummary,
            IgnoreAuthors,
            IgnoreFiles,
            ByDay,
            Reverse,
            AuthorLimit
        };

var ConfigArg = new Argument<ConfigOptions>("path", description: "Path to config file", parse: (result) =>
{
    var input = result.Tokens.Single().Value;
    return new ConfigOptions(input);
});
var Config = new Command("config", "Configure defaults for the tool")
{
    ConfigArg
};

var Chart = new Command("chart", "Launch interactive server to view results") { Path, IgnoreAuthors, FromDate, ToDate, Mailmap, IgnoreFiles };

root.AddCommand(Config);
// root.AddCommand(Plot);
root.AddCommand(Chart);

root.SetHandler(async (context) =>
{
    if (context.ParseResult.GetValueForOption(Version))
    {
        Console.WriteLine($"Git Contrib v{Assembly.GetEntryAssembly()?.GetName().Version}");
        return;
    }
    var options = new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        ByDay = context.ParseResult.GetValueForOption(ByDay),
        Path = context.ParseResult.GetValueForArgument(Path),
        Format = context.ParseResult.GetValueForOption(Format),
        Reverse = context.ParseResult.GetValueForOption(Reverse),
        AuthorLimit = context.ParseResult.GetValueForOption(AuthorLimit),
        ShowSummary = context.ParseResult.GetValueForOption(ShowSummary),
        IgnoreAuthors = context.ParseResult.GetValueForOption(IgnoreAuthors) ?? Array.Empty<string>(),
        IgnoreFiles = context.ParseResult.GetValueForOption(IgnoreFiles) ?? Array.Empty<string>(),
    };
    await Work.DoWork(options);
});


Config.SetHandler(async (options) =>
{

    await Work.DoWork(Options.Convert(options.ParseResult.GetValueForArgument(ConfigArg)));
});

Chart.SetHandler((context) =>
{
    var options = new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Path = context.ParseResult.GetValueForArgument(Path),
        IgnoreAuthors = context.ParseResult.GetValueForOption(IgnoreAuthors) ?? Array.Empty<string>(),
        IgnoreFiles = context.ParseResult.GetValueForOption(IgnoreFiles) ?? Array.Empty<string>(),
    };
    ChartServer.DoWork(options);
});

return await new CommandLineBuilder(root)
    .UseHelp()
    .UseSuggestDirective()
    .UseTypoCorrections()
    .RegisterWithDotnetSuggest()
    .Build()
    .InvokeAsync(args);
