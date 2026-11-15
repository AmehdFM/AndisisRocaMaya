using System.IO;
using Microsoft.Win32;
using System.Windows;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaRocaMaya.Services.Reports;

public static class PdfExportService
{
    public static void Export(Func<ReportDocument> createDocument, string suggestedName, Action<string> setStatus)
    {
        try
        {
            var path = Save(createDocument(), suggestedName);
            if (path is not null) setStatus($"PDF guardado en {path}");
        }
        catch (Exception exception)
        {
            var message = $"No se pudo exportar el PDF. {exception.Message}";
            setStatus(message);
            MessageBox.Show(message, "Exportar PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static string? Save(ReportDocument document, string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar reporte PDF",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = suggestedName
        };
        if (dialog.ShowDialog() != true) return null;

        var data = Generate(document);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(dialog.FileName)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporaryPath, data);
            File.Move(temporaryPath, dialog.FileName, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        return dialog.FileName;
    }

    public static byte[] Generate(ReportDocument document)
    {
        // The project is currently an academic prototype using the Community license.
        QuestPDF.Settings.License = LicenseType.Community;
        return document.GeneratePdf();
    }
}
