using System.CommandLine;
using System.Reflection;
using git_contrib;
using git_contrib.Models;

var repoPathArgument = new Argument<string>("path")
{
    Description = "Path to search for git repositories",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

// Keep human-readable parsing (e.g., "yesterday", "1w") by validating and parsing ourselves.
var fromOption = new Option<string?>("--from")
{
    Description = "Starting date for commits to be considered",
    Arity = ArgumentArity.ZeroOrOne
};
fromOption.Validators.Add(result =>
{
    if (result.Tokens.Count == 0)
    {
        return;
    }

    var input = result.Tokens.Single().Value;
    if (!Utils.TryParseHumanReadableDateTimeOffset(input, out _))
    {
        result.AddError($"Invalid value for --from: {input}");
    }
});

var toOption = new Option<string?>("--to")
{
    Description = "Ending date for commits to be considered",
    Arity = ArgumentArity.ZeroOrOne
};
toOption.Validators.Add(result =>
{
    if (result.Tokens.Count == 0)
    {
        return;
    }

    var input = result.Tokens.Single().Value;
    if (!Utils.TryParseHumanReadableDateTimeOffset(input, out _))
    {
        result.AddError($"Invalid value for --to: {input}");
    }
});

var metricOption = new Option<Metric?>("--metric")
{
    Description = "Which metric to show. Defaults to overall"
};

var swapAxesOption = new Option<bool>("--swap")
{
    Description = "Flip the axes of the output"
};

var mailmapOption = new Option<string>("--mailmap")
{
    Description = "Path to mailmap file",
    DefaultValueFactory = _ => "",
    Arity = ArgumentArity.ZeroOrOne
};

var outputFormatOption = new Option<Format>("--format")
{
    Description = "Format to output results in",
    DefaultValueFactory = _ => Format.Table
};

var hideSummaryOption = new Option<bool>("--hide-summary")
{
    Description = "Hide project summary details"
};

var reverseOption = new Option<bool>("--reverse")
{
    Description = "Reverse the order of the results"
};

var authorLimitOption = new Option<int?>("--limit")
{
    Description = "Limit the number of authors to show"
};

var ignoreAuthorsOption = new Option<string[]>("--ignore-authors")
{
    Description = "Authors to ignore",
    AllowMultipleArgumentsPerToken = true,
    DefaultValueFactory = _ => Array.Empty<string>()
};

var ignoreFilesOption = new Option<string[]>("--ignore-files")
{
    Description = "Files to ignore",
    AllowMultipleArgumentsPerToken = true,
    DefaultValueFactory = _ => Array.Empty<string>()
};

var ignoreDefaultsOption = new Option<bool>("--ignore-defaults")
{
    Description = "Ignore default found in .gitcontrib"
};

var excludeDaysOption = new Option<string?>("--exclude-days")
{
    Description = "Comma-separated days to completely exclude (e.g. su,sa). Counts are removed from totals.",
    Arity = ArgumentArity.ExactlyOne
};
excludeDaysOption.Validators.Add(result =>
{
    if (result.Tokens.Count == 0) return;
    try { Utils.ParseDaysList(result.Tokens.Single().Value); }
    catch (ArgumentException ex) { result.AddError(ex.Message); }
});

var hideDaysOption = new Option<string?>("--hide-days")
{
    Description = "Comma-separated days to hide from display but include in totals marked with * (e.g. su,sa)",
    Arity = ArgumentArity.ExactlyOne
};
hideDaysOption.Validators.Add(result =>
{
    if (result.Tokens.Count == 0) return;
    try { Utils.ParseDaysList(result.Tokens.Single().Value); }
    catch (ArgumentException ex) { result.AddError(ex.Message); }
});

var rootCommand = new RootCommand($"Git Contrib v{Assembly.GetEntryAssembly()?.GetName().Version} gives statistics by authors to the project.")
{
    repoPathArgument,
    fromOption,
    toOption,
    metricOption,
    swapAxesOption,
    mailmapOption,
    outputFormatOption,
    hideSummaryOption,
    reverseOption,
    authorLimitOption,
    ignoreAuthorsOption,
    ignoreFilesOption,
    ignoreDefaultsOption,
    excludeDaysOption,
    hideDaysOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(repoPathArgument) ?? Directory.GetCurrentDirectory();
    var metric = parseResult.GetValue(metricOption);
    var swapAxes = parseResult.GetValue(swapAxesOption);
    var outputFormat = parseResult.GetValue(outputFormatOption);
    var mailmap = parseResult.GetValue(mailmapOption) ?? "";
    var hideSummary = parseResult.GetValue(hideSummaryOption);
    var reverse = parseResult.GetValue(reverseOption);
    var authorLimit = parseResult.GetValue(authorLimitOption);
    var ignoreAuthors = parseResult.GetValue(ignoreAuthorsOption);
    var ignoreFiles = parseResult.GetValue(ignoreFilesOption);
    var ignoreDefaults = parseResult.GetValue(ignoreDefaultsOption);
    var excludeDaysRaw = parseResult.GetValue(excludeDaysOption);
    var hideDaysRaw = parseResult.GetValue(hideDaysOption);

    var excludeDays = !string.IsNullOrWhiteSpace(excludeDaysRaw) ? Utils.ParseDaysList(excludeDaysRaw) : Array.Empty<DayOfWeek>();
    var hideDays = !string.IsNullOrWhiteSpace(hideDaysRaw) ? Utils.ParseDaysList(hideDaysRaw) : Array.Empty<DayOfWeek>();

    var defaultOptions = new ConfigOptions(path);

    DateTimeOffset? fromDate = null;
    var fromValue = parseResult.GetValue(fromOption);
    if (!string.IsNullOrWhiteSpace(fromValue) && Utils.TryParseHumanReadableDateTimeOffset(fromValue, out var parsedFrom))
    {
        fromDate = parsedFrom;
    }

    var toDate = DateTimeOffset.Now;
    var toValue = parseResult.GetValue(toOption);
    if (!string.IsNullOrWhiteSpace(toValue) && Utils.TryParseHumanReadableDateTimeOffset(toValue, out var parsedTo))
    {
        toDate = parsedTo;
    }

    if (!await Utils.IsGitDirectoryAsync(path))
    {
        Console.WriteLine("Not a git directory: " + path);
        return 1;
    }

    var options = new Options
    {
        ToDate = toDate,
        Metric = metric,
        SwapAxes = swapAxes,
        Path = path,
        Format = outputFormat,
        Reverse = reverse,
        Mailmap = mailmap,
        AuthorLimit = authorLimit,
        HideSummary = hideSummary,
        IgnoreAuthors = ignoreAuthors ?? [],
        IgnoreFiles = ignoreFiles ?? [],
        IgnoreDefaults = ignoreDefaults,
        ExcludeDays = excludeDays,
        HideDays = hideDays
    };

    if (!options.IgnoreDefaults && !defaultOptions.IsEmpty)
    {
        options.MergeOptions(defaultOptions);
    }

    if (options.SwapAxes && options.Metric == null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Warning: --swap can only be used with metrics: commits, files, lines");
        Console.ResetColor();
    }

    if (!fromDate.HasValue && string.IsNullOrWhiteSpace(defaultOptions.FromDate))
    {
        options.FromDate = metric.HasValue ? DateTimeOffset.Now.AddDays(-6) : DateTimeOffset.MinValue;
    }
    else if (fromDate.HasValue)
    {
        options.FromDate = fromDate.Value;
    }

    await Work.DoWork(options);
    return 0;
});

return rootCommand.Parse(args).Invoke();
