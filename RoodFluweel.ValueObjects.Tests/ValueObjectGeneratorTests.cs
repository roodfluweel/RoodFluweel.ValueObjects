using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RoodFluweel.ValueObjects.Generators;
using Shouldly;
using System.Text;

namespace RoodFluweel.ValueObjects.Tests;

public class ValueObjectGeneratorTests
{
    [Fact]
    public void GeneratorProduces_EqualsAndGetHashCode_ForFoo()
    {
        // 1) Bronsaak met [ValueObject]
        const string userSource = @"
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
}";
        // 2) Parse en compileer tot een Roslyn-compilatie
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(userSource, Encoding.UTF8));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectAttribute).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "DemoAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 3) Maak de driver en run de generator
        var generator = new ValueObjectGenerator();
        // gebruik GeneratorDriver als type, niet CSharpGeneratorDriver
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics
        );

        // 5) Inspecteer de gegenereerde bronnen
        var runResult = driver.GetRunResult();
        // Er is exact 1 generator en die levert 1 source file
        runResult.Results.ShouldHaveSingleItem()
            .GeneratedSources.ShouldHaveSingleItem();

        var generated = runResult.Results[0].GeneratedSources[0];
        var code = generated.SourceText.ToString();

        // 6) Check op de belangrijkste snippets
        code.ShouldContain("public bool Equals(Foo other)");
        code.ShouldContain("Equals(X, other.X) && Equals(Y, other.Y)");
        code.ShouldContain("public override int GetHashCode()");
        code.ShouldContain("HashCode.Combine(X, Y)");
    }

    [Fact]
    public void GenerateValueObject_ForClassWithProperties_GeneratesCorrectCode()
    {
        // 1) User‐broncode met een class met properties
        const string userSource = """
                                  using RoodFluweel.ValueObjects;
                                  namespace Demo
                                  {
                                      [ValueObject]
                                      public partial class Bar
                                      {
                                          public int A { get; }
                                          public string B { get; }
                                          public Bar(int a, string b) { A = a; B = b; }
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
        // 3) Haal het INamedTypeSymbol voor Demo.Bar op
        var model = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == "Bar");
        var symbol = model.GetDeclaredSymbol(classDecl);
        symbol.ShouldNotBeNull();
        // 4) Call your builder
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);
        // 5) Assert dat de belangrijkste stukken erin zitten
        generated.ShouldContain("public bool Equals(Bar other)");
        generated.ShouldContain("Equals(A, other.A) && Equals(B, other.B)");
        generated.ShouldContain("public override int GetHashCode()");
        generated.ShouldContain("HashCode.Combine(A, B)");
    }

    [Fact]
    public void GenerateValueObject_ForClassWithNoProperties_ReturnsEmptyObject()
    {
        // User‐Source
        const string userSource = """
                                  using RoodFluweel.ValueObjects;
                                  namespace Demo
                                  {
                                      [ValueObject]
                                      public partial class Empty
                                      {
                                          // Geen properties
                                      }
                                  }
                                  """;
        // Arrange
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

        // Act        
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);

        // Assert
        generated.ShouldContain("public bool Equals(Empty other)");

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
                                          public static string SomeStaticProperty { get; } = "Static";
                                          public SomeMethod(int a) { A = a; }
                                      }
                                  }
                                  """;
        // Arrange
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var voAsm = MetadataReference.CreateFromFile(typeof(ValueObjectCodeBuilder).Assembly.Location);
        var compilation = CSharpCompilation.Create(
            "DemoTests",
            new[] { syntaxTree },
            new[] { mscorlib, voAsm },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // Haal het INamedTypeSymbol voor Demo.Baz op
        var model = compilation.GetSemanticModel(syntaxTree);
        var classDecl = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == "Baz");
        var symbol = model.GetDeclaredSymbol(classDecl);
        symbol.ShouldNotBeNull();

        // Act
        var generated = ValueObjectCodeBuilder.GenerateValueObject(symbol!);

        // Arrange
        generated.ShouldNotContain("SomeStaticProperty");
        generated.ShouldNotContain("SomeMethod");
    }
}