public class ChangeSet
{
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Files { get; set; }
    public int Commits { get; set; }
}

public class AuthorData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Dictionary<string, ChangeSet> ChangeMap { get; set; } = new Dictionary<string, ChangeSet>();
}

enum TableOption
{
    None,
    Lines,
    Files,
    Commits
    // Add other options as needed
}
