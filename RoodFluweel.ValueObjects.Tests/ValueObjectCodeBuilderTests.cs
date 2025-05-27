using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace RoodFluweel.ValueObjects.Tests;

public class ValueObjectCodeBuilderTests
{
    [Fact]
    public void GenerateValueObject_ForSimpleClass_ContainsEqualsAndHashCode()
    {
        // 1) User‐broncode met enkele properties
        const string userSource = """

                                  using RoodFluweel.ValueObjects;

                                  namespace Demo
                                  {
                                      [ValueObject]
                                      public partial class Foo
                                      {
                                          public int X { get; }
                                          public string Y { get; }
                                          public Foo(int x, string y) { X = x; Y = y; }
                                      }
                                  }
                                  """;

        // 2) Parse en compileer
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var voAsm = MetadataReference.CreateFromFile(typeof(ValueObjectCodeBuilder).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            "DemoTests",
            new[] { syntaxTree },
            new[] { mscorlib, voAsm },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 3) Haal het INamedTypeSymbol voor Demo.Foo op
        var model = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == "Foo");
        var symbol = model.GetDeclaredSymbol(classDecl);
        symbol.ShouldNotBeNull();

        // 4) Call your builder
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);

        // 5) Assert dat de belangrijkste stukken erin zitten
        generated.ShouldContain("public bool Equals(Foo other)");
        generated.ShouldContain("Equals(X, other.X) && Equals(Y, other.Y)");
        generated.ShouldContain("public override int GetHashCode()");
        generated.ShouldContain("HashCode.Combine(X, Y)");
    }

    [Fact]
    public void GenerateValueObject_ForClassWithStaticProperty_ExcludesStaticProperty()
    {
        // 1) User‐broncode met een statische property
        const string userSource = """
                                  using RoodFluweel.ValueObjects;
                                  namespace Demo
                                  {
                                      [ValueObject]
                                      public partial class Baz
                                      {
                                          public int A { get; }
                                          public static string StaticProperty { get; } = "Static";
                                          public Baz(int a) { A = a; }
                                      }
                                  }
                                  """;
        // 2) Parse en compileer
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var voAsm = MetadataReference.CreateFromFile(typeof(ValueObjectCodeBuilder).Assembly.Location);
        var compilation = CSharpCompilation.Create(
            "DemoTests",
            new[] { syntaxTree },
            new[] { mscorlib, voAsm },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // 3) Haal het INamedTypeSymbol voor Demo.Baz op
        var model = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == "Baz");
        var symbol = model.GetDeclaredSymbol(classDecl);
        symbol.ShouldNotBeNull();
        // 4) Call your builder
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);
        // 5) Assert dat de statische property niet in de gegenereerde code zit
        generated.ShouldNotContain("StaticProperty");
    }

    [Fact]
    public void GenerateValueObject_ForClassWithNoProperties_ReturnsEmptyObject()
    {
        // 1) User‐broncode zonder properties
        const string userSource = """
                                  using RoodFluweel.ValueObjects;
                                  namespace Demo
                                  {
                                      [ValueObject]
                                      public partial class Empty
                                      {
                                      }
                                  }
                                  """;
        // 2) Parse en compileer
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var voAsm = MetadataReference.CreateFromFile(typeof(ValueObjectCodeBuilder).Assembly.Location);
        var compilation = CSharpCompilation.Create(
            "DemoTests",
            new[] { syntaxTree },
            new[] { mscorlib, voAsm },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // 3) Haal het INamedTypeSymbol voor Demo.Empty op
        var model = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == "Empty");
        var symbol = model.GetDeclaredSymbol(classDecl);
        symbol.ShouldNotBeNull();
        // 4) Call your builder
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);
        // 5) Assert dat de gegenereerde code geen properties bevat
        generated.ShouldContain("public bool Equals(Empty other)");
        generated.ShouldContain("public override int GetHashCode()");
    }
}