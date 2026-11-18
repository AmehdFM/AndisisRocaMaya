using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public sealed record ReportCallout(string Title, string Message, string Accent = "#245F8C")
{
    public void Compose(IContainer container)
    {
        container.ShowEntire().Background("#F5F8FB").BorderLeft(3).BorderColor(Accent).Padding(9).Column(panel =>
        {
            panel.Item().Text(Title).FontSize(11).FontColor(Accent).SemiBold();
            panel.Item().PaddingTop(3).Text(Message).FontSize(12).FontColor(Accent);
        });
    }
}
