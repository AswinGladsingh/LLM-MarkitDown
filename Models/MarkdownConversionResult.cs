namespace MarkdownConverter.Models;

public sealed record MarkdownConversionResult(
    string FileName,
    string ContentType,
    string Markdown,
    int CharacterCount);
