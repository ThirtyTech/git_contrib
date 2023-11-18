package main

import (
	"fmt"
	"time"

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

func printTableByDay(maxDates int, daysAgo time.Time, totals map[string]*AuthorData, dates []string) {
	tw := table.NewWriter()
	tw.SetTitle("Totals By Author By Day")

	headers := table.Row{"Author's Name"}
	for i := 0; i < maxDates; i++ {
		date := daysAgo.AddDate(0, 0, i+1)
		headers = append(headers, date.Format("01/02"))
	}
	headers = append(headers, "Total")
	tw.AppendHeader(headers)

	for _, authorData := range sortAuthorsByTotalChanges(totals) {
		var total int
		row := table.Row{authorData.Name}
		for i, date := range dates {
			if i >= maxDates {
				break
			}
			changes := authorData.ChangeMap[date]
			totalChanges := changes.Additions + changes.Deletions
			total = total + totalChanges
			row = append(row, humanize.Comma(int64(totalChanges)))
		}
		row = append(row, humanize.Comma(int64(total)))
		tw.AppendRow(row)
	}
	fmt.Println(tw.Render())
}
