public class AuthorContrib
{
	public string AuthorName { get; set; } = "";
	public int Commits { get; set; }
	// TODO: Break down files by further details. Extensions, etc
	public int Files { get; set; }
	public int Lines { get; set; }
}