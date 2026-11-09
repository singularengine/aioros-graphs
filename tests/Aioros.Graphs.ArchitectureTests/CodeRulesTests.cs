using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aioros.Graphs.ArchitectureTests;

public sealed class CodeRulesTests
{
    public static TheoryData<string> Folders { get; } = ["src", "tests", "bench"];

    [Theory]
    [MemberData(nameof(Folders))]
    public async Task EveryClassIsSealedOrStaticAsync(string folder)
    {
        var files = await Repository.ParseAsync(folder);

        var open = from file in files
                   from type in file.Root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                   where type is ClassDeclarationSyntax || (type is RecordDeclarationSyntax record && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
                   where !type.Modifiers.Any(SyntaxKind.SealedKeyword) && !type.Modifiers.Any(SyntaxKind.StaticKeyword) && !type.Modifiers.Any(SyntaxKind.AbstractKeyword)
                   where !IsBenchmark(type)
                   select $"{file.Path}: {type.Identifier.Text}";

        Assert.Empty(open);
    }

    private static bool IsBenchmark(TypeDeclarationSyntax type) =>
        type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString() == "Benchmark"));

    [Fact]
    public async Task TheSourcesHaveNoLocksNoConcurrentCollectionsAndNoReflectionAsync()
    {
        var files = await Repository.ParseAsync("src");

        var found = from file in files
                    from node in file.Root.DescendantNodes()
                    where node is LockStatementSyntax
                        || (node is UsingDirectiveSyntax directive && directive.Name?.ToString() is "System.Reflection" or "System.Collections.Concurrent")
                        || (node is QualifiedNameSyntax name && name.ToString().StartsWith("System.Reflection", StringComparison.Ordinal))
                    select $"{file.Path}: {node}";

        Assert.Empty(found);
    }

    [Fact]
    public async Task TheSourcesHaveNoCommentsAsync()
    {
        var files = await Repository.ParseAsync("src");

        var comments = from file in files
                       from trivia in file.Root.DescendantTrivia()
                       where trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                       select $"{file.Path}: {trivia}";

        Assert.Empty(comments);
    }

    [Theory]
    [MemberData(nameof(Folders))]
    public async Task NoTypeIsLongerThan600LinesAsync(string folder)
    {
        var files = await Repository.ParseAsync(folder);

        var lines = from file in files
                    from type in file.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                    let span = type.GetLocation().GetLineSpan()
                    where span.EndLinePosition.Line - span.StartLinePosition.Line + 1 > 600
                    select $"{file.Path}: {type.Identifier.Text}";

        Assert.Empty(lines);
    }

    [Fact]
    public void TheCoreReferencesNoPackageAndNoProject()
    {
        var project = XDocument.Load(Path.Combine(Repository.Root, "src", "Aioros.Graphs", "Aioros.Graphs.csproj"));

        var references = project.Descendants().Select(e => e.Name.LocalName).ToList();

        Assert.DoesNotContain("PackageReference", references);
        Assert.DoesNotContain("ProjectReference", references);
        Assert.DoesNotContain("FrameworkReference", references);
    }

    [Fact]
    public void OnlyTheAvaloniaAdapterAndTheTestsReferenceAvalonia()
    {
        var allowed = new[] { "Aioros.Graphs.Avalonia.csproj", "Aioros.Graphs.Tests.csproj" };

        var referencing = Directory.EnumerateFiles(Repository.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => XDocument.Load(path).Descendants().Any(e => e.Name.LocalName == "PackageReference" && (e.Attribute("Include")?.Value ?? "").StartsWith("Avalonia", StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .ToList();

        Assert.All(referencing, name => Assert.Contains(name, allowed));
    }
}
