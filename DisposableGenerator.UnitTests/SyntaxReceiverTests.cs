using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DisposableGenerator.UnitTests
{
    public class SyntaxReceiverTests
    {
        [Fact]
        public void OnVisitSyntaxNode_SyntaxWithoutBaseList_FindsNoCandidates()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText(@"
                public class TestClass
                {

                }
                ");
            var classNode = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var syntaxReceiver = new SyntaxReceiver();

            // Act
            syntaxReceiver.OnVisitSyntaxNode(classNode);

            // Assert
            Assert.False(syntaxReceiver.CandidateClasses.Any());
        }

        [Fact]
        public void OnVisitSyntaxNode_SyntaxWithUninterestingBase_FindsNoCandidates()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText(@"
                public class TestClass : IEnumerable
                {

                }
                ");
            var classNode = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var syntaxReceiver = new SyntaxReceiver();

            // Act
            syntaxReceiver.OnVisitSyntaxNode(classNode);

            // Assert
            Assert.False(syntaxReceiver.CandidateClasses.Any());
        }

        [Fact]
        public void OnVisitSyntaxNode_SyntaxWithIDisposableBase_AddsSingleCandidate()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText(@"
                public class TestClass : IDisposable
                {

                }
                ");
            var classNode = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var syntaxReceiver = new SyntaxReceiver();

            // Act
            syntaxReceiver.OnVisitSyntaxNode(classNode);

            // Assert
            Assert.Single(syntaxReceiver.CandidateClasses);
        }

        [Fact]
        public void OnVisitSyntaxNode_SyntaxWithFullyQualifiedIDisposableBase_AddsSingleCandidate()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText(@"
                public class TestClass : System.IDisposable
                {

                }
                ");
            var classNode = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var syntaxReceiver = new SyntaxReceiver();

            // Act
            syntaxReceiver.OnVisitSyntaxNode(classNode);

            // Assert
            Assert.Single(syntaxReceiver.CandidateClasses);
        }

        [Fact]
        public void OnVisitSyntaxNode_MultipleVisitsWithInterestingClass_YieldsMultipleCandidates()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText(@"
                public class TestClass : System.IDisposable
                {

                }
                ");
            var classNode = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var syntaxReceiver = new SyntaxReceiver();

            // Act
            syntaxReceiver.OnVisitSyntaxNode(classNode);
            syntaxReceiver.OnVisitSyntaxNode(classNode);
            syntaxReceiver.OnVisitSyntaxNode(classNode);

            // Assert
            Assert.Equal(3, syntaxReceiver.CandidateClasses.Count);
        }
    }
}