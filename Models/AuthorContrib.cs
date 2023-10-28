using System.Text.Json.Serialization;
using LibGit2Sharp;

public class AuthorContrib
{
    public string Author { get; set; } = "";
    // Path to project. Used when multiple repositories are being processed in the future.
    public string Project { get; set; } = "";
    // TODO: Break down files by further details. Extensions, etc
    public Totals Totals { get; set; } = new();
    public List<Totals> TotalsByDate { get; set; } = new();
    [JsonIgnore]
    public List<Commit> Commits { get; set; } = new();
}

public class Totals
{

    public DateOnly Date { get; set; }
    public int Files { get; set; }
    public int Lines { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public int Commits { get; set; }
}
