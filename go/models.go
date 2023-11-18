package main

type ChangeSet struct {
	Additions int
	Deletions int
	Files     int
	Commits   int
}

// AuthorData holds the change sets by date for one author.
type AuthorData struct {
	Name      string
	Email     string
	ChangeMap map[string]ChangeSet
}
