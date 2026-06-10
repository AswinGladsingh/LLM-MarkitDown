using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownConverter.Services;

public static class MarkdownCleaner
{
    public static string Clean(string markdown, bool compact, bool preserveWhitespace = false)
    {
        markdown = markdown.Replace("\r\n", "\n").Replace('\r', '\n');

        if (!preserveWhitespace)
        {
            markdown = Regex.Replace(markdown, @"[ \t]+", " ");
            markdown = Regex.Replace(markdown, @" *\n *", "\n");
        }

        if (compact)
        {
            markdown = RemoveRepeatedBoilerplate(markdown);

            if (!preserveWhitespace)
            {
                markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
            }
        }

        return markdown.Trim() + "\n";
    }

    public static string EscapeTableCell(string value)
    {
        return value.Replace("|", "\\|").Replace("\n", "<br>");
    }

    private static string RemoveRepeatedBoilerplate(string markdown)
    {
        var lines = markdown.Split('\n');
        var repeated = lines
            .Select(line => line.Trim())
            .Where(line => line.Length is > 4 and < 120)
            .GroupBy(line => line, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 3)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (repeated.Count == 0)
        {
            return markdown;
        }

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            if (!repeated.Contains(line.Trim()))
            {
                builder.AppendLine(line);
            }
        }

        return builder.ToString();
    }
}
