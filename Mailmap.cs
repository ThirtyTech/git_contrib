using System.Collections;

public class Mailmap : IDictionary<string, string>
{
	private readonly Dictionary<string, string> _mailmap = new Dictionary<string, string>();

	public Mailmap(string path)
	{
		foreach (var line in File.ReadLines(Path.Combine(path, ".mailmap")))
		{
			// Ignore comments and empty lines
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
			{
				continue;
			}

			string[] parts = line.Split(new string[] { "> " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			// TODO: Implement the other variations of mailmap. See https://git-scm.com/docs/gitmailmap
			if (parts.Length == 2)
			{
				_mailmap[parts[1] + '>'] = parts[0] + '>';
			}
		}
	}

	public string? Validate(string author)
	{
		if (_mailmap.TryGetValue(author, out var correctAuthor))
			return correctAuthor;

		return author;
	}

	public string this[string key]
	{
		get => _mailmap[key];
		set => _mailmap[key] = value;
	}

	public ICollection<string> Keys => _mailmap.Keys;

	public ICollection<string> Values => _mailmap.Values;

	public int Count => _mailmap.Count;

	public bool IsReadOnly => false;


	public void Add(string key, string value)
	{
		_mailmap.Add(key, value);
	}

	public void Add(KeyValuePair<string, string> item)
	{
		_mailmap.Add(item.Key, item.Value);
	}

	public void Clear()
	{
		_mailmap.Clear();
	}

	public bool Contains(KeyValuePair<string, string> item)
	{
		return _mailmap.Contains(item);
	}

	public bool ContainsKey(string key)
	{
		return _mailmap.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		((IDictionary<string, string>)_mailmap).CopyTo(array, arrayIndex);
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _mailmap.GetEnumerator();
	}

	public bool Remove(string key)
	{
		return _mailmap.Remove(key);
	}

	public bool Remove(KeyValuePair<string, string> item)
	{
		return ((IDictionary<string, string>)_mailmap).Remove(item);
	}

	public bool TryGetValue(string key, out string value)
	{
		return _mailmap.TryGetValue(key, out value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _mailmap.GetEnumerator();
	}
}
