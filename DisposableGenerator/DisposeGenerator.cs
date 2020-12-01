using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposableGenerator
{
    // What to do when a type inherits from IDisposable
    // x Am I an interface? Do nothing.
    // x Do I inherit from IDisposable? Yes, continue.
    // - Is the class partial?
    // x Do I already have a non-abstract dispose method? Do nothing.
    // - Do I already have an abstract dispose on a base? Implement.
    // x Do I have a DisposeManaged? Call it.
    // x Do I have a DisposeUnmanaged? Call it.

    // Analyzer / Diagnostics:
    // - Are DisposeManaged/DisposeUnmanaged parameter-less and void?
    // - Are other Dispose objects disposed in DisposeManaged?

    [Generator]
    public partial class DisposeGenerator : ISourceGenerator
    {
        private struct DisposeWork
        {
            public ITypeSymbol Symbol;
            public IEnumerable<ITypeSymbol> DisposableMembers;
            public bool ImplementManaged;
            public bool ImplementUnmanaged;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is DisposeSyntaxReceiver receiver))
                return;

            var workToDo = DetermineWork(context, receiver);

            // Let's get generating!
            foreach (DisposeWork work in workToDo)
            {
                // TODO: Move this and context.AddSource inside of EmitSource?
                string hintName = $"{work.Symbol.ContainingNamespace}.{work.Symbol.Name}.Dispose.cs"; 
                string sourceText = EmitSource(context, work);

                context.AddSource(hintName, sourceText);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new DisposeSyntaxReceiver());
        }

        private static List<DisposeWork> DetermineWork(GeneratorExecutionContext context, DisposeSyntaxReceiver receiver)
        {
            var disposeInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IDisposable");
            if (disposeInterfaceSymbol is null)
                return new List<DisposeWork>();

            // Determine the real amount of work to do
            List<DisposeWork> workToDo = new List<DisposeWork>();
            foreach (ClassDeclarationSyntax candidate in receiver.CandidateClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
                var candidateType = model.GetDeclaredSymbol(candidate) as ITypeSymbol;

                if (candidateType is null) continue;

                // Do I inherit from IDisposable?
                if (!candidateType.AllInterfaces.Contains(disposeInterfaceSymbol)) continue;

                // Do I already have a public void Dispose()?
                if (ContainsPublicDispose(candidateType, out var disposeMethodSymbol))
                {
                    // TODO: Handle base classes eventually...
                    continue;
                }

                var work = new DisposeWork
                {
                    Symbol = candidateType,

                    DisposableMembers = GetDisposableMembers(context, candidateType),

                    // Do I have a DisposeManaged member method? Call it.
                    ImplementManaged = ContainsCustomDisposer(candidateType, "DisposeManaged"),

                    // Do I have a DisposeUnmanaged member method? Call it.
                    ImplementUnmanaged = ContainsCustomDisposer(candidateType, "DisposeUnmanaged"),
                };

                //if (work.ImplementUnmanaged == false && work.ImplementManaged == false)
                //{
                //    context.ReportDiagnostic(
                //        Diagnostic.Create(
                //            "DP0001",
                //            nameof(DisposeGenerator),
                //            $"Class '{candidateType.Name}' does not implement a DisposeManaged or DisposeUnmanaged method.",
                //            DiagnosticSeverity.Warning,
                //            DiagnosticSeverity.Warning,
                //            true,
                //            3,
                //            location: candidate.GetLocation()));
                //}
                workToDo.Add(work);
            }

            return workToDo;
        }

        private static bool ContainsPublicDispose(
            ITypeSymbol classSymbol,
            [NotNullWhen(true)]
            out IMethodSymbol? methodSymbol)
        {
            foreach (var member in classSymbol.GetMembers())
            {
                // We're looking for "void Dispose()"
                if (member is IMethodSymbol
                    {
                    ReturnsVoid: true,
                    Name: "Dispose",
                    Parameters: { Length: 0 }
                    } methodCandidate)
                {
                    methodSymbol = methodCandidate;
                    return true;
                }
            }

            methodSymbol = null;
            return false;
        }

        private static bool ContainsCustomDisposer(ITypeSymbol classSymbol, string name)
        {
            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IMethodSymbol
                    {
                    ReturnsVoid: true,
                    Parameters: { Length: 0 }
                    } methodCandidate)
                {
                    if (methodCandidate.Name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<ITypeSymbol> GetDisposableMembers(GeneratorExecutionContext context, ITypeSymbol symbol)
        {
            var disposeInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IDisposable");
            if (disposeInterfaceSymbol is null)
                return Enumerable.Empty<ITypeSymbol>();

            return symbol.GetMembers()
                .OfType<ITypeSymbol>()
                .Where(m => m.AllInterfaces.Contains(disposeInterfaceSymbol));
        }
    }
}
