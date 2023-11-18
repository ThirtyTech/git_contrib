package main

type ChangeSet struct {
	Additions int
	Deletions int
}

// AuthorData holds the change sets by date for one author.
type AuthorData struct {
	Name      string
	ChangeMap map[string]ChangeSet
}
