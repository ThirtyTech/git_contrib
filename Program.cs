using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using git_contrib;
using git_contrib.Models;

var Version = new Option<bool>("--version", "Show the version information and exit");

var RepoPath = new Argument<string>("path", description: "Path to search for git repositories", getDefaultValue: Directory.GetCurrentDirectory);
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
var Metric = new Option<Metric?>("--metric", description: "Which metric to show. Defaults to overall");
var Mailmap = new Option<string>("--mailmap", description: "Path to mailmap file");
var OutputFormat = new Option<Format>("--format", description: "Format to output results in", getDefaultValue: () => Format.Table);
var HideSummary = new Option<bool>("--hide-summary", description: "Hide project summary details");
var Reverse = new Option<bool>("--reverse", description: "Reverse the order of the results");
var AuthorLimit = new Option<int?>("--limit", description: "Limit the number of authors to show");
var IgnoreAuthors = new Option<string[]>("--ignore-authors", description: "Authors to ignore") { AllowMultipleArgumentsPerToken = true };
var IgnoreFiles = new Option<string[]>("--ignore-files", description: "Files to ignore") { AllowMultipleArgumentsPerToken = true };
var IgnoreDefaults = new Option<bool>("--ignore-defaults", description: "Ignore default found in .gitcontrib");

var root = new RootCommand($"Git Contrib v{Assembly.GetEntryAssembly()?.GetName().Version} gives statistics by authors to the project.") {
            Version,
            FromDate,
            ToDate,
            RepoPath,
            Mailmap,
            OutputFormat,
            HideSummary,
            IgnoreAuthors,
            IgnoreFiles,
            Metric,
            Reverse,
            AuthorLimit,
            IgnoreDefaults
        };

root.SetHandler(async (context) =>
{
    if (context.ParseResult.GetValueForOption(Version))
    {
        Console.WriteLine($"Git Contrib v{Assembly.GetEntryAssembly()?.GetName().Version}");
        return;
    }

    var path = context.ParseResult.GetValueForArgument(RepoPath);
    var byDay = context.ParseResult.GetValueForOption(Metric);
    var fromDate = context.ParseResult.GetValueForOption(FromDate);

    if (!await Utils.IsGitDirectoryAsync(path))
    {
        Console.WriteLine("Not a git directory: " + path);
        return;
    }

    var defaultOptions = new ConfigOptions(path);

    var options = new Options
    {
        ToDate = context.ParseResult.GetValueForOption(ToDate) ?? DateTimeOffset.Now,
        Metric = context.ParseResult.GetValueForOption(Metric),
        Path = context.ParseResult.GetValueForArgument(RepoPath),
        Format = context.ParseResult.GetValueForOption(OutputFormat),
        Reverse = context.ParseResult.GetValueForOption(Reverse),
        Mailmap = context.ParseResult.GetValueForOption(Mailmap) ?? "",
        AuthorLimit = context.ParseResult.GetValueForOption(AuthorLimit),
        HideSummary = context.ParseResult.GetValueForOption(HideSummary),
        IgnoreAuthors = context.ParseResult.GetValueForOption(IgnoreAuthors) ?? [],
        IgnoreFiles = context.ParseResult.GetValueForOption(IgnoreFiles) ?? [],
        IgnoreDefaults = context.ParseResult.GetValueForOption(IgnoreDefaults)
    };

    if (!options.IgnoreDefaults && !defaultOptions.IsEmpty)
    {
        options.MergeOptions(defaultOptions);
    }

    if (!fromDate.HasValue && string.IsNullOrWhiteSpace(defaultOptions.FromDate))
    {
        if (byDay.HasValue)
        {
            fromDate = DateTimeOffset.Now.AddDays(-6);
        }
        else
        {
            fromDate = fromDate = DateTimeOffset.MinValue;
        }
        options.FromDate = fromDate ?? DateTimeOffset.MinValue;
    }
    else if (fromDate.HasValue)
    {
        options.FromDate = fromDate.Value;
    }

    await Work.DoWork(options);
});

return await new CommandLineBuilder(root)
    .UseHelp()
    .UseSuggestDirective()
    .UseTypoCorrections()
    .RegisterWithDotnetSuggest()
    .Build()
    .InvokeAsync(args);
