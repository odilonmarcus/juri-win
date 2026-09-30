using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ConselhoJuridicoIA.Services;

public sealed class PdfExportService
{
    public void Export(string path, string question, string opinion)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Parecer — Conselho Jurídico IA";
        document.Info.Author = "Conselho Jurídico IA";

        var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
        var headingFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
        var bodyFont = new XFont("Segoe UI", 10, XFontStyleEx.Regular);
        var metaFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);

        PdfPage? page = null;
        XGraphics? gfx = null;
        var margin = 46d;
        var y = 0d;
        var lineHeight = 14.5d;
        var usableWidth = 0d;
        var bottom = 0d;

        void NewPage()
        {
            gfx?.Dispose();
            page = document.AddPage();
            page.Size = PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            usableWidth = page.Width.Point - margin * 2;
            bottom = page.Height.Point - margin;
            y = margin;
        }

        void Ensure(double needed)
        {
            if (page is null || y + needed > bottom)
                NewPage();
        }

        void DrawLine(string text, XFont font, double height, XBrush brush)
        {
            Ensure(height + 2);
            gfx!.DrawString(text, font, brush, new XRect(margin, y, usableWidth, height), XStringFormats.TopLeft);
            y += height;
        }

        void DrawParagraph(string text, XFont font, XBrush brush)
        {
            foreach (var rawLine in NormalizeLines(text))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    y += lineHeight * 0.7;
                    continue;
                }

                foreach (var line in Wrap(rawLine, gfx!, font, usableWidth))
                    DrawLine(line, font, lineHeight, brush);
            }
        }

        NewPage();
        DrawLine("Conselho Jurídico IA", titleFont, 27, XBrushes.Black);
        DrawLine("Parecer consolidado", headingFont, 18, XBrushes.DimGray);
        DrawLine($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", metaFont, 15, XBrushes.Gray);
        y += 10;

        DrawLine("Questão analisada", headingFont, 18, XBrushes.Black);
        DrawParagraph(question, bodyFont, XBrushes.Black);
        y += 10;

        DrawLine("Parecer", headingFont, 18, XBrushes.Black);
        DrawParagraph(opinion, bodyFont, XBrushes.Black);

        gfx?.Dispose();
        document.Save(path);
    }

    private static IEnumerable<string> NormalizeLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private static IEnumerable<string> Wrap(string text, XGraphics gfx, XFont font, double maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            yield return string.Empty;
            yield break;
        }

        var line = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            var candidate = line + " " + words[i];
            if (gfx.MeasureString(candidate, font).Width <= maxWidth)
            {
                line = candidate;
                continue;
            }

            yield return line;
            line = words[i];
        }

        yield return line;
    }
}
