using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MarkdownConverter.Models;
using UglyToad.PdfPig;

namespace MarkdownConverter.Services;

public sealed class DocumentMarkdownConverter
{
    private static readonly XNamespace Wordprocessing = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public async Task<MarkdownConversionResult> ConvertAsync(
        IFormFile file,
        MarkdownConversionOptions options,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("The uploaded file is empty.");
        }

        await using var input = file.OpenReadStream();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var markdown = extension switch
        {
            ".pdf" => ConvertPdf(input),
            ".docx" => ConvertDocx(input),
            ".txt" => await ReadTextAsync(input, cancellationToken),
            ".md" or ".markdown" => await ReadTextAsync(input, cancellationToken),
            ".rtf" => ConvertRtf(await ReadTextAsync(input, cancellationToken)),
            ".html" or ".htm" => ConvertHtml(await ReadTextAsync(input, cancellationToken)),
            _ => throw new NotSupportedException(
                $"Unsupported file type '{extension}'. Supported types: .pdf, .docx, .txt, .md, .rtf, .html.")
        };

        markdown = MarkdownCleaner.Clean(markdown, options.Compact);

        return new MarkdownConversionResult(
            file.FileName,
            "text/markdown; charset=utf-8",
            markdown,
            markdown.Length);
    }

    private static string ConvertPdf(Stream stream)
    {
        var builder = new StringBuilder();

        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"## Page {page.Number}");
            builder.AppendLine();
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ConvertDocx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidOperationException("The DOCX file does not contain word/document.xml.");

        using var documentStream = documentEntry.Open();
        var document = XDocument.Load(documentStream);
        var builder = new StringBuilder();

        foreach (var block in document.Descendants(Wordprocessing + "body").Elements())
        {
            if (block.Name == Wordprocessing + "p")
            {
                AppendParagraph(builder, block);
            }
            else if (block.Name == Wordprocessing + "tbl")
            {
                AppendTable(builder, block);
            }
        }

        return builder.ToString();
    }

    private static void AppendParagraph(StringBuilder builder, XElement paragraph)
    {
        var text = ExtractParagraphText(paragraph).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var style = paragraph
            .Element(Wordprocessing + "pPr")
            ?.Element(Wordprocessing + "pStyle")
            ?.Attribute(Wordprocessing + "val")
            ?.Value;

        if (style is not null && style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            var headingLevel = ExtractHeadingLevel(style);
            builder.AppendLine($"{new string('#', headingLevel)} {text}");
        }
        else if (IsListParagraph(paragraph))
        {
            builder.AppendLine($"- {text}");
        }
        else
        {
            builder.AppendLine(text);
        }

        builder.AppendLine();
    }

    private static void AppendTable(StringBuilder builder, XElement table)
    {
        var rows = table.Elements(Wordprocessing + "tr")
            .Select(row => row.Elements(Wordprocessing + "tc")
                .Select(cell => MarkdownCleaner.EscapeTableCell(ExtractParagraphText(cell).Trim()))
                .ToArray())
            .Where(cells => cells.Length > 0)
            .ToList();

        if (rows.Count == 0)
        {
            return;
        }

        builder.AppendLine("| " + string.Join(" | ", rows[0]) + " |");
        builder.AppendLine("| " + string.Join(" | ", rows[0].Select(_ => "---")) + " |");

        foreach (var row in rows.Skip(1))
        {
            builder.AppendLine("| " + string.Join(" | ", row) + " |");
        }

        builder.AppendLine();
    }

    private static string ExtractParagraphText(XElement element)
    {
        var builder = new StringBuilder();

        foreach (var node in element.Descendants())
        {
            if (node.Name == Wordprocessing + "t")
            {
                builder.Append(node.Value);
            }
            else if (node.Name == Wordprocessing + "tab")
            {
                builder.Append(' ');
            }
            else if (node.Name == Wordprocessing + "br")
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static bool IsListParagraph(XElement paragraph)
    {
        return paragraph
            .Element(Wordprocessing + "pPr")
            ?.Element(Wordprocessing + "numPr") is not null;
    }

    private static int ExtractHeadingLevel(string style)
    {
        var match = Regex.Match(style, @"Heading(?<level>\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["level"].Value, out var level)
            ? Math.Clamp(level, 1, 6)
            : 2;
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ConvertRtf(string rtf)
    {
        var text = Regex.Replace(rtf, @"\\'[0-9a-fA-F]{2}", " ");
        text = Regex.Replace(text, @"\\[a-zA-Z]+\d* ?", " ");
        text = Regex.Replace(text, @"[{}]", " ");
        return WebUtility.HtmlDecode(text);
    }

    private static string ConvertHtml(string html)
    {
        var text = Regex.Replace(html, @"<(script|style)[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(h1|h2|h3|p|div|li|tr)>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li[^>]*>", "- ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(text);
    }
}
