using System.Text.Json;

public class Options
{
    public Options(ConfigOptions options)
    {
        // TODO: Conver this to Automapper or the codegen version
        FromDate = Utils.TryParseHumanReadableDateTimeOffset(options.FromDate, out var _fromDate) ? _fromDate : DateTimeOffset.MinValue;
        ToDate = Utils.TryParseHumanReadableDateTimeOffset(options.ToDate, out var _toDate) ? _toDate : DateTimeOffset.Now;
        Folder = options.Folder;
        Mailmap = options.Mailmap;
        Format = options.Format;
    }
    public DateTimeOffset FromDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset ToDate { get; set; } = DateTimeOffset.Now;
    public string Folder { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; }
}

public class ConfigOptions
{
    public ConfigOptions(string path)
    {
        if (File.Exists(path))
        {
            var config = JsonSerializer.Deserialize<ConfigOptions>(File.ReadAllText(path));
            if (config == null)
            {
                throw new Exception("Config file found at: " + path + " but could not be deserialized.");
            }
            FromDate = config.FromDate;
            ToDate = config.ToDate;
            Folder = config.Folder;
            Mailmap = config.Mailmap;
            Format = config.Format;
        }
        else
        {
            throw new Exception("Config file not found at: " + path);
        }

    }
    public string FromDate { get; set; } = "";
    public string ToDate { get; set; } = "";
    public string Folder { get; set; } = Directory.GetCurrentDirectory();
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; } = Format.Table;

}

public enum Format
{
    Table,
    Json
}