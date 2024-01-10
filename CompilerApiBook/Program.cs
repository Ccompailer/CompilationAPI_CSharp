using System;
using System.Collections.Generic;
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
                         public void Method1() { } 
                         public void Method2(int a, Guid b) { } 
                         public void Method3(string a) { } 
                         public void Method4(ref string a) { } 
                        }";

            var tree = SyntaxFactory.ParseSyntaxTree(code);

            PrintMethodContentViaTree(tree);
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
}