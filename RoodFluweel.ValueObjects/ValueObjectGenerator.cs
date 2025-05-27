using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoodFluweel.ValueObjects;

[Generator]
public class ValueObjectGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ValueObjectReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver == null)
        {
            return;
        }
        if (context.SyntaxReceiver is not ValueObjectReceiver receiver)
        {
            return;
        }

        foreach (TypeDeclarationSyntax candidate in receiver.Candidates)
        {
            SemanticModel model = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
            ISymbol symbol = model.GetDeclaredSymbol(candidate);

            if (symbol is not INamedTypeSymbol namedType)
            {
                continue;
            }

            if (!namedType.GetAttributes().Any(a => a.AttributeClass?.Name == "ValueObjectAttribute"))
            {
                continue;
            }

            var source = ValueObjectCodeBuilder.GenerateValueObject(namedType);
            context.AddSource($"{namedType.Name}_ValueObject.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    internal class ValueObjectReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> Candidates { get; } = [];

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax { AttributeLists.Count: > 0 } tds)
            {
                Candidates.Add(tds);
            }
        }
    }
}