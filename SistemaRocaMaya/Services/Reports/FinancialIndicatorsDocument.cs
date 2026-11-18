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
        var groups = new[]
        {
            new IndicatorGroup("Rentabilidad de las ventas",
                "Muestra cuánto queda de cada L 100 vendidos. Si el margen cae, revisa si subió el costo de la mercadería o bajó el precio de venta.",
                indicators.Take(2).ToArray()),
            new IndicatorGroup("Capacidad de pago",
                "Compara la cobertura de las obligaciones corrientes con y sin inventario. Si la cobertura cae al excluirlo, revisa los cobros pendientes y los pagos próximos.",
                indicators.Skip(2).Take(2).ToArray()),
            new IndicatorGroup("Financiamiento y rendimiento",
                "Lee el endeudamiento junto con la rentabilidad del patrimonio. Si aumenta la deuda sin una mejora del rendimiento, revisa qué se está financiando con esos recursos.",
                indicators.Skip(4).ToArray())
        };

        container.Column(column =>
        {
            column.Item().Element(ComposeOverview);
            foreach (var group in groups.Where(group => group.Indicators.Count > 0))
            {
                column.Item().PageBreak();
                column.Item().Text(group.Title).FontSize(18).SemiBold();
                column.Item().PaddingTop(5).PaddingBottom(15).Text(group.Review);
                foreach (var indicator in group.Indicators)
                    column.Item().PaddingBottom(14).Element(body => ComposeIndicator(body, indicator));
            }
        });
    }

    private void ComposeOverview(IContainer container)
    {
        var currentLabel = cutoff.ToString("MMM yy", OperationalDemoStore.HondurasCulture);
        var previousLabel = cutoff.AddMonths(-1).ToString("MMM yy", OperationalDemoStore.HondurasCulture);

        container.Column(column =>
        {
            column.Item().Text("Lectura del período").FontSize(18).SemiBold();
            column.Item().PaddingTop(5).PaddingBottom(16)
                .Text("Consulta el valor al corte, su variación respecto al mes anterior y la referencia cuando aplica. Las páginas siguientes muestran la evolución de los últimos seis meses.");
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.25f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.25f);
                });
                table.Header(header =>
                {
                    foreach (var heading in new[] { "Indicador", previousLabel, currentLabel, "Variación", "Referencia" })
                    {
                        var cell = header.Cell().Background(PaleBlue).Padding(6);
                        if (heading != "Indicador") cell = cell.AlignRight();
                        cell.Text(heading).SemiBold();
                    }
                });

                foreach (var indicator in indicators)
                {
                    var previous = indicator.Months.Count > 1 ? indicator.Months[^2].Value : "—";
                    var current = indicator.Months.LastOrDefault()?.Value ?? "—";
                    var reference = indicator.ThresholdLabel.Replace("Referencia: ", "");
                    IContainer Cell() => table.Cell().BorderBottom(0.5f).BorderColor(Divider).Padding(6);
                    Cell().Text(indicator.Name);
                    Cell().AlignRight().Text(previous);
                    Cell().AlignRight().Text(current).SemiBold();
                    Cell().AlignRight().Text(FormatChange(indicator));
                    Cell().AlignRight().Text(reference.Length == 0 ? "—" : reference);
                }
            });

            column.Item().PaddingTop(18).BorderLeft(2).BorderColor(Blue).Background(PaleBlue).Padding(10).Column(notes =>
            {
                notes.Item().Text("Cómo interpretar la comparación").SemiBold();
                notes.Item().PaddingTop(5).Text("El mes actual incluye operaciones hasta la fecha de corte. El mes anterior corresponde al mes completo.");
                notes.Item().PaddingTop(5).Text("La variación se expresa en puntos porcentuales (pp) o veces. Un guion indica que falta un dato para el cálculo o que no hay referencia.");
                notes.Item().PaddingTop(5).Text("El resultado usa las ventas y el costo registrado de la mercadería. Al no incluir gastos operativos, intereses ni impuestos, el margen neto coincide con el margen bruto.");
            });
        });
    }

    private static void ComposeIndicator(IContainer container, AccountingIndicatorViewModel indicator)
    {
        container.Border(1).BorderColor(Divider).Padding(10).Column(section =>
        {
            section.Item().Row(row =>
            {
                row.RelativeItem().Text(indicator.Name).FontSize(13).SemiBold();
                row.AutoItem().PaddingLeft(10).Text(indicator.Value).FontSize(13).FontColor("#245F8C").SemiBold();
            });
            section.Item().PaddingTop(3).Text($"Variación frente al mes anterior: {FormatChange(indicator)}")
                .FontSize(9).FontColor("#526A7A");
            section.Item().PaddingTop(6).Svg(RenderTrend(indicator)).FitWidth();
        });
    }

    private static string FormatChange(AccountingIndicatorViewModel indicator)
    {
        if (indicator.Months.Count < 2 || indicator.Months[^2].Amount is not decimal previous
            || indicator.Months[^1].Amount is not decimal current)
            return "—";

        var change = current - previous;
        var unit = indicator.Unit == "%" ? "pp" : indicator.Unit;
        return $"{change.ToString("+0.00;-0.00;0.00", OperationalDemoStore.HondurasCulture)} {unit}";
    }

    private sealed record IndicatorGroup(string Title, string Review, IReadOnlyList<AccountingIndicatorViewModel> Indicators);

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
        if (indicator.Line.Figures.Count > 0)
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
