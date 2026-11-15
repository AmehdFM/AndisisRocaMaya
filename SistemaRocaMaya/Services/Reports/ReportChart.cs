using System.Globalization;
using System.Security;
using System.Text;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportChartSeries(string Name, IReadOnlyList<decimal> Values);

public sealed record ReportChart(string Title, IReadOnlyList<string> Labels, IReadOnlyList<ReportChartSeries> Series)
{
    private static readonly string[] Colors = ["#3888C0", "#9A5B23", "#287553"];

    public IEnumerable<string> RenderPages()
    {
        if (Series.Count == 0 || Series.Any(series => series.Values.Count != Labels.Count))
            throw new ArgumentException("Las series del gráfico deben coincidir con sus categorías.");
        for (var start = 0; start < Labels.Count; start += 12)
            yield return Render(start, Math.Min(12, Labels.Count - start));
    }

    private string Render(int start, int count)
    {
        var values = Series.SelectMany(series => series.Values.Skip(start).Take(count)).ToArray();
        var minimum = Math.Min(0m, values.Min());
        var maximum = Math.Max(0m, values.Max());
        if (maximum == minimum) maximum = minimum + 1m;
        double Y(decimal value) => 68 + (double)((maximum - value) / (maximum - minimum)) * 122;
        static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static string Escape(string value) => SecurityElement.Escape(value) ?? "";
        var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 720 240\" font-family=\"Lato\" font-size=\"10\">");
        svg.Append($"<text x=\"0\" y=\"16\" font-size=\"14\" fill=\"#080818\">{Escape(Title)}</text>");
        for (var index = 0; index < Series.Count; index++)
        {
            var x = index * 210;
            svg.Append($"<rect x=\"{x}\" y=\"29\" width=\"9\" height=\"9\" fill=\"{Colors[index % Colors.Length]}\"/><text x=\"{x + 14}\" y=\"37\">{Escape(Series[index].Name)}</text>");
        }
        for (var tick = 0; tick <= 4; tick++)
        {
            var amount = minimum + (maximum - minimum) * tick / 4;
            var y = Number(Y(amount));
            svg.Append($"<path d=\"M 78,{y} H 712\" fill=\"none\" stroke=\"#D8E5ED\"/><text x=\"72\" y=\"{Number(Y(amount) + 3)}\" text-anchor=\"end\">{Escape(OperationalDemoStore.FormatMoney(amount))}</text>");
        }
        var categoryWidth = 634d / count;
        var barWidth = Math.Min(24, categoryWidth / (Series.Count + 1));
        for (var category = 0; category < count; category++)
        {
            var center = 78 + categoryWidth * (category + 0.5);
            svg.Append($"<text x=\"{Number(center)}\" y=\"210\" text-anchor=\"middle\">{Escape(Labels[start + category])}</text>");
            for (var series = 0; series < Series.Count; series++)
            {
                var y = Y(Series[series].Values[start + category]);
                var zero = Y(0);
                var x = center - barWidth * Series.Count / 2 + series * barWidth;
                svg.Append($"<rect x=\"{Number(x)}\" y=\"{Number(Math.Min(y, zero))}\" width=\"{Number(barWidth - 2)}\" height=\"{Number(Math.Abs(y - zero))}\" fill=\"{Colors[series % Colors.Length]}\"/>");
            }
        }
        svg.Append($"<path d=\"M 78,68 V 190 M 78,{Number(Y(0))} H 712\" fill=\"none\" stroke=\"#526A7A\"/></svg>");
        return svg.ToString();
    }
}
