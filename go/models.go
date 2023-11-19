package main

import "strings"

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

type TableOption int
type TableOptions = struct {
	None    TableOption
	Lines   TableOption
	Files   TableOption
	Commits TableOption
}

const (
	None TableOption = iota
	Lines
	Files
	Commits
)

func (t TableOption) ToString() string {
	switch t {
	case Lines:
		return "Lines"
	case Files:
		return "Files"
	case Commits:
		return "Commits"
	default:
		return "None"
	}
}

func ToTableOption(s string) TableOption {
	switch strings.ToLower(s) {
	case "lines":
		return Lines
	case "files":
		return Files
	case "commits":
		return Commits
	default:
		return None
	}
}
