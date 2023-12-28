<p align="center">
<img src="./Assets/git_contrib_logo.jpg" width="250" />
</center>

# Git Contrib

The goal of this project is to give project owners insights into efforts contributed by authors.

## Installation

You can install using the releases and download the pre-built binary for your operating system. Otherwise you can build from source 
using the dotnet platform.

## Usage

`git_contrib --from "1 week" --by-day`

## Help

```text
Description:
  Git Contrib v0.2 gives statistics by authors to the project.

Usage:
  git_contrib [<path>] [command] [options]

Arguments:
  <path>  Path to search for git repositories [default: 
          /home/jsheely/Projects/ThirtyTech/git_contrib]

Options:
  --version                                     Show the version information and exit
  --from <from>                                 Starting date for commits to be considered
  --to <to>                                     Ending date for commits to be considered
  --mailmap <mailmap>                           Path to mailmap file
  --format <BarChart|Chart|Json|None|Table>     Format to output results in [default: Table]
  --show-summary                                Show project summary details
  --ignore-authors <ignore-authors>             Authors to ignore
  --ignore-files <ignore-files>                 Files to ignore
  --by-day                                      Show results by day
  <Commits|CommitsFlipped|Files|FilesFlipped|L
  ines|LinesFlipped>
  --reverse                                     Reverse the order of the results
  --limit <limit>                               Limit the number of authors to show
  -?, -h, --help                                Show help and usage information


Commands:
  config <path>  Configure defaults for the tool
```
