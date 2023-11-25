using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;


// var Version = new Option<bool>("--version", "Show the version information and exit");
// var From = new Option<string>("--from", "Starting date for commits to be considered");
// var To = new Option<int>("--to", getDefaultValue: () => 0, "Ending number of days for commits to be considered");
// var ByDay = new Option<string>("--by-day", "Show results by day [lines, commits, files]");
// var Format = new Option<string>("--format", getDefaultValue: () => "table", "Format to output results in");
// var Inverted = new Option<bool>("--inverted", "Invert authors and dates in table");
// var IgnoreAuthors = new Option<string[]>("--ignore-authors", "Authors to ignore");
// var IgnoreFiles = new Option<string[]>("--ignore-files", "Files to ignore");
// var Path = new Argument<string>("path", getDefaultValue: () => ".", "Path to the directory");
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
var ByDay = new Option<bool>("--by-day", description: "Show results by day");
var ShowSummary = new Option<bool>("--show-summary", description: "Show project summary details");
var IgnoreAuthors = new Option<string[]>("--ignore-authors", description: "Authors to ignore") { AllowMultipleArgumentsPerToken = true };
var IgnoreFiles = new Option<string[]>("--ignore-files", description: "Files to ignore") { AllowMultipleArgumentsPerToken = true };

var rootCommand = new RootCommand
{
    Version,
    FromDate,
    ToDate,
    ByDay,
    IgnoreAuthors,
    IgnoreFiles,
    Path
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

rootCommand.SetHandler(async (context) =>
{
    var version = context.ParseResult.GetValueForOption(Version);
    var path = context.ParseResult.GetValueForArgument(Path);
    if(version) {
        Console.WriteLine("0.1.0");
        return;
    }

    if(!await Utils.IsGitDirectoryAsync(path)) {
        Console.WriteLine("%s is not a git directory", path);
        return;
    }

});

return await new CommandLineBuilder(rootCommand)
    .UseHelp()
    .UseSuggestDirective()
    .UseTypoCorrections()
    .RegisterWithDotnetSuggest()
    .Build()
    .InvokeAsync(args);
