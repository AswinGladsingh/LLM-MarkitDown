using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MarkdownConverter.Services.Parsers;

public static class CSharpParser
{
    public static string Convert(string fileName, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var md = new StringBuilder();

        md.AppendLine($"# {Path.GetFileName(fileName)}");
        md.AppendLine();

        // Namespace
        foreach (var ns in root.DescendantNodes()
                     .OfType<NamespaceDeclarationSyntax>())
        {
            md.AppendLine($"## Namespace: {ns.Name}");
            md.AppendLine();
        }

        // Interfaces
        foreach (var iface in root.DescendantNodes()
                     .OfType<InterfaceDeclarationSyntax>())
        {
            md.AppendLine($"## Interface: {iface.Identifier}");
            md.AppendLine();

            md.AppendLine("### Methods");

            foreach (var method in iface.Members
                         .OfType<MethodDeclarationSyntax>())
            {
                var parameters = string.Join(
                    ", ",
                    method.ParameterList.Parameters
                        .Select(p => $"{p.Type} {p.Identifier}"));

                md.AppendLine(
                    $"- {method.Identifier}({parameters})");
            }

            md.AppendLine();
        }

        // Enums
        foreach (var enumDecl in root.DescendantNodes()
                     .OfType<EnumDeclarationSyntax>())
        {
            md.AppendLine($"## Enum: {enumDecl.Identifier}");
            md.AppendLine();

            foreach (var member in enumDecl.Members)
            {
                md.AppendLine($"- {member.Identifier}");
            }

            md.AppendLine();
        }

        // Classes
        foreach (var cls in root.DescendantNodes()
                     .OfType<ClassDeclarationSyntax>())
        {
            md.AppendLine($"## Class: {cls.Identifier}");
            md.AppendLine();

            // Interfaces / Base classes
            if (cls.BaseList != null)
            {
                md.AppendLine("### Implements");

                foreach (var type in cls.BaseList.Types)
                {
                    md.AppendLine($"- {type}");
                }

                md.AppendLine();
            }

            // Constructor Dependencies
            var constructors = cls.Members
                .OfType<ConstructorDeclarationSyntax>();

            if (constructors.Any())
            {
                md.AppendLine("### Dependencies");

                foreach (var ctor in constructors)
                {
                    foreach (var parameter in ctor.ParameterList.Parameters)
                    {
                        md.AppendLine($"- {parameter.Type}");
                    }
                }

                md.AppendLine();
            }

            // Properties
            var properties = cls.Members
                .OfType<PropertyDeclarationSyntax>();

            if (properties.Any())
            {
                md.AppendLine("### Properties");

                foreach (var property in properties)
                {
                    md.AppendLine(
                        $"- {property.Type} {property.Identifier}");
                }

                md.AppendLine();
            }

            // Public Methods Only
            var methods = cls.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(x => x.Text == "public"));

            if (methods.Any())
            {
                md.AppendLine("### Public Methods");

                foreach (var method in methods)
                {
                    var parameters = string.Join(
                        ", ",
                        method.ParameterList.Parameters
                            .Select(p => $"{p.Type} {p.Identifier}"));

                    md.AppendLine(
                        $"- {method.Identifier}({parameters})");
                }

                md.AppendLine();
            }
        }

        return md.ToString();
    }
}