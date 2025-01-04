namespace git_contrib.Models;

public class ChangeSet
{
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Files => FileItems.Count;
    public int Commits { get; set; }
    public HashSet<string> FileItems { get; set; } = [];
    public int Lines => Additions + Deletions;
}

public class AuthorData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Dictionary<string, ChangeSet> ChangeMap { get; set; } = [];
    public int UniqueFiles => ChangeMap.SelectMany(x => x.Value.FileItems).Distinct().Count();
    public int TotalCommits => ChangeMap.Sum(x => x.Value.Commits);
    public int TotalLines => ChangeMap.Sum(x => x.Value.Lines);
}
