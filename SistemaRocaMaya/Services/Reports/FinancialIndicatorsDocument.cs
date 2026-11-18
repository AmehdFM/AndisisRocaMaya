using System.Globalization;
using System.Security;
using System.Text;
using System.Windows.Media;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Services.Reports;

public sealed class FinancialIndicatorsDocument(DateTime cutoff, IReadOnlyList<AccountingIndicatorViewModel> indicators)
    : ReportDocument("Resumen contable",
        $"Indicadores financieros · {cutoff.AddMonths(-5):MM/yyyy} – {cutoff:MM/yyyy} · Corte {cutoff:dd/MM/yyyy}")
{
    protected override void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            for (var index = 0; index < indicators.Count; index++)
            {
                if (index > 0 && index % 2 == 0) column.Item().PageBreak();
                var indicator = indicators[index];
                column.Item().PaddingBottom(18).Column(section =>
                {
                    section.Item().Row(row =>
                    {
                        row.RelativeItem().Text(indicator.Name).FontSize(14).SemiBold();
                        row.AutoItem().PaddingLeft(10).Text(indicator.Value).FontSize(14).FontColor(Blue).SemiBold();
                    });
                    section.Item().PaddingTop(8).Svg(RenderTrend(indicator)).FitWidth();
                    section.Item().PaddingTop(6).Text(indicator.Explanation);
                });
            }
        });
    }

    private static string RenderTrend(AccountingIndicatorViewModel indicator)
    {
        static string Escape(string text) => SecurityElement.Escape(text) ?? "";
        static string Number(double number) => number.ToString("0.##", CultureInfo.InvariantCulture);
        var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 720 205\" font-family=\"Lato\" font-size=\"12\" fill=\"#080818\">");
        svg.Append($"<text x=\"0\" y=\"12\">Unidad: {Escape(indicator.Unit)}</text>");
        svg.Append($"<text x=\"710\" y=\"12\" text-anchor=\"end\" fill=\"#9A5B23\">{Escape(indicator.ThresholdLabel)}</text>");
        svg.Append("<rect x=\"70\" y=\"28\" width=\"640\" height=\"120\" fill=\"#F5F8FB\"/>");
        var ticks = new[] { indicator.AxisTop, indicator.AxisMiddle, indicator.AxisBottom };
        for (var tick = 0; tick < ticks.Length; tick++)
        {
            var y = 28 + tick * 60;
            svg.Append($"<path d=\"M 66,{y} H 710\" fill=\"none\" stroke=\"#C8D7E1\"/><text x=\"64\" y=\"{y + 3}\" text-anchor=\"end\">{Escape(ticks[tick])}</text>");
        }
        svg.Append("<path d=\"M 70,28 V 148 H 710\" fill=\"none\" stroke=\"#526A7A\" stroke-width=\"1.25\"/>");
        svg.Append("<g transform=\"translate(70 28) scale(1.0666667 0.8)\">");
        if (indicator.ThresholdLine is { } reference)
            svg.Append($"<path d=\"M {Number(reference.StartPoint.X)},{Number(reference.StartPoint.Y)} L {Number(reference.EndPoint.X)},{Number(reference.EndPoint.Y)}\" fill=\"none\" stroke=\"#9A5B23\" stroke-dasharray=\"5 3\"/>");
        var path = indicator.Line.ToString(CultureInfo.InvariantCulture);
        svg.Append($"<path d=\"{Escape(path)}\" fill=\"none\" stroke=\"#245F8C\" stroke-width=\"2\"/>");
        foreach (var dot in indicator.Dots.Children.OfType<EllipseGeometry>())
            svg.Append($"<circle cx=\"{Number(dot.Center.X)}\" cy=\"{Number(dot.Center.Y)}\" r=\"3.5\" fill=\"#245F8C\"/>");
        svg.Append("</g>");
        for (var index = 0; index < indicator.Months.Count; index++)
        {
            var month = indicator.Months[index];
            var x = Number(70 + (index + 0.5) * 640 / indicator.Months.Count);
            svg.Append($"<text x=\"{x}\" y=\"167\" text-anchor=\"middle\" font-weight=\"700\">{Escape(month.Label)}</text>");
            svg.Append($"<text x=\"{x}\" y=\"182\" text-anchor=\"middle\" fill=\"#526A7A\">{Escape(month.Value)}</text>");
        }
        svg.Append("<text x=\"390\" y=\"201\" text-anchor=\"middle\" fill=\"#526A7A\">Mes contable</text></svg>");
        return svg.ToString();
    }
}
