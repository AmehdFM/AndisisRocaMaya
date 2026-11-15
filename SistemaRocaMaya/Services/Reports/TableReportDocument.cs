using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportColumn(string Title, float Width = 1, bool Numeric = false);
public sealed record ReportRow(IReadOnlyList<string> Cells, bool IsTotal = false);

public sealed class TableReportDocument : ReportDocument
{
    private readonly IReadOnlyList<ReportColumn> _columns;
    private readonly IReadOnlyList<ReportRow> _rows;
    private readonly IReadOnlyList<string> _notes;
    private readonly ReportChart? _chart;

    public TableReportDocument(string title, string context, IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<ReportRow> rows, bool landscape = false, IReadOnlyList<string>? notes = null,
        ReportChart? chart = null) : base(title, context, landscape)
    {
        if (columns.Count == 0 || rows.Any(row => row.Cells.Count != columns.Count))
            throw new ArgumentException("Las filas deben coincidir con las columnas del reporte.");
        _columns = columns;
        _rows = rows;
        _notes = notes ?? [];
        _chart = chart;
    }

    protected override void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            foreach (var note in _notes)
                column.Item().PaddingBottom(6).Text(note);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var definition in _columns) columns.RelativeColumn(definition.Width);
                });
                table.Header(header =>
                {
                    foreach (var definition in _columns)
                    {
                        var cell = header.Cell().Background(PaleBlue).BorderBottom(1).BorderColor(Blue)
                            .Padding(5).DefaultTextStyle(style => style.FontSize(9).SemiBold());
                        if (definition.Numeric) cell = cell.AlignRight();
                        cell.Text(definition.Title);
                    }
                });
                foreach (var row in _rows)
                {
                    for (var index = 0; index < _columns.Count; index++)
                    {
                        var cell = table.Cell().Background(row.IsTotal ? PaleBlue : "#FFFFFF")
                            .BorderBottom(0.5f).BorderColor(Divider).Padding(5)
                            .DefaultTextStyle(style => row.IsTotal ? style.FontSize(9).SemiBold() : style.FontSize(9));
                        if (_columns[index].Numeric) cell = cell.AlignRight();
                        cell.Text(row.Cells[index]);
                    }
                }
            });
            if (_rows.Count == 0) column.Item().PaddingTop(8).Text("Sin registros para los criterios seleccionados.");

            if (_chart is not null)
            {
                // Split long month ranges into readable charts instead of shrinking their labels.
                foreach (var svg in _chart.RenderPages())
                    column.Item().PaddingTop(16).ShowEntire().Svg(svg).FitWidth();
            }
        });
    }
}
