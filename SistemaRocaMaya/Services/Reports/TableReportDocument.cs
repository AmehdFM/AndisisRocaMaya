using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportColumn(string Title, float Width = 1, bool Numeric = false, bool Centered = false, string? Group = null);
public sealed record ReportRow(IReadOnlyList<string> Cells, bool IsTotal = false, bool StartsGroup = false);

public sealed class TableReportDocument : ReportDocument
{
    private readonly IReadOnlyList<ReportColumn> _columns;
    private readonly IReadOnlyList<ReportRow> _rows;
    private readonly IReadOnlyList<string> _notes;
    private readonly ReportChart? _chart;
    private readonly string? _chartSvg;
    private readonly ReportTableSummary? _summary;
    private readonly bool _groupSeparators;
    private readonly IReadOnlyList<ReportCallout> _chartCallouts;
    private readonly IReadOnlyList<ReportCallout> _headerCards;

    public TableReportDocument(string title, string context, IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<ReportRow> rows, bool landscape = false, IReadOnlyList<string>? notes = null,
        ReportChart? chart = null, ReportTableSummary? summary = null, bool groupSeparators = false,
        IReadOnlyList<ReportCallout>? chartCallouts = null, IReadOnlyList<ReportCallout>? headerCards = null,
        string? chartSvg = null) : base(title, context, landscape)
    {
        if (columns.Count == 0 || rows.Any(row => row.Cells.Count != columns.Count))
            throw new ArgumentException("Las filas deben coincidir con las columnas del reporte.");
        _columns = columns;
        _rows = rows;
        _notes = notes ?? [];
        _chart = chart;
        _chartSvg = chartSvg;
        _summary = summary;
        _groupSeparators = groupSeparators;
        _chartCallouts = chartCallouts ?? [];
        _headerCards = headerCards ?? [];
    }

    protected override void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            foreach (var note in _notes)
                column.Item().PaddingBottom(6).Text(note);

            if (_headerCards.Count > 0)
                column.Item().PaddingBottom(10).ShowEntire().Row(row =>
                {
                    foreach (var card in _headerCards)
                        row.RelativeItem().PaddingRight(4).Element(card.Compose);
                });

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var definition in _columns) columns.RelativeColumn(definition.Width);
                });
                table.Header(header =>
                {
                    if (_columns.Any(definition => definition.Group is not null))
                    {
                        var index = 0;
                        while (index < _columns.Count)
                        {
                            var group = _columns[index].Group;
                            var end = index + 1;
                            while (end < _columns.Count && _columns[end].Group == group) end++;
                            header.Cell().ColumnSpan((uint)(end - index)).Background(PaleBlue)
                                .BorderBottom(1).BorderColor(Divider).Padding(4).AlignCenter().Text(group ?? "").SemiBold();
                            index = end;
                        }
                    }

                    foreach (var definition in _columns)
                    {
                        var cell = header.Cell().Background(PaleBlue).BorderBottom(1).BorderColor(Blue)
                            .Padding(5).DefaultTextStyle(style => style.FontSize(9).SemiBold());
                        if (definition.Centered) cell = cell.AlignCenter();
                        else if (definition.Numeric) cell = cell.AlignRight();
                        cell.Text(definition.Title);
                    }
                });
                foreach (var row in _rows)
                {
                    for (var index = 0; index < _columns.Count; index++)
                    {
                        var cell = table.Cell().Background(row.IsTotal ? PaleBlue : "#FFFFFF")
                            .DefaultTextStyle(style => row.IsTotal ? style.FontSize(9).SemiBold() : style.FontSize(9));
                        if (_groupSeparators && row.StartsGroup)
                            cell = cell.BorderTop(1).BorderColor(Blue);
                        else if (!_groupSeparators)
                            cell = cell.BorderBottom(0.5f).BorderColor(Divider);
                        cell = cell.Padding(5);
                        if (_columns[index].Centered) cell = cell.AlignCenter();
                        else if (_columns[index].Numeric) cell = cell.AlignRight();
                        cell.Text(row.Cells[index]);
                    }
                }
            });
            if (_rows.Count == 0) column.Item().PaddingTop(8).Text("Sin registros para los criterios seleccionados.");

            if (_summary is not null)
                column.Item().PaddingTop(12).Element(_summary.Compose);

            if (_chart is not null)
            {
                // Split long month ranges into readable charts instead of shrinking their labels.
                foreach (var svg in _chart.RenderPages())
                    column.Item().PaddingTop(16).ShowEntire().Svg(svg).FitWidth();
            }

            if (_chartSvg is not null)
                column.Item().PaddingTop(16).ShowEntire().Svg(_chartSvg).FitWidth();

            if (_chartCallouts.Count > 0)
                column.Item().PaddingTop(10).ShowEntire().Row(row =>
                {
                    foreach (var callout in _chartCallouts)
                        row.RelativeItem().PaddingRight(4).Element(callout.Compose);
                });
        });
    }
}

public sealed record ReportTableSummary(string Title, IReadOnlyList<string> Headers, IReadOnlyList<ReportRow> Rows)
{
    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(5).Text(Title).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in Headers) columns.RelativeColumn();
                });
                table.Header(header =>
                {
                    foreach (var label in Headers)
                        header.Cell().Background(ReportSummaryBackground).Padding(6).AlignCenter().Text(label).SemiBold();
                });
                foreach (var row in Rows)
                {
                    foreach (var value in row.Cells)
                    {
                        var cell = table.Cell().Padding(6).AlignCenter();
                        if (row.IsTotal) cell.Text(value).SemiBold();
                        else cell.Text(value);
                    }
                }
            });
        });
    }

    private const string ReportSummaryBackground = "#EAF5FB";
}
