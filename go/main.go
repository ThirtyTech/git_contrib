package main

import (
	"bufio"
	"fmt"
	"math"
	"os"
	"os/exec"
	"strconv"
	"strings"
	"time"
	"unicode"

	"github.com/spf13/cobra"
)

var debug bool

func run(path string, daysAgo time.Time, toDate int, byDay bool, showSummary bool, ignoreAuthors []string) error {

	maxDates := int(math.Round(time.Now().Sub(daysAgo).Hours() / 24))
	if toDate > 0 {
		maxDates = maxDates - toDate - 1
	}
	var dates []string
	for i := 1; i < maxDates; i++ {
		date := daysAgo.AddDate(0, 0, i)
		dates = append(dates, date.Format("2006-01-02"))
	}

	var dateMap = arrayToMap(dates)

	gitCmd := "git"
	gitArgs := []string{"--no-pager", "log", "--all", "--summary", "--numstat", "--mailmap", "--no-merges", "--since", daysAgo.Format("2006-01-02"), "--format=^%h|%aI|%aN|<%aE>"}
	if toDate > 0 {
		gitArgs = append(gitArgs, "--until", daysAgo.AddDate(0, 0, toDate).Format("2006-01-02"))
	}
	cmd := exec.Command(gitCmd, gitArgs...)

	cmd.Dir = path

	commitMap := make(map[string]bool)

	stdout, err := cmd.StdoutPipe()
	if err != nil {
		fmt.Println("Error creating StdoutPipe for Cmd", err)
		return nil
	}

	if err := cmd.Start(); err != nil {
		fmt.Println("Error starting Cmd", err)
		return nil
	}

	totals := make(map[string]*AuthorData)

	scanner := bufio.NewScanner(stdout)

	var currentAuthor *AuthorData
	var currentDate string
	var skipUntilNextCommit bool
	for scanner.Scan() {
		line := scanner.Text()

		if strings.TrimSpace(line) == "" {
			continue
		}

		if skipUntilNextCommit && !strings.HasPrefix(line, "^") {
			continue
		}

		if strings.HasPrefix(line, "^") {
			parts := strings.Split(line, "|")
			if len(parts) < 3 {
				return fmt.Errorf("Invalid author line format: %s", line)
			}

			commit := strings.Trim(parts[0], "^")
			if _, exists := commitMap[commit]; exists {
				skipUntilNextCommit = true
				continue
			}

			skipUntilNextCommit = false
			commitMap[commit] = true

			currentDate = strings.Split(parts[1], "T")[0]
			if !dateMap[currentDate] {
				continue
			}

			var authorName = parts[2]

			if len(ignoreAuthors) > 0 {
				skip := false
				for _, author := range ignoreAuthors {
					if strings.Contains(strings.ToLower(authorName), strings.ToLower(author)) {
						skip = true
					}
				}
				if skip {
					continue
				}
			}

			email := strings.Trim(parts[3], "<>")
			email = strings.ToLower(email)
			if _, exists := totals[email]; !exists {
				totals[email] = &AuthorData{
					Name:  authorName,
					Email: email,
					ChangeMap: map[string]ChangeSet{
						currentDate: {
							Commits: 1,
						},
					},
				}
			} else {
				if changeSet, ok := totals[email].ChangeMap[currentDate]; ok {
					changeSet.Commits++
					totals[email].ChangeMap[currentDate] = changeSet
				} else {
					totals[email].ChangeMap[currentDate] = ChangeSet{
						Commits: 1,
					}
				}
			}
			currentAuthor = totals[email]
		} else if unicode.IsDigit(rune(line[0])) && currentAuthor != nil {
			parts := strings.Fields(line)
			if len(parts) < 2 {
				return fmt.Errorf("Invalid totals line format: %s", line)
			}

			additions, err := strconv.Atoi(parts[0])
			if err != nil {
				return fmt.Errorf("Invalid number for additions: %s", parts[0])
			}
			deletions, err := strconv.Atoi(parts[1])
			if err != nil {
				return fmt.Errorf("Invalid number for deletions: %s", parts[1])
			}

			if changeSet, ok := currentAuthor.ChangeMap[currentDate]; ok {
				changeSet.Additions += additions
				changeSet.Deletions += deletions
				changeSet.Files++
				currentAuthor.ChangeMap[currentDate] = changeSet
			}
		}
	}

	if err := scanner.Err(); err != nil {
		return fmt.Errorf("error reading standard input: %v", err)
	}

	if byDay {
		printTableByDay(maxDates, daysAgo, totals, dates, showSummary)
	} else {
		printTableTotals(totals, showSummary)
	}

	return nil
}

func main() {
	var (
		version       bool
		fromDate      string
		toDate        int
		byDay         bool
		format        string
		showSummary   bool
		ignoreAuthors []string
		ignoreFiles   []string
	)

	debugEnv, exists := os.LookupEnv("DEBUG")
	if exists {
		debug, _ = strconv.ParseBool(debugEnv)
	}

	var rootCmd = &cobra.Command{
		Use: "git_contrib",
		Short: `git_contrib [path] gives statistics by authors to the project.

Positional Arguments:
path (optional)    Path to the directory. If not provided, defaults to the current directory.`,
		Run: func(cmd *cobra.Command, args []string) {
			var formattedFromDate time.Time
			if version {
				return
			}

			path := "."
			if len(args) > 0 {
				path = args[0]
			}
			//TODO: Check if path is a valid git directory

			if fromDate == "" {
				formattedFromDate = time.Date(1970, 1, 1, 0, 0, 0, 0, time.UTC)
			} else {
				formattedFromDate, _ = TryParseHumanReadableDateTimeOffset(fromDate)
			}

			if !debug {
				if err := run(path, formattedFromDate, toDate, byDay, showSummary, ignoreAuthors); err != nil {
					fmt.Fprintf(os.Stderr, "%s\n", err)
					os.Exit(1)
				}
			}
		},
	}

	rootCmd.Flags().BoolVar(&version, "version", false, "Show the version information and exit")
	rootCmd.Flags().StringVar(&fromDate, "from", "", "Starting date for commits to be considered")
	rootCmd.Flags().IntVar(&toDate, "to", 0, "Ending number of days for commits to be considered")
	rootCmd.Flags().BoolVar(&byDay, "by-day", false, "Show results by day")
	rootCmd.Flags().StringVar(&format, "format", "table", "Format to output results in")
	rootCmd.Flags().BoolVar(&showSummary, "show-summary", false, "Show project summary details")
	rootCmd.Flags().StringSliceVar(&ignoreAuthors, "ignore-authors", nil, "Authors to ignore")
	rootCmd.Flags().StringSliceVar(&ignoreFiles, "ignore-files", nil, "Files to ignore")

	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		os.Exit(1)
	}
}
