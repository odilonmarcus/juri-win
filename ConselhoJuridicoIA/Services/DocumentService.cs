using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ConselhoJuridicoIA.Models;
using UglyToad.PdfPig;

namespace ConselhoJuridicoIA.Services;

public sealed class DocumentService
{
    private const int MaxCharactersPerFile = 220_000;

    static DocumentService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<DocumentAttachment> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        var ext = info.Extension.ToLowerInvariant();

        string content = ext switch
        {
            ".pdf" => await Task.Run(() => ExtractPdf(path), cancellationToken),
            ".txt" => await ReadTextAsync(path, cancellationToken),
            ".rtf" => ExtractRtf(await ReadTextAsync(path, cancellationToken)),
            _ => throw new NotSupportedException($"Formato não suportado: {ext}")
        };

        content = Normalize(content);
        if (ext == ".pdf" && string.IsNullOrWhiteSpace(content))
        {
            content = "[AVISO DO APLICATIVO: não foi possível extrair texto deste PDF. Ele pode ser digitalizado/imagem, protegido ou não conter camada de texto. Converta-o para PDF pesquisável/OCR antes de confiar na análise deste documento.]";
        }

        var truncated = content.Length > MaxCharactersPerFile;
        if (truncated)
            content = content[..MaxCharactersPerFile] + "\n\n[CONTEÚDO TRUNCADO PELO APLICATIVO PARA LIMITAR O TAMANHO DA ANÁLISE]";

        return new DocumentAttachment
        {
            FileName = info.Name,
            FilePath = info.FullName,
            FileSizeBytes = info.Length,
            Content = content,
            IsTruncated = truncated
        };
    }

    private static async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ExtractPdf(string path)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(path);
        var pageNumber = 0;
        foreach (var page in pdf.GetPages())
        {
            pageNumber++;
            sb.AppendLine($"\n--- Página {pageNumber} ---");
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    // Extrator deliberadamente simples: preserva texto, parágrafos, caracteres Unicode e escapes hexadecimais.
    // O objetivo é transformar RTF em contexto textual para os modelos, não reproduzir formatação visual.
    private static string ExtractRtf(string rtf)
    {
        if (!rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.OrdinalIgnoreCase))
            return rtf;

        var sb = new StringBuilder(rtf.Length);
        var skipGroup = new Stack<bool>();
        var skip = false;

        for (var i = 0; i < rtf.Length; i++)
        {
            var c = rtf[i];
            if (c == '{')
            {
                skipGroup.Push(skip);
                continue;
            }
            if (c == '}')
            {
                if (skipGroup.Count > 0)
                    skip = skipGroup.Pop();
                continue;
            }
            if (c != '\\')
            {
                if (!skip && c != '\r' && c != '\n')
                    sb.Append(c);
                continue;
            }

            if (i + 1 >= rtf.Length)
                break;

            var next = rtf[i + 1];
            if (next is '\\' or '{' or '}')
            {
                if (!skip)
                    sb.Append(next);
                i++;
                continue;
            }

            if (next == '\'')
            {
                if (i + 3 < rtf.Length && int.TryParse(rtf.Substring(i + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                {
                    if (!skip)
                        sb.Append(Encoding.GetEncoding(1252).GetString(new[] { (byte)hex }));
                    i += 3;
                }
                continue;
            }

            var start = i + 1;
            var j = start;
            while (j < rtf.Length && char.IsLetter(rtf[j])) j++;
            var word = rtf[start..j];

            var sign = 1;
            if (j < rtf.Length && rtf[j] == '-')
            {
                sign = -1;
                j++;
            }

            var numberStart = j;
            while (j < rtf.Length && char.IsDigit(rtf[j])) j++;
            int? parameter = null;
            if (j > numberStart && int.TryParse(rtf[numberStart..j], out var parsed))
                parameter = sign * parsed;

            if (j < rtf.Length && rtf[j] == ' ') j++;
            i = j - 1;

            switch (word)
            {
                case "par":
                case "line":
                    if (!skip) sb.AppendLine();
                    break;
                case "tab":
                    if (!skip) sb.Append('\t');
                    break;
                case "u":
                    if (!skip && parameter.HasValue)
                    {
                        var value = parameter.Value < 0 ? parameter.Value + 65536 : parameter.Value;
                        sb.Append((char)value);
                    }
                    break;
                case "fonttbl":
                case "colortbl":
                case "stylesheet":
                case "info":
                case "pict":
                case "object":
                case "header":
                case "footer":
                    skip = true;
                    break;
            }
        }

        return sb.ToString();
    }

    private static string Normalize(string text)
    {
        text = text.Replace("\0", string.Empty);
        text = Regex.Replace(text, "[ \\t]+", " ");
        text = Regex.Replace(text, "\\r?\\n[ \\t]+", Environment.NewLine);
        text = Regex.Replace(text, "(\\r?\\n){4,}", Environment.NewLine + Environment.NewLine + Environment.NewLine);
        return text.Trim();
    }
}
