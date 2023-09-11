public class Options {
    public DateTimeOffset FromDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset ToDate { get; set; } = DateTimeOffset.Now;
    public string Folder { get; set; } = "";
    public string Mailmap { get; set; } = "";
    public Format Format { get; set; }
}

public enum Format {
    Json,
    Table
}