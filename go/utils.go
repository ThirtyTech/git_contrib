package main

import (
	"sort"
	"strconv"
	"strings"
	"time"
)

func findNextMonday() time.Time {
	today := time.Now()
	daysUntilMonday := (7 - int(today.Weekday()) + 1) % 7
	// if daysUntilMonday == 0 { // If today is Monday, get the next Monday
	//     daysUntilMonday = 7
	// }
	nextMonday := today.AddDate(0, 0, daysUntilMonday)
	return nextMonday
}

func truncateDate(date time.Time) time.Time {
	year, month, day := date.Date()
	return time.Date(year, month, day, 0, 0, 0, 0, date.Location())
}

func TryParseHumanReadableDateTimeOffset(input string) (time.Time, bool) {
	if input == "" {
		return time.Time{}, false
	}

	parts := strings.Fields(input)

	now := time.Now()

	switch strings.ToLower(parts[0]) {
	case "week":
		return truncateDate(now.AddDate(0, 0, -7)), true
	case "workweek":
        fallthrough
	case "work":
		return truncateDate(findNextMonday()), true
	}

	quantity, err := strconv.Atoi(parts[0])
	if err != nil {
		return time.Time{}, false
	}

	switch strings.ToLower(parts[1]) {
	case "second", "seconds":
		return truncateDate(now.Add(time.Duration(-quantity) * time.Second)), true
	case "minute", "minutes":
		return truncateDate(now.Add(time.Duration(-quantity) * time.Minute)), true
	case "hour", "hours":
		return truncateDate(now.Add(time.Duration(-quantity) * time.Hour)), true
	case "day", "days":
		return truncateDate(now.AddDate(0, 0, -quantity)), true
	case "week", "weeks":
		return truncateDate(now.AddDate(0, 0, -quantity*7)), true
	case "month", "months":
		return truncateDate(now.AddDate(0, -quantity, 0)), true
	default:
		return time.Time{}, false
	}
}

func arrayToMap(keys []string) map[string]bool {
	m := make(map[string]bool)
	for _, key := range keys {
		m[key] = true
	}
	return m
}

func (ad *AuthorData) calculateTotalChanges() int {
	total := 0
	for _, changes := range ad.ChangeMap {
		total += changes.Additions + changes.Deletions
	}
	return total
}

func sortAuthorsByTotalChanges(authorsMap map[string]*AuthorData) []*AuthorData {
	var authors []*AuthorData
	for _, author := range authorsMap {
		authors = append(authors, author)
	}

	sort.SliceStable(authors, func(i, j int) bool {
		return authors[i].calculateTotalChanges() > authors[j].calculateTotalChanges()
	})

	return authors
}
