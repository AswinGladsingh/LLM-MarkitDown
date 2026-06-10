using System.Text;
using MarkdownConverter.Models;
using MarkdownConverter.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DocumentMarkdownConverter>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
});

var app = builder.Build();

app.MapGet("/", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Markdown Converter</title>
  <style>
    body { font-family: system-ui, sans-serif; margin: 3rem auto; max-width: 720px; line-height: 1.5; }
    form { display: grid; gap: 1rem; padding: 1.25rem; border: 1px solid #ddd; border-radius: 8px; }
    button { width: fit-content; padding: .65rem 1rem; }
    code { background: #f4f4f4; padding: .15rem .3rem; border-radius: 4px; }
  </style>
</head>
<body>
  <h1>Document to Markdown</h1>
  <p>Upload documents or source files like <code>.pdf</code>, <code>.docx</code>, <code>.html</code>, <code>.js</code>, <code>.ts</code>, or <code>.cs</code> and download a compact Markdown file.</p>
  <form action="/convert" method="post" enctype="multipart/form-data">
    <input name="file" type="file" required>
    <label><input name="compact" type="checkbox" value="true" checked> Compact repeated boilerplate and blank lines</label>
    <button type="submit">Convert to .md</button>
  </form>
</body>
</html>
""", "text/html; charset=utf-8"));

app.MapPost("/convert", async (
    IFormFile file,
    bool? compact,
    DocumentMarkdownConverter converter,
    CancellationToken cancellationToken) =>
{
    var result = await converter.ConvertAsync(
        file,
        new MarkdownConversionOptions(compact ?? true),
        cancellationToken);

    var outputName = $"{Path.GetFileNameWithoutExtension(result.FileName)}.md";
    return Results.File(
        Encoding.UTF8.GetBytes(result.Markdown),
        result.ContentType,
        outputName);
})
.DisableAntiforgery();

app.MapPost("/convert/json", async (
    IFormFile file,
    bool? compact,
    DocumentMarkdownConverter converter,
    CancellationToken cancellationToken) =>
{
    var result = await converter.ConvertAsync(
        file,
        new MarkdownConversionOptions(compact ?? true),
        cancellationToken);

    return Results.Ok(result);
})
.DisableAntiforgery();

app.Run();
