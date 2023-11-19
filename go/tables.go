package main

import (
	"fmt"
	"time"

	"github.com/dustin/go-humanize"
	"github.com/jedib0t/go-pretty/v6/table"
)

func printTableTotals(totals map[string]*AuthorData, showSummary bool) {
	tw := table.NewWriter()

	tw.AppendHeader(table.Row{"Author", "Commits", "Files", "Lines"})
	tw.SetIndexColumn(1)
	tw.SetTitle("Totals By Author")

	footer := table.Row{"Summary Totals"}
	var footerCounts []int
	if showSummary {
		footerCounts = make([]int, 3)
	}

	for _, authorData := range sortAuthorsByTotalChanges(totals) {
		var totalLines int
		var totalFiles int
		var totalCommits int
		for _, changes := range authorData.ChangeMap {
			totalChanges := changes.Additions + changes.Deletions
			totalLines += totalChanges
			totalFiles += changes.Files
			totalCommits += changes.Commits
		}
		if showSummary {
			footerCounts[0] += totalCommits
			footerCounts[1] += totalFiles
			footerCounts[2] += totalLines
		}
		tw.AppendRow(table.Row{authorData.Name, humanize.Comma(int64(totalCommits)), humanize.Comma(int64(totalFiles)), humanize.Comma(int64(totalLines))})
	}
	if showSummary {
		footer = append(footer, humanize.Comma(int64(footerCounts[0])))
		footer = append(footer, humanize.Comma(int64(footerCounts[1])))
		footer = append(footer, humanize.Comma(int64(footerCounts[2])))
		tw.AppendFooter(footer)
	}

	fmt.Println(tw.Render())
}

func printTableByDay(tableOption TableOption, maxDates int, daysAgo time.Time, totals map[string]*AuthorData, dates []string, showSummary bool) {
	tw := table.NewWriter()
	tw.SetTitle(fmt.Sprintf("Total %s By Author By Day", tableOption.ToString()))

	headers := table.Row{"Author's Name"}
	for i := 1; i < maxDates; i++ {
		date := daysAgo.AddDate(0, 0, i)
		headers = append(headers, date.Format("01/02"))
	}
	headers = append(headers, "Total")
	tw.AppendHeader(headers)

	footer := table.Row{"Summary Totals"}
	var footerCounts []int
	if showSummary {
		footerCounts = make([]int, maxDates)
	}

	for _, authorData := range sortAuthorsByTotalChanges(totals) {
		var total int
		row := table.Row{authorData.Name}

		for i, date := range dates {
			changes := authorData.ChangeMap[date]
			var totalChanges int
			switch tableOption {
			case Lines:
				totalChanges = changes.Additions + changes.Deletions
			case Files:
				totalChanges = changes.Files
			case Commits:
				totalChanges = changes.Commits
			}
			total = total + totalChanges
			row = append(row, humanize.Comma(int64(totalChanges)))
			if showSummary {
				footerCounts[i] += totalChanges
			}
		}
		row = append(row, humanize.Comma(int64(total)))
		tw.AppendRow(row)
	}
	if showSummary {
		var footerTotal int
		for _, count := range footerCounts {
			footerTotal += count
			footer = append(footer, humanize.Comma(int64(count)))
		}
		footer = append(footer, humanize.Comma(int64(footerTotal)))
		tw.AppendFooter(footer)
	}
	fmt.Println(tw.Render())
}
