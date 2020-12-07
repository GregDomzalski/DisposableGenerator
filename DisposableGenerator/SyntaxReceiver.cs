using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposableGenerator
{
    // Quickly reduce the number of interesting types and syntax trees to look at.
    public class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.BaseList is { } baseList)
            {
                if (baseList.Types
                    .Select(t => t.ToString())
                    .Any(s => s == "IDisposable" || s == "System.IDisposable"))
                {
                    CandidateClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
