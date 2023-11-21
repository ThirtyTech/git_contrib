package main

import (
	"fmt"
	"os"
	"strconv"
	"time"

	"github.com/spf13/cobra"
)

var debug bool
var log bool

func main() {
	var (
		version       bool
		fromDate      string
		toDate        int
		byDay         string
		inverted      bool
		format        string
		hideSummary   bool = true
		ignoreAuthors []string
		ignoreFiles   []string
	)

	debugEnv, exists := os.LookupEnv("DEBUG")
	if exists {
		debug, _ = strconv.ParseBool(debugEnv)
	}
	logEnv, exists := os.LookupEnv("LOG")
	if exists {
		log, _ = strconv.ParseBool(logEnv)
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
			if !IsGitDirectory(path) {
				fmt.Fprintf(os.Stderr, "%s is not a git directory\n", path)
				os.Exit(1)
			}

			if fromDate == "" {
				formattedFromDate = time.Date(1970, 1, 1, 0, 0, 0, 0, time.UTC)
			} else {
				formattedFromDate, _ = TryParseHumanReadableDateTimeOffset(fromDate)
			}

			byDayOption := ToTableOption(byDay)
			if !debug {
				if err := run(path, formattedFromDate, toDate, byDayOption, inverted, hideSummary, ignoreAuthors, ignoreFiles); err != nil {
					fmt.Fprintf(os.Stderr, "%s\n", err)
					os.Exit(1)
				}
			}
		},
	}

	rootCmd.Flags().BoolVar(&version, "version", false, "Show the version information and exit")
	rootCmd.Flags().StringVar(&fromDate, "from", "", "Starting date for commits to be considered")
	rootCmd.Flags().IntVar(&toDate, "to", 0, "Ending number of days for commits to be considered")
	rootCmd.Flags().StringVar(&byDay, "by-day", "", "Show results by day [lines, commits, files]")
	rootCmd.Flags().StringVar(&format, "format", "table", "Format to output results in")
	// rootCmd.Flags().BoolVar(&hideSummary, "hide-summary", false, "Hide project summary details")
	rootCmd.Flags().BoolVar(&inverted, "inverted", false, "Invert authors and dates in table")
	rootCmd.Flags().StringSliceVar(&ignoreAuthors, "ignore-authors", nil, "Authors to ignore")
	rootCmd.Flags().StringSliceVar(&ignoreFiles, "ignore-files", nil, "Files to ignore")

	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", err)
		os.Exit(1)
	}
}
