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
	"golang.org/x/text/language"
	"golang.org/x/text/message"
)

// ChangeSet stores the additions and deletions.
var debug bool = true

func run(path string, daysAgo time.Time) error {

	maxDates := int(math.Round(time.Now().Sub(daysAgo).Hours() / 24))
	var dates []string
	for i := 0; i < maxDates; i++ {
		date := daysAgo.AddDate(0, 0, i+1)
		dates = append(dates, date.Format("2006-01-02"))
	}

	var dateMap = arrayToMap(dates)

	gitCmd := "git"
	gitArgs := []string{"--no-pager", "log", "--all", "--summary", "--numstat", "--mailmap", "--topo-order", "--format=^%C(yellow)%h%C(reset) %C(green)%aI%C(reset) %C(red)%aN <%ae>%C(reset) %gs"}
	cmd := exec.Command(gitCmd, gitArgs...)

	//Change Dir
	cmd.Dir = path

	// Create a map of commit hashes
	commitMap := make(map[string]bool)

	// Get a pipe to read from standard output
	stdout, err := cmd.StdoutPipe()
	if err != nil {
		fmt.Println("Error creating StdoutPipe for Cmd", err)
		return nil
	}

	// Start running the command
	if err := cmd.Start(); err != nil {
		fmt.Println("Error starting Cmd", err)
		return nil
	}

	// Prepare a map to store the totals by author.
	totals := make(map[string]*AuthorData)

	// scanner := bufio.NewScanner(os.Stdin)
	scanner := bufio.NewScanner(stdout)

	var currentAuthor *AuthorData
	var currentDate string
	var skipUntilNextCommit bool
	for scanner.Scan() {
		line := scanner.Text()

		// Check if the line is empty.
		if strings.TrimSpace(line) == "" {
			continue
		}

		// Un the progress of skipping files until next commit
		if skipUntilNextCommit && !strings.HasPrefix(line, "^") {
			continue
		}

		if strings.HasPrefix(line, "^") {
			// Author information line.
			parts := strings.Fields(line)
			if len(parts) < 3 {
				return fmt.Errorf("Invalid author line format: %s", line)
			}

			// Checks that commit hash has not already been seen in other branches. Skip files until next commit
			commit := strings.Trim(parts[0], "^")
			if _, exists := commitMap[commit]; exists {
				skipUntilNextCommit = true
				continue
			}

			skipUntilNextCommit = false
			commitMap[commit] = true

			currentDate = strings.Split(parts[1], "T")[0]
			if !dateMap[currentDate] {
				// continue
				break
			}

			// Extract the email part of the author as identifier
			email := strings.Trim(parts[4], "<>")
			if _, exists := totals[email]; !exists {
				totals[email] = &AuthorData{
					Name:      parts[2] + " " + parts[3],
					ChangeMap: make(map[string]ChangeSet),
				}
			}
			currentAuthor = totals[email]
		} else if unicode.IsDigit(rune(line[0])) && currentAuthor != nil {
			// Totals line, starts with a number.
			parts := strings.Fields(line)
			if len(parts) < 2 {
				return fmt.Errorf("Invalid totals line format: %s", line)
			}

			// Parse additions and deletions.
			additions, err := strconv.Atoi(parts[0])
			if err != nil {
				return fmt.Errorf("Invalid number for additions: %s", parts[0])
			}
			deletions, err := strconv.Atoi(parts[1])
			if err != nil {
				return fmt.Errorf("Invalid number for deletions: %s", parts[1])
			}

			// Accumulate totals per date for the current author.
			currentAuthor.ChangeMap[currentDate] = ChangeSet{
				Additions: currentAuthor.ChangeMap[currentDate].Additions + additions,
				Deletions: currentAuthor.ChangeMap[currentDate].Deletions + deletions,
			}
		}
	}

	// Check for errors in the scanning.
	if err := scanner.Err(); err != nil {
		return fmt.Errorf("error reading standard input: %v", err)
	}

	// Print the totals.
	fmt.Printf("%-20s", "Aurhor's Name")
	for i := 0; i < maxDates; i++ {
		date := daysAgo.AddDate(0, 0, i+1)
		fmt.Printf("\t%s", date.Format("01/02"))
	}
	fmt.Print("\tTotal")
	fmt.Println()

	p := message.NewPrinter(language.English)

	// Print the totals in a tabular format.
	for _, authorData := range sortAuthorsByTotalChanges(totals) {
		fmt.Printf("%-20s", authorData.Name)
		var total int
		for i, date := range dates {
			if i >= maxDates {
				break
			}
			changes := authorData.ChangeMap[date]
			totalChanges := changes.Additions + changes.Deletions
			total = total + totalChanges
			p.Printf("\t%d", totalChanges)
		}
		p.Printf("\t%d", total)
		fmt.Println()
	}

	return nil
}

func getCurrentDirectory() string {
	dir, err := os.Getwd()
	if err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
	return dir
}

func main() {
	var (
		version       bool
		fromDate      string
		toDate        string
		byDay         bool
		mailmap       string
		format        string
		showSummary   bool
		ignoreAuthors []string
		ignoreFiles   []string
	)

	var rootCmd = &cobra.Command{
		Use: "git_contrib",
		Short: `git_contrib [path] gives statistics by authors to the project.

Positional Arguments:
path (optional)    Path to the directory. If not provided, defaults to the current directory.`,
		Run: func(cmd *cobra.Command, args []string) {
			var formattedFromDate time.Time
			if version {
				fmt.Println("Version: 1.0.0") // Replace with actual version
				return
			}

			path := "."
			if len(args) > 0 {
				path = args[0]
			}

			if fromDate == "" {
				formattedFromDate, _ = TryParseHumanReadableDateTimeOffset("week")
			} else {
				formattedFromDate, _ = TryParseHumanReadableDateTimeOffset(fromDate)
			}
			if toDate == "" {
				toDate = time.Now().Format(time.RFC3339)
			}

			fmt.Println(formattedFromDate)

			if !debug {
				if err := run(path, formattedFromDate); err != nil {
					fmt.Fprintf(os.Stderr, "%s\n", err)
					os.Exit(1)
				}
			}

		},
	}

	rootCmd.Flags().BoolVar(&version, "version", false, "Show the version information and exit")
	rootCmd.Flags().StringVar(&fromDate, "from", "", "Starting date for commits to be considered")
	rootCmd.Flags().StringVar(&toDate, "to", "", "Ending date for commits to be considered")
	rootCmd.Flags().BoolVar(&byDay, "by-day", false, "Show results by day")
	rootCmd.Flags().StringVar(&mailmap, "mailmap", "", "Path to mailmap file")
	rootCmd.Flags().StringVar(&format, "format", "table", "Format to output results in")
	rootCmd.Flags().BoolVar(&showSummary, "show-summary", false, "Show project summary details")
	rootCmd.Flags().StringSliceVar(&ignoreAuthors, "ignore-authors", nil, "Authors to ignore")
	rootCmd.Flags().StringSliceVar(&ignoreFiles, "ignore-files", nil, "Files to ignore")

	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		os.Exit(1)
	}
}
