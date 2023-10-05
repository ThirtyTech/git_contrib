using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
// using LibGit2Sharp;
// GlobalSettings.NativeLibraryPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libgit2-e632535.so");

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
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var Format = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => global::Format.Table);
var ShowSummary = new Option<bool>("--show-summary", description: "Show project summary details", getDefaultValue: () => false);
var IgnoreAuthors = new Option<string[]>("--ignore-authors", description: "Authors to ignore", getDefaultValue: () => new string[] { });

var root = new RootCommand {
            FromDate,
            ToDate,
            Path,
            Mailmap,
            Format,
            ShowSummary,
            IgnoreAuthors
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

var Plot = new Command("plot", "Plot the results of the analysis")
{
    FromDate,
    ToDate,
    Path,
    Mailmap,
    Format,
};

var Chart = new Command("chart", "Laucnh interactive server to view results") { Path, IgnoreAuthors, FromDate, ToDate, Mailmap };

root.AddCommand(Config);
root.AddCommand(Plot);
root.AddCommand(Chart);

root.SetHandler((context) =>
{
    Work.DoWork(new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Path = context.ParseResult.GetValueForArgument(Path),
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Format = context.ParseResult.GetValueForOption(Format),
        ShowSummary = context.ParseResult.GetValueForOption(ShowSummary)
    });
});


Config.SetHandler((options) =>
{
    Work.DoWork(Options.Convert(options.ParseResult.GetValueForArgument(ConfigArg)));
});

Plot.SetHandler(async (context) =>
{
    await CommitPlot.DoWorkAsync(new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Path = context.ParseResult.GetValueForArgument(Path),
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Format = context.ParseResult.GetValueForOption(Format),
    });

});

Chart.SetHandler((context) =>
{
    ChartServer.DoWork(new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Path = context.ParseResult.GetValueForArgument(Path),
        IgnoreAuthors = context.ParseResult.GetValueForOption(IgnoreAuthors) ?? new string[] { },
    });
});

return await new CommandLineBuilder(root)
    .UseHelp()
    .UseSuggestDirective()
    .UseTypoCorrections()
    .RegisterWithDotnetSuggest()
    .Build()
    .InvokeAsync(args);
