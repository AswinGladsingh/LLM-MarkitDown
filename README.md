# MarkdownConverter

A small ASP.NET Core API that converts uploaded documents into compact Markdown so the text can be fed to LLMs with lower token usage.

## Supported Inputs

- PDF (`.pdf`)
- Word Open XML (`.docx`)
- Markdown (`.md`, `.markdown`)
- Plain text (`.txt`)
- RTF (`.rtf`)
- HTML (`.html`, `.htm`)

Old binary Word files (`.doc`) are not supported directly. Save them as `.docx` first.

## Run

```powershell
dotnet restore
dotnet run --project .\MarkdownConverter.csproj
```

Open the URL printed by `dotnet run`, then upload a file in the browser.

## API

Download a Markdown file:

```powershell
curl.exe -F "file=@C:\path\to\document.pdf" -F "compact=true" http://localhost:5000/convert -o document.md
```

Return JSON:

```powershell
curl.exe -F "file=@C:\path\to\document.docx" http://localhost:5000/convert/json
```

`compact=true` removes repeated short lines and collapses extra blank space. This is useful for PDFs with repeated headers, footers, or page labels.
