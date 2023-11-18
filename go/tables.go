package main

import (
	"fmt"

	"github.com/dustin/go-humanize"
	"github.com/jedib0t/go-pretty/v6/table"
)

func printTableTotals(totals map[string]*AuthorData) {
	tw := table.NewWriter()

	tw.AppendHeader(table.Row{"Author", "Commits", "Files", "Lines"})
	tw.SetIndexColumn(1)
	tw.SetTitle("Totals By Author")

	for _, authorData := range sortAuthorsByTotalChanges(totals) {
		var totalLines int
		var totalFiles int
		var totalCommits int
		for _, changes := range authorData.ChangeMap {
			totalChanges := changes.Additions + changes.Deletions
			totalLines = totalLines + totalChanges
			totalFiles = totalFiles + changes.Files
			totalCommits = totalCommits + changes.Commits
		}
		tw.AppendRow(table.Row{authorData.Name, humanize.Comma(int64(totalCommits)), humanize.Comma(int64(totalFiles)), humanize.Comma(int64(totalLines))})
	}
	fmt.Println(tw.Render())
}
