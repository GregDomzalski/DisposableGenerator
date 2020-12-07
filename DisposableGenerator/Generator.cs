using System.Collections.Generic;
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
    public class Generator : ISourceGenerator
    {
        /// <summary>
        /// Runs the IDisposable Generator against the current context.
        /// Note that the generator is only called once per compilation,
        /// so we will likely be processing many classes.
        /// </summary>
        /// <param name="context">
        /// The <see cref="GeneratorExecutionContext"/> which contains,
        /// among other things, the <see cref="SyntaxReceiver"/> that
        /// should contain all of the candidate symbols that the generator
        /// shall run against.
        /// </param>
        public void Execute(GeneratorExecutionContext context)
        {
            // Sanity check: We should only ever be working with one type
            // of syntax receiver.
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // Do a pass over the current execution context given the
            // candidate symbols found by the syntax receiver.
            var workToDo = DetermineWork(context, receiver);

            // For each piece of work that has been found to do, emit the
            // generated source code and add it to the compilation.
            foreach (DisposeWork work in workToDo)
            {
                var disposeWriter = new Writer(work);

                string hintName = disposeWriter.SuggestFileName();
                string sourceText = disposeWriter.Emit();

                context.AddSource(hintName, sourceText);
            }
        }

        /// <summary>
        /// Initializes the generator into a given context. This is mainly
        /// used to register a syntax receiver that can be used to quickly
        /// determine whether this generator should participate in the
        /// current compilation or not.
        /// </summary>
        /// <param name="context">
        /// The <see cref="GeneratorInitializationContext"/> given to us by
        /// the Roslyn Compiler. This context allows us to register our
        /// syntax receiver with the compiler.
        /// </param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private static List<DisposeWork> DetermineWork(GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            var disposeInterfaceSymbol = context.Compilation.GetTypeByMetadataName("System.IDisposable");
            if (disposeInterfaceSymbol is null)
                return new List<DisposeWork>();

            // Determine the real amount of work to do
            var workToDo = new List<DisposeWork>();
            foreach (ClassDeclarationSyntax candidate in receiver.CandidateClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
                var candidateType = model.GetDeclaredSymbol(candidate) as ITypeSymbol;

                if (candidateType is null) continue;

                if (!candidateType.InheritsFromSymbol(disposeInterfaceSymbol)) continue;

                // Do I already have a public void Dispose()?
                if (candidateType.ContainsPublicDisposeMember(out var disposeMethodSymbol))
                {
                    // TODO: Handle base classes eventually...
                    continue;
                }

                var disposableMembers = candidateType.GetMembersThatInheritFrom(disposeInterfaceSymbol);

                AddWorkToQueue(candidateType, disposableMembers, workToDo);
                //if (work.ImplementUnmanaged == false && work.ImplementManaged == false)
                //{
                //    context.ReportDiagnostic(
                //        Diagnostic.Create(
                //            "DP0001",
                //            nameof(Generator),
                //            $"Class '{candidateType.Name}' does not implement a DisposeManaged or DisposeUnmanaged method.",
                //            DiagnosticSeverity.Warning,
                //            DiagnosticSeverity.Warning,
                //            true,
                //            3,
                //            location: candidate.GetLocation()));
                //}
            }

            return workToDo;
        }

        private static void AddWorkToQueue(
            ITypeSymbol candidateType,
            IEnumerable<string> disposableMembers,
            ICollection<DisposeWork> workToDo)
        {
            var work = new DisposeWork
            {
                NamespaceName = candidateType.ContainingNamespace.ToString(),
                ClassName = candidateType.Name,
                DeclaredAccessibility = candidateType.DeclaredAccessibility.ToString(),

                DisposableMemberNames = disposableMembers,

                // Do I have a DisposeManaged member method? Call it.
                ImplementManaged = candidateType.ContainsCustomDisposer("DisposeManaged"),

                // Do I have a DisposeUnmanaged member method? Call it.
                ImplementUnmanaged = candidateType.ContainsCustomDisposer("DisposeUnmanaged"),
            };

            workToDo.Add(work);
        }



    }
}
