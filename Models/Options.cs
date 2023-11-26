using System.Text.Json;
using Mapster;

public class Options
{
    public Options() { }
    public static Options Convert(ConfigOptions options)
    {
        // TODO: Convert this to Automapper or the codegen version
        var result = new Options
        {
            FromDate = Utils.TryParseHumanReadableDateTimeOffset(options.FromDate, out var _fromDate) ? _fromDate : DateTimeOffset.MinValue,
            ToDate = Utils.TryParseHumanReadableDateTimeOffset(options.ToDate, out var _toDate) ? _toDate : DateTimeOffset.Now,
            Path = options.Path,
            ByDay = options.ByDay,
            Mailmap = options.Mailmap,
            Format = options.Format,
            IgnoreAuthors = options.IgnoreAuthors,
            IgnoreFiles = options.IgnoreFiles,
        };
        return result;
    }
    public ByDay? ByDay { get; set; }
    public DateTimeOffset FromDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset ToDate { get; set; } = DateTimeOffset.Now;
    public string Path { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public bool ShowSummary { get; set; } = false;
    public Format Format { get; set; }
    public bool Fetch { get; set; } = true;
    public string[] IgnoreAuthors { get; set; } = [];
    public string[] IgnoreFiles { get; set; } = [];
}

public class ConfigOptions
{
    public ConfigOptions() { }
    public ConfigOptions(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (File.Exists(path))
            {
                var config = JsonSerializer.Deserialize<ConfigOptions>(File.ReadAllText(path), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
                if (config == null)
                {
                    throw new Exception("Config file found at: " + path + " but could not be deserialized.");
                }
                config.Adapt(this);
            }
            else
            {
                throw new Exception("Config file not found at: " + path);
            }
        }

    }
    public string FromDate { get; set; } = "";
    public ByDay? ByDay { get; set; }
    public string ToDate { get; set; } = "";
    public string Path { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; } = Format.Table;
    public string[] IgnoreAuthors { get; set; } = [];
    public string[] IgnoreFiles { get; set; } = [];

}

public enum ByDay {
    Lines,
    Commits,
    Files,
    LinesFlipped,
    CommitsFlipped,
    FilesFlipped,
}

public enum Format
{
    Table,
    Json,
    Chart,
    BarChart,
    None
}
