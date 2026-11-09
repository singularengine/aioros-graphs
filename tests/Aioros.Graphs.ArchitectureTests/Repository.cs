using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aioros.Graphs.ArchitectureTests;

internal static partial class Repository
{
    public static string Root { get; } = FindRoot();

    public static IEnumerable<string> Sources(string folder) =>
        Directory.EnumerateFiles(Path.Combine(Root, folder), "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal);

    public static async Task<IReadOnlyList<(string Path, SyntaxNode Root)>> ParseAsync(string folder)
    {
        var trees = new List<(string, SyntaxNode)>();
        foreach (var file in Sources(folder))
        {
            var tree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(file), path: file);
            trees.Add((Path.GetRelativePath(Root, file), await tree.GetRootAsync()));
        }

        return trees;
    }

    public static async Task<IReadOnlyList<string>> CriteriaAsync()
    {
        var criteria = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, "docs", "acceptance"), "*.md").Order(StringComparer.Ordinal))
        {
            foreach (var line in await File.ReadAllLinesAsync(file))
            {
                var match = CriterionRow().Match(line);
                if (match.Success)
                {
                    criteria.Add(match.Groups[1].Value);
                }
            }
        }

        return criteria;
    }

    public static async Task<IReadOnlyList<(string Test, IReadOnlyList<string> Criteria)>> TestsAsync()
    {
        var tests = new List<(string, IReadOnlyList<string>)>();
        foreach (var (_, root) in (await ParseAsync("tests")).Where(file => !file.Path.Contains("ArchitectureTests", StringComparison.Ordinal)))
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsTest))
            {
                tests.Add((method.Identifier.Text, [.. Attributes(method).Where(IsCriterionTrait).Select(a => Text(a.ArgumentList!.Arguments[1]))]));
            }
        }

        return tests;
    }

    private static bool IsTest(MethodDeclarationSyntax method) =>
        Attributes(method).Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static bool IsCriterionTrait(AttributeSyntax attribute) =>
        attribute.Name.ToString() == "Trait"
        && attribute.ArgumentList is { Arguments.Count: 2 } arguments
        && Text(arguments.Arguments[0]) == "AC";

    private static IEnumerable<AttributeSyntax> Attributes(MethodDeclarationSyntax method) =>
        method.AttributeLists.SelectMany(list => list.Attributes);

    private static string Text(AttributeArgumentSyntax argument) =>
        argument.Expression is LiteralExpressionSyntax literal ? literal.Token.ValueText : argument.Expression.ToString();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aioros.Graphs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    [GeneratedRegex(@"^\|\s*([A-Z]+-\d{2,})\s*\|")]
    private static partial Regex CriterionRow();
}
