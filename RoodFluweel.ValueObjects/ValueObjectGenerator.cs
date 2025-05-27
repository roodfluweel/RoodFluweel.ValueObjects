using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace RoodFluweel.ValueObjects.Generators
{
    [Generator]
    public class ValueObjectGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Identify types marked with [ValueObject]
            var candidateTypes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0,
                    transform: static (ctx, _) =>
                    {
                        var typeDecl = (TypeDeclarationSyntax)ctx.Node;
                        var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                        if (symbol == null) return null;

                        // Check for [ValueObject] attribute
                        foreach (var attr in symbol.GetAttributes())
                        {
                            var name = attr.AttributeClass?.ToDisplayString();
                            if (name == "RoodFluweel.ValueObjects.ValueObjectAttribute"
                                || name == "ValueObjectAttribute")
                            {
                                return symbol;
                            }
                        }
                        return null;
                    })
                .Where(static t => t is not null)!;

            // 2. Collect all matching symbols
            var allSymbols = candidateTypes.Collect();

            // 3. Combine with the compilation
            var compilationAndSymbols = context.CompilationProvider.Combine(allSymbols);

            // 4. Register output: generate code for each symbol
            context.RegisterSourceOutput(compilationAndSymbols, static (spc, source) =>
            {
                var (compilation, symbols) = source;
                foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default))
                {
                    var typeSymbol = (INamedTypeSymbol)symbol;

                    // Generate the code using your existing builder
                    var code = ValueObjectCodeBuilder.GenerateValueObject(typeSymbol!);
                    spc.AddSource($"{typeSymbol!.Name}_ValueObject.g.cs", SourceText.From(code, Encoding.UTF8));
                }
            });
        }
    }
}
