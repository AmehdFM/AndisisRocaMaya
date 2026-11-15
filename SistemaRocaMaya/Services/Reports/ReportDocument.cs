using System.IO;
using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public abstract class ReportDocument : IDocument
{
    protected const string Ink = "#080818";
    protected const string Blue = "#3888C0";
    protected const string PaleBlue = "#EAF5FB";
    protected const string Divider = "#C8DCE8";
    private readonly string _title;
    private readonly string _period;

    protected ReportDocument(string title, string period)
    {
        _title = title;
        _period = period;
    }

    public DocumentMetadata GetMetadata() => new() { Title = _title, Author = "Consul Farma Roca Maya" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(10).FontColor(Ink));
            page.Header().PaddingBottom(18).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(_title).FontSize(19).SemiBold();
                    row.ConstantItem(105).Svg(LoadLogo()).FitWidth();
                });
                column.Item().PaddingTop(6).Text(_period).FontSize(10).FontColor(Blue);
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Divider);
            });
            page.Content().Element(ComposeContent);
            page.Footer().PaddingTop(12).BorderTop(1).BorderColor(Divider).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Consul Farma Roca Maya").FontSize(8).FontColor(Blue);
                row.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    protected abstract void ComposeContent(IContainer container);

    private static string LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SistemaRocaMaya.Assets.ReportWordmark.svg")
            ?? throw new InvalidOperationException("No se encontró el logotipo del reporte.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
