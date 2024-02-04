using System.Text.Json;
using System.Text.Json.Serialization;
using Mapster;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            Metric = options.Metric,
            Mailmap = options.Mailmap,
            Format = options.Format,
            IgnoreAuthors = options.IgnoreAuthors,
            IgnoreFiles = options.IgnoreFiles,
        };
        return result;
    }
    public Metric? Metric { get; set; }
    public DateTimeOffset FromDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset ToDate { get; set; } = DateTimeOffset.Now;
    public string Path { get; set; } = string.Empty;
    public string Mailmap { get; set; } = string.Empty;
    public int? AuthorLimit { get; set; }
    public bool HideSummary { get; set; }
    public bool Reverse { get; set; }
    public Format Format { get; set; }
    public bool Fetch { get; set; }
    public string[] IgnoreAuthors { get; set; } = [];
    public string[] IgnoreFiles { get; set; } = [];
    public bool IgnoreDefaults { get; set; }
    public void MergeOptions(ConfigOptions configOptions)
    {
        if (configOptions == null || configOptions.IsEmpty)
        {
            return;
        }

        var _converted = Convert(configOptions);

        var optionsProperties = typeof(Options).GetProperties();

        foreach (var configProperty in optionsProperties)
        {
            var optionsProperty = optionsProperties.FirstOrDefault(p => p.Name == configProperty.Name);
            if (optionsProperty != null && optionsProperty.CanWrite)
            {
                var optionsValue = optionsProperty.GetValue(this);
                if (optionsValue == null ||
                    optionsValue.Equals(optionsProperty.PropertyType.IsValueType ? Activator.CreateInstance(optionsProperty.PropertyType) : null) ||
                    (optionsProperty.PropertyType == typeof(string) && string.IsNullOrEmpty((string)optionsValue)) || optionsValue is Array)
                {
                    var configValue = configProperty.GetValue(_converted);
                    if (configValue == null ||
                        configValue.Equals(configProperty.PropertyType.IsValueType ? Activator.CreateInstance(configProperty.PropertyType) : null) ||
                        (configProperty.PropertyType == typeof(string) && string.IsNullOrEmpty((string)configValue)) ||
                        (configValue is Array configArray && configArray.Length == 0))
                    {
                        continue;
                    }
                    else if (configValue is Array configArray2 && optionsProperty.GetValue(this) is Array optionsArray2)
                    {
                        var mergedArray = Array.CreateInstance(optionsProperty.PropertyType.GetElementType()!, configArray2.Length + optionsArray2.Length);
                        Array.Copy(optionsArray2, mergedArray, optionsArray2.Length);
                        Array.Copy(configArray2, 0, mergedArray, optionsArray2.Length, configArray2.Length);
                        optionsProperty.SetValue(this, mergedArray);
                    }
                    else
                    {
                        optionsProperty.SetValue(this, configValue);
                    }
                }
            }
        }
    }
}

public class ConfigOptions
{
    public ConfigOptions() { }
    public ConfigOptions(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (!path.Contains(".gitcontrib"))
            {
                path = System.IO.Path.Combine(path, ".gitcontrib");
            }
            if (!File.Exists(path))
            {
                IsEmpty = true;
                return;
            }
            // Get File extention or no file extension in the case of yaml
            var ext = System.IO.Path.GetExtension(path);

            // Check if file is valid yaml or json
            if (ext == ".yaml" || ext == ".yml" || ext == ".gitcontrib")
            {
                // Deserialize yaml
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                var yaml = File.ReadAllText(path);
                var config = deserializer.Deserialize<ConfigOptions>(yaml);
                if (config == null)
                {
                    throw new Exception("Config file found at: " + path + " but could not be deserialized.");
                }
                config.Adapt(this);
            }
            else if (ext == ".json")
            {
                // Deserialize json
                var config = JsonSerializer.Deserialize<ConfigOptions>(File.ReadAllText(path), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });
                if (config == null)
                {
                    throw new Exception("Config file found at: " + path + " but could not be deserialized.");
                }
                config.Adapt(this);
            }
        }

    }
    public string FromDate { get; set; } = "";
    public Metric? Metric { get; set; }
    public string ToDate { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; } = Format.Table;
    public string[] IgnoreAuthors { get; set; } = [];
    public string[] IgnoreFiles { get; set; } = [];
    public bool IsEmpty { get; }
}

public enum Metric
{
    All,
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
