using System.Printing;
using System.Windows;
using System.Windows.Controls;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public static class ReportPrintService
{
    public static void Print(Func<ReportDocument> createDocument, Action<string> setStatus)
    {
        try
        {
            var report = createDocument();
            QuestPDF.Settings.License = LicenseType.Community;
            // QuestPDF paginates the same document used for PDF export.
            // WPF prints its rendered pages at 300 dpi without an external PDF viewer.
            var images = report.GenerateImages(new ImageGenerationSettings { RasterDpi = 300 }).ToArray();
            if (images.Length == 0) throw new InvalidOperationException("El reporte no contiene páginas.");

            var dialog = new PrintDialog
            {
                UserPageRangeEnabled = true,
                MinPage = 1,
                MaxPage = (uint)images.Length,
                PageRange = new PageRange(1, images.Length),
                PrintTicket = new PrintTicket
                {
                    PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetter),
                    PageOrientation = report.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait
                }
            };
            if (dialog.ShowDialog() != true) return;

            var capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
            var landscape = dialog.PrintTicket.PageOrientation is PageOrientation.Landscape or PageOrientation.ReverseLandscape;
            var paper = dialog.PrintTicket.PageMediaSize;
            var width = paper?.Width ?? 816;
            var height = paper?.Height ?? 1056;
            var pageSize = new System.Windows.Size(
                capabilities.OrientedPageMediaWidth ?? (landscape ? height : width),
                capabilities.OrientedPageMediaHeight ?? (landscape ? width : height));
            var area = capabilities.PageImageableArea;
            var printable = area is null
                ? new Rect(0, 0, dialog.PrintableAreaWidth, dialog.PrintableAreaHeight)
                : new Rect(area.OriginWidth, area.OriginHeight, area.ExtentWidth, area.ExtentHeight);
            var range = dialog.PageRangeSelection == PageRangeSelection.UserPages
                ? dialog.PageRange : new PageRange(1, images.Length);
            var paginator = new ReportPaginator(images, pageSize, printable, range.PageFrom, range.PageTo);
            dialog.PrintDocument(paginator, report.GetMetadata().Title ?? "Reporte Roca Maya");
            setStatus("Reporte enviado a la cola de impresión.");
        }
        catch (Exception exception)
        {
            var message = $"No se pudo imprimir el reporte. {exception.Message}";
            setStatus(message);
            MessageBox.Show(message, "Imprimir reporte", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
