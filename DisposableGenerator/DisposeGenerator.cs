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

    public class DisposeWork
    {
        public string NamespaceName;
        public string ClassName;
        public string DeclaredAccessibility;

        public IEnumerable<string> DisposableMemberNames;

        public bool ImplementManaged;
        public bool ImplementUnmanaged;

        public bool HasWork => ImplementUnmanaged || ImplementManaged || DisposableMemberNames.Any();

        public DisposeWork()
        {
            NamespaceName = string.Empty;
            ClassName = string.Empty;
            DeclaredAccessibility = string.Empty;
            DisposableMemberNames = Enumerable.Empty<string>();
        }
    }

    [Generator]
    public class DisposeGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is DisposeSyntaxReceiver receiver))
                return;

            var workToDo = DetermineWork(context, receiver);

            // Let's get generating!
            foreach (DisposeWork work in workToDo)
            {
                var disposeWriter = new DisposeWriter(work);

                // TODO: Move this and context.AddSource inside of EmitSource?
                string hintName = $"{work.NamespaceName}.{work.ClassName}.Dispose.cs"; 
                string sourceText = disposeWriter.Emit();

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
                    NamespaceName = candidateType.ContainingNamespace.ToString(),
                    ClassName = candidateType.Name,
                    DeclaredAccessibility = candidateType.DeclaredAccessibility.ToString(),

                    DisposableMemberNames = GetDisposableMembers(context, candidateType),

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

        private static IEnumerable<string> GetDisposableMembers(GeneratorExecutionContext context, ITypeSymbol symbol)
        {
            var disposeInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IDisposable");
            if (disposeInterfaceSymbol is null)
                return Enumerable.Empty<string>();

            return symbol.GetMembers()
                .OfType<ITypeSymbol>()
                .Where(m => m.AllInterfaces.Contains(disposeInterfaceSymbol))
                .Select(m => m.Name);
        }
    }
}
