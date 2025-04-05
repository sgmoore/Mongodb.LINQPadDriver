using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MongoDB.LINQPadDriver
{
    internal class SimpleParser
    {
        public List<string> ClassNames {  get;  }
        public string SourceCode { get; }

        public SimpleParser(string filename)
        {
            SourceCode = System.IO.File.ReadAllText(filename);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceCode);
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            ClassNames = new List<string>();

            foreach (ClassDeclarationSyntax classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                {
                    ClassNames.Add(classDecl.Identifier.Text);
                }
            }
        }
    }
}

