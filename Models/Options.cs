public class Options {
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public string Folder { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; }
}

public enum Format {
    Json,
    Table
}