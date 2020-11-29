using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DisposableGenerator
{
    // Quickly reduce the number of interesting types and syntax trees to look at.
    public class DisposeSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax
                {
                    BaseList:
                    {
                        Types:
                        {
                            Count: >= 1
                        }
                    }
                } classDeclarationSyntax)
            {
                CandidateClasses.Add(classDeclarationSyntax);
            }
        }
    }
}
