using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportStatementRow(string Group, string Account, string Amount, bool IsTotal, bool IsSectionHeader);

public sealed class FinancialPositionDocument(DateTime cutoff, IReadOnlyList<ReportStatementRow> rows)
    : ReportDocument("Estado de Situación Financiera",
        $"Al cierre de {cutoff.ToString("MMMM yyyy", OperationalDemoStore.HondurasCulture)} · Corte {cutoff:dd/MM/yyyy}")
{
    protected override void ComposeContent(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2.2f);
                columns.RelativeColumn();
            });
            table.Header(header =>
            {
                header.Cell().Element(Heading).Text("Grupo");
                header.Cell().Element(Heading).Text("Cuenta");
                header.Cell().Element(Heading).AlignRight().Text("Saldo (L)");
            });

            foreach (var row in rows)
            {
                if (row.IsSectionHeader)
                {
                    table.Cell().ColumnSpan(3).EnsureSpace(70).Background(PaleBlue).Padding(7)
                        .Text(row.Group).FontColor(Blue).SemiBold();
                    continue;
                }

                IContainer Cell(IContainer cell) => cell.Background(row.IsTotal ? PaleBlue : "#FFFFFF")
                    .BorderBottom(0.5f).BorderColor(Divider).Padding(7)
                    .DefaultTextStyle(style => row.IsTotal ? style.SemiBold() : style);

                table.Cell().Element(Cell).Text(row.Group);
                table.Cell().Element(Cell).Text(row.Account);
                table.Cell().Element(Cell).AlignRight().Text(row.Amount);
            }
        });
    }

    private static IContainer Heading(IContainer container) => container.Background(PaleBlue)
        .BorderBottom(1).BorderColor(Blue).Padding(7).DefaultTextStyle(style => style.SemiBold());
}
