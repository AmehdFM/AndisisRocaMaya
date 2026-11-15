using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SistemaRocaMaya.Services.Reports;

public sealed class ReportPaginator : DocumentPaginator
{
    private readonly IReadOnlyList<byte[]> _pages;
    private readonly Rect _printableArea;
    private readonly int _firstPage;

    public ReportPaginator(IReadOnlyList<byte[]> pages, Size pageSize, Rect printableArea, int from, int to)
    {
        if (from < 1 || to < from || from > pages.Count)
            throw new ArgumentException("El rango de páginas no es válido.");
        if (pageSize.Width <= 0 || pageSize.Height <= 0 || printableArea.Width <= 0 || printableArea.Height <= 0)
            throw new ArgumentException("La impresora no proporcionó un tamaño de página válido.");
        _pages = pages;
        _printableArea = printableArea;
        _firstPage = from - 1;
        PageCount = Math.Min(to, pages.Count) - from + 1;
        PageSize = pageSize;
    }

    public override bool IsPageCountValid => true;
    public override int PageCount { get; }
    public override Size PageSize { get; set; }
    public override IDocumentPaginatorSource? Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= PageCount) return DocumentPage.Missing;
        using var stream = new MemoryStream(_pages[_firstPage + pageNumber], writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        var scale = Math.Min(_printableArea.Width / image.PixelWidth, _printableArea.Height / image.PixelHeight);
        var width = image.PixelWidth * scale;
        var height = image.PixelHeight * scale;
        var target = new Rect(_printableArea.X + (_printableArea.Width - width) / 2,
            _printableArea.Y + (_printableArea.Height - height) / 2, width, height);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(PageSize));
            drawing.DrawImage(image, target);
        }
        return new DocumentPage(visual, PageSize, new Rect(PageSize), target);
    }
}
