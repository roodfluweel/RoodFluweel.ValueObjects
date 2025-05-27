using Microsoft.CodeAnalysis;
using System.Linq;

namespace RoodFluweel.ValueObjects;

internal static class ValueObjectCodeBuilder
{
    public static string GenerateValueObject(INamedTypeSymbol typeSymbol)
    {
        var ns = typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;

        IPropertySymbol[] members = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic)
            .ToArray();

        var comparisons = string.Join(" && ", members.Select(m => $"Equals({m.Name}, other.{m.Name})"));
        var hashValues = string.Join(", ", members.Select(m => m.Name));

        return $$"""

                 using System;
                 namespace {{ns}}
                 {
                     partial class {{typeName}} : IEquatable<{{typeName}}>
                     {
                         public bool Equals({{typeName}} other)
                         {
                             if (ReferenceEquals(null, other)) return false;
                             if (ReferenceEquals(this, other)) return true;
                             return {{comparisons}};
                         }

                         public override bool Equals(object obj)
                         {
                             return Equals(obj as {{typeName}});
                         }

                         public override int GetHashCode()
                         {
                             return HashCode.Combine({{hashValues}});
                         }

                         public static bool operator ==({{typeName}} left, {{typeName}} right)
                         {
                             return Equals(left, right);
                         }

                         public static bool operator !=({{typeName}} left, {{typeName}} right)
                         {
                             return !Equals(left, right);
                         }
                     }
                 }
                 """;
    }
}