using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DisposableGenerator
{
    /// <summary>
    /// Extensions to <see cref="ITypeSymbol"/> that allow us to succinctly perform
    /// queries against the semantic model that are specifically interesting to the
    /// implementation of this source generator.
    /// </summary>
    public static class TypeSymbolExtensions
    {
        public static bool InheritsFromSymbol(this ITypeSymbol self, INamedTypeSymbol candidate) =>
            self.AllInterfaces.Contains(candidate);

        public static bool ContainsPublicDisposeMember(
            this ITypeSymbol self,
            [NotNullWhen(true)]
            out IMethodSymbol? methodSymbol)
        {
            foreach (var member in self.GetMembers())
            {
                // We're looking for "void Dispose()"
                if (!(member is IMethodSymbol
                    {
                    ReturnsVoid: true,
                    Name: "Dispose",
                    Parameters: { Length: 0 }
                    } methodCandidate)) continue;

                methodSymbol = methodCandidate;
                return true;
            }

            methodSymbol = null;
            return false;
        }

        public static bool ContainsCustomDisposer(this ITypeSymbol self, string name)
        {
            foreach (var member in self.GetMembers())
            {
                if (!(member is IMethodSymbol
                    {
                    ReturnsVoid: true,
                    Parameters: { Length: 0 }
                    } methodCandidate)) continue;

                if (methodCandidate.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetMembersThatInheritFrom(
            this ITypeSymbol self,
            INamedTypeSymbol disposeInterfaceSymbol) =>
            self.GetMembers()
                .OfType<ITypeSymbol>()
                .Where(m => m.InheritsFromSymbol(disposeInterfaceSymbol))
                .Select(m => m.Name);
    }
}