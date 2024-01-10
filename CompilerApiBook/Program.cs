using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompilerApiBook
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var code = @" 
                        using System; 
                        public class ContainsMethods 
                        { 
                         private void Method1() { } 
                         internal void Method2(int a, Guid b) { } 
                         protected void Method3(string a) { } 
                         public void Method4(ref string a) { }
                         protected internal void Method5(long a) { } 
                        }";

            var tree = SyntaxFactory.ParseSyntaxTree(code);

            ModifyTreeViaTree(tree);
        }

        private static void ModifyTreeViaTree(SyntaxTree tree)
        {
            Console.Out.WriteLine(nameof(Program.ModifyTreeViaTree));
            Console.Out.WriteLine(tree);

            var methods = tree
                .GetRoot()
                .DescendantNodes(_ => true)
                .OfType<MethodDeclarationSyntax>();

            var newTree = tree
                .GetRoot()
                .ReplaceNodes(
                    methods,
                    (method, methodWithReplacements) =>
                    {
                        var visibilityTokens = method.DescendantTokens(_ => true)
                            .Where(x => x.IsKind(SyntaxKind.PublicKeyword)
                                        || x.IsKind(SyntaxKind.PrivateKeyword)
                                        || x.IsKind(SyntaxKind.ProtectedKeyword)
                                        || x.IsKind(SyntaxKind.InternalKeyword)).ToImmutableList();

                        if (!visibilityTokens.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
                        {
                            var tokenPosition = 0;

                            var newMethod = method.ReplaceTokens(
                                visibilityTokens,
                                (x, _) =>
                                {
                                    ++tokenPosition;

                                    return tokenPosition == 1
                                        ? SyntaxFactory.Token(
                                            x.LeadingTrivia,
                                            SyntaxKind.PublicKeyword,
                                            x.TrailingTrivia)
                                        : new SyntaxToken();
                                });

                            return newMethod;
                        }

                        return method;
                    });

            Console.Out.WriteLine(newTree);
        }

        private static void PrintMethodContentViaTree(SyntaxTree tree)
        {
            var methods = tree.GetRoot()
                .DescendantNodes(_ => true)
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var parameters = new List<string>();

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    parameters.Add($"{parameter.Type.ToFullString().Trim()} {parameter.Identifier.Text}");
                }

                Console.Out.WriteLine($"{method.Identifier.Text}({string.Join(", ", parameters)})");
            }
        }

        private static void PrintMethodContentViaSemanticModel(SyntaxTree tree)
        {
            Console.Out.WriteLine(nameof(Program.PrintMethodContentViaSemanticModel));

            var compilation = CSharpCompilation.Create(
                "MethodContent",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                });

            var model = compilation.GetSemanticModel(tree, true);

            var methods = tree
                .GetRoot()
                .DescendantNodes(_ => true)
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var methodInfo = model.GetDeclaredSymbol(method);

                var parameters = new List<string>();

                foreach (var parameter in methodInfo.Parameters)
                {
                    var isRef = parameter.RefKind == RefKind.Ref ? "ref" : string.Empty;
                    parameters.Add($"{isRef} {parameter.Type.Name} {parameter.Name}");
                }

                Console.Out.WriteLine(
                    $"{methodInfo.Name}({string.Join(", ", parameters)})");
            }
        }
    }

    public sealed class MethodWalker
        : CSharpSyntaxWalker
    {
        public MethodWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
            : base(depth)
        {
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var parameters = new List<string>();

            foreach (var parameter in node.ParameterList.Parameters)
            {
                parameters.Add($"{parameter.Type.ToFullString().Trim()} {parameter.Identifier.Text}");
            }

            Console.Out.WriteLine($"{node.Identifier.Text}({string.Join(", ", parameters)})");

            base.VisitMethodDeclaration(node);
        }
    }

    public sealed class MethodVisitor
        : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var visibilityTokens = node
                .DescendantTokens(_ => true)
                .Where(x => x.IsKind(SyntaxKind.PublicKeyword)
                            || x.IsKind(SyntaxKind.PrivateKeyword)
                            || x.IsKind(SyntaxKind.ProtectedKeyword)
                            || x.IsKind(SyntaxKind.InternalKeyword)).ToImmutableList();

            if (!visibilityTokens.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            {
                var tokenPosition = 0;

                var newMethod = node.ReplaceTokens(
                    visibilityTokens,
                    (x, _) =>
                    {
                        ++tokenPosition;

                        return tokenPosition == 1
                            ? SyntaxFactory.Token(
                                x.LeadingTrivia,
                                SyntaxKind.PublicKeyword,
                                x.TrailingTrivia)
                            : new SyntaxToken();
                    });
                
                return newMethod;
            }
            
            return node;
        }
    }
}