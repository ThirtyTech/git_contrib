using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text.Json;
using LibGit2Sharp;
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
var ByDay = new Option<bool>("--by-day", description: "Show results by day");
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var Format = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => global::Format.Table);
var ShowSummary = new Option<bool>("--show-summary", description: "Show project summary details");
var IgnoreAuthors = new Option<string[]>("--ignore-authors", description: "Authors to ignore") { AllowMultipleArgumentsPerToken = true };
var IgnoreFiles = new Option<string[]>("--ignore-files", description: "Files to ignore") { AllowMultipleArgumentsPerToken = true };

var root = new RootCommand {
            FromDate,
            ToDate,
            Path,
            Mailmap,
            Format,
            ShowSummary,
            IgnoreAuthors,
            IgnoreFiles,
            ByDay
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

var Chart = new Command("chart", "Launch interactive server to view results") { Path, IgnoreAuthors, FromDate, ToDate, Mailmap, IgnoreFiles };

root.AddCommand(Config);
root.AddCommand(Plot);
root.AddCommand(Chart);

root.SetHandler((context) =>
{
    var options = new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        ByDay = context.ParseResult.GetValueForOption(ByDay),
        Path = context.ParseResult.GetValueForArgument(Path),
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Format = context.ParseResult.GetValueForOption(Format),
        ShowSummary = context.ParseResult.GetValueForOption(ShowSummary),
        IgnoreAuthors = context.ParseResult.GetValueForOption(IgnoreAuthors) ?? Array.Empty<string>(),
        IgnoreFiles = context.ParseResult.GetValueForOption(IgnoreFiles) ?? Array.Empty<string>(),
    };
    Work.DoWork(options);
});


Config.SetHandler((options) =>
{

    Work.DoWork(Options.Convert(options.ParseResult.GetValueForArgument(ConfigArg)));
});

Plot.SetHandler(async (context) =>
{
    var options = new Options
    {
        FromDate = context.ParseResult.GetValueForOption(FromDate) ?? DateTimeOffset.MinValue,
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Path = context.ParseResult.GetValueForArgument(Path),
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? string.Empty,
        Format = context.ParseResult.GetValueForOption(Format),
    };
    await CommitPlot.DoWorkAsync(options);
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
