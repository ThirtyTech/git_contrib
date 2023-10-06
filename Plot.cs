using System.Diagnostics;
using System.Reactive.Linq;
using ConsoleTables;

public static class CommitPlot
{
    public static async Task DoWorkAsync(Options options)
    {
        // Write code to call Work.DoWork in a loop for each day starting from FromDate and setting ToDate to 24 hours later.
        var days = (DateTime.Now - options.FromDate).Days;
        var result = await Observable.Start(() => Work.DoWork(new Options
        {
            Fetch = false,
            FromDate = options.FromDate,
            ToDate = options.ToDate,
            Path = options.Path,
            Mailmap = options.Mailmap,
            Format = global::Format.None,
        }));



        var rangeResults = await Observable.Range(0, days)
            .Select(i => Observable.Start(() => Work.DoWork(new Options
            {
                Fetch = false,
                FromDate = options.FromDate.AddDays(i),
                ToDate = options.FromDate.AddDays(i + 1),
                Path = options.Path,
                Mailmap = options.Mailmap,
                Format = global::Format.None,
                ShowSummary = options.ShowSummary
            })))
            .Merge(Environment.ProcessorCount - 1)
            .ToList();



        var table = new ConsoleTable("Author", "Files", "Commits", "Lines", "Trend");
        table.Options.EnableCount = false;
        foreach (var author in result)
        {
            var numbers = rangeResults.SelectMany(x => x.Where(y => y.Author == author.Author)).Select(y => y.Totals.Lines).ToArray();
            table.AddRow(author.Author,
                author.Totals.Files.ToString("N0"),
                author.Totals.Commits.ToString("N0"),
                author.Totals.Lines.ToString("N0"),
                Trend.GenerateTrendLine(numbers).PadLeft(8).PadRight(8)
            );
        }
        table.Write();

        // var commitPlot = new Plot();

        // commitPlot.Title("Lines Of Code");
        // // DateTime[] dates = Generate.DateTime.Days(days);
        // DateTime[] dates = Enumerable.Range(0, days).Select(i => options.FromDate.DateTime.AddDays(i)).ToArray();

        // // convert DateTime to OLE Automation (OADate) format
        // double[] xs = dates.Select(x => x.ToOADate()).ToArray();
        // // double[] ys = Generate.RandomWalk(xs.Length);
        // var ys = results.Select(x => x.Where(y => y.Author.StartsWith("Jonathan Myer"))
        // .Select(y => (double)y.Totals.Lines))
        // .SelectMany(x => x)
        // .ToList();

        // commitPlot.Add.Scatter(xs, ys);

        // // tell the plot to display dates on the bottom axis
        // commitPlot.AxisStyler.DateTimeTicks(Edge.Bottom);
        // commitPlot.SavePng("datetime-axis-quickstart.png", 3000, 3000);
    }
}
