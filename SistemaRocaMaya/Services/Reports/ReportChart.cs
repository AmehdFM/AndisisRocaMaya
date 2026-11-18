using System.Globalization;
using System.Security;
using System.Text;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportChartSeries(string Name, IReadOnlyList<decimal> Values);

public sealed record ReportChart(string Title, IReadOnlyList<string> Labels, IReadOnlyList<ReportChartSeries> Series,
    string CategoryAxisTitle)
{
    private static readonly string[] Colors = ["#245F8C", "#92531D", "#24724F"];
    private const int CategoriesPerPage = 6;
    private const int PlotLeft = 110;
    private const int PlotRight = 716;
    private const int PlotTop = 80;
    private const int PlotBottom = 212;

    public IEnumerable<string> RenderPages()
    {
        if (Series.Count == 0 || Series.Any(series => series.Values.Count != Labels.Count))
            throw new ArgumentException("Las series del gráfico deben coincidir con sus categorías.");
        if (Labels.Count == 0) yield break;

        var values = Series.SelectMany(series => series.Values).ToArray();
        var minimum = Math.Min(0m, values.Min());
        var maximum = Math.Max(0m, values.Max());
        if (maximum == minimum) maximum = 1m;
        for (var start = 0; start < Labels.Count; start += CategoriesPerPage)
            yield return Render(start, Math.Min(CategoriesPerPage, Labels.Count - start), minimum, maximum);
    }

    private string Render(int start, int count, decimal minimum, decimal maximum)
    {
        static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static string Escape(string value) => SecurityElement.Escape(value) ?? "";
        double Y(decimal value) => PlotTop + (double)((maximum - value) / (maximum - minimum)) * (PlotBottom - PlotTop);
        var valueStart = PlotBottom + 38;
        var graphicHeight = valueStart + Series.Count * 15 + 26;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 728 {graphicHeight}\" font-family=\"Lato\" font-size=\"12\" fill=\"#172B3A\">");
        var pageTitle = Labels.Count > CategoriesPerPage
            ? $"{Title} ({start / CategoriesPerPage + 1}/{(Labels.Count + CategoriesPerPage - 1) / CategoriesPerPage})"
            : Title;
        svg.Append($"<text x=\"0\" y=\"18\" font-size=\"16\" font-weight=\"700\">{Escape(pageTitle)}</text>");
        for (var index = 0; index < Series.Count; index++)
        {
            var x = index * 210;
            svg.Append($"<rect x=\"{x}\" y=\"32\" width=\"11\" height=\"11\" rx=\"2\" fill=\"{Colors[index % Colors.Length]}\"/><text x=\"{x + 17}\" y=\"42\">{Escape(Series[index].Name)}</text>");
        }

        svg.Append("<text x=\"4\" y=\"65\" font-size=\"11\" font-weight=\"700\">Monto (L)</text>");
        svg.Append($"<rect x=\"{PlotLeft}\" y=\"{PlotTop}\" width=\"{PlotRight - PlotLeft}\" height=\"{PlotBottom - PlotTop}\" fill=\"#F5F8FB\"/>");
        var ticks = minimum < 0m && maximum > 0m
            ? new[] { minimum, minimum / 2m, 0m, maximum / 2m, maximum }
            : Enumerable.Range(0, 5).Select(tick => minimum + (maximum - minimum) * tick / 4m).ToArray();
        foreach (var amount in ticks)
        {
            var y = Number(Y(amount));
            svg.Append($"<path d=\"M {PlotLeft - 5},{y} H {PlotRight}\" fill=\"none\" stroke=\"#C8D7E1\"/>");
            svg.Append($"<text x=\"{PlotLeft - 8}\" y=\"{Number(Y(amount) + 4)}\" text-anchor=\"end\">{Escape(OperationalDemoStore.FormatMoney(amount))}</text>");
        }

        var categoryWidth = (double)(PlotRight - PlotLeft) / count;
        var barWidth = Math.Min(26, categoryWidth / (Series.Count + 1));
        var zero = Y(0);
        for (var category = 0; category < count; category++)
        {
            var center = PlotLeft + categoryWidth * (category + 0.5);
            var centerText = Number(center);
            svg.Append($"<text x=\"{centerText}\" y=\"{PlotBottom + 20}\" text-anchor=\"middle\" font-weight=\"700\">{Escape(Labels[start + category])}</text>");
            for (var series = 0; series < Series.Count; series++)
            {
                var amount = Series[series].Values[start + category];
                var y = Y(amount);
                var x = center - barWidth * Series.Count / 2 + series * barWidth;
                var color = Colors[series % Colors.Length];
                svg.Append($"<rect x=\"{Number(x + 1)}\" y=\"{Number(Math.Min(y, zero))}\" width=\"{Number(barWidth - 2)}\" height=\"{Number(Math.Abs(y - zero))}\" fill=\"{color}\"/>");
                svg.Append($"<text x=\"{centerText}\" y=\"{valueStart + series * 15}\" text-anchor=\"middle\" fill=\"{color}\">{Escape(OperationalDemoStore.FormatMoney(amount))}</text>");
            }
        }
        svg.Append($"<path d=\"M {PlotLeft},{PlotTop} V {PlotBottom} H {PlotRight} M {PlotLeft},{Number(zero)} H {PlotRight}\" fill=\"none\" stroke=\"#526A7A\" stroke-width=\"1.25\"/>");
        svg.Append($"<text x=\"{(PlotLeft + PlotRight) / 2}\" y=\"{graphicHeight - 8}\" text-anchor=\"middle\" font-weight=\"700\">{Escape(CategoryAxisTitle)}</text></svg>");
        return svg.ToString();
    }
}
