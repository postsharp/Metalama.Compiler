﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Compiler.UnitTests
{
    public class PdbTests : CSharpPDBTestBase
    {
        [Fact]
        public void EmitDebugInfoForTransformedSource()
        {
            string programCode = @"
using System;

class Program
{
    void Main()
    {
        string hello = ""Hello"";
        string world = ""world"";
        string helloWorld = $""{hello} {world}!"";
        Console.WriteLine(helloWorld);
    }
}
".NormalizeLineEndings();
            string libraryCode = @"
using System;

class Library
{
    void M()
    {
        Console.WriteLine(""step {0}"");
    }
}
".NormalizeLineEndings();

            var programTree = ParseSyntaxTree(programCode, path: "Program.cs", encoding: Encoding.UTF8);
            var libraryTree = ParseSyntaxTree(libraryCode, path: "Library.cs", encoding: Encoding.UTF8);

            // Syntax trees must be added in alphabetical order because WindowsPDB does not reorder trees, but PortablePDB does reorder them alphabetically.
            Compilation comp = CSharpCompilation.Create("Compilation", new[] { libraryTree, programTree }, new[] { MscorlibRef }, options: TestOptions.DebugDll);
            
            comp = MetalamaCompilerTest.ExecuteTransformer(comp, new InterleaveStatementsTransformer());
            
            
            // Check that we can emit.
            var result = comp.Emit(Stream.Null, pdbStream: Stream.Null);
            result.Diagnostics.Verify();
            Assert.True(result.Success);

            var programHash = CryptographicHashProvider.ComputeSha1(Encoding.UTF8.GetBytesWithPreamble(programTree.ToString())).ToArray();
            var libraryHash = CryptographicHashProvider.ComputeSha1(Encoding.UTF8.GetBytesWithPreamble(libraryTree.ToString())).ToArray();

            comp.VerifyPdb($@"
<symbols>
  <files>
    <file id=""1"" name=""Library.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""D7-A8-FA-7A-39-91-65-8D-15-11-B5-17-91-FA-DA-06-F4-32-9E-91"" />
    <file id=""2"" name=""Program.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""C7-BE-1D-CD-D3-47-76-98-55-B9-9E-28-7F-42-BE-68-7F-F3-D7-07"" />
  </files>
  <methods>
    <method containingType=""Library"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0xc"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""Library"" methodName=""M"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""85"" />
          <slot kind=""0"" offset=""160"" />
          <slot kind=""0"" offset=""235"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0x1d"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""32"" document=""2"" />
        <entry offset=""0x23"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0x34"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""32"" document=""2"" />
        <entry offset=""0x3a"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0x4b"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""49"" document=""2"" />
        <entry offset=""0x5d"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0x6e"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""39"" document=""2"" />
        <entry offset=""0x75"" hidden=""true"" document=""2"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x76"">
        <local name=""hello"" il_index=""0"" il_start=""0x0"" il_end=""0x76"" attributes=""0"" />
        <local name=""world"" il_index=""1"" il_start=""0x0"" il_end=""0x76"" attributes=""0"" />
        <local name=""helloWorld"" il_index=""2"" il_start=""0x0"" il_end=""0x76"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        class InterleaveStatementsTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                var trees = context.Compilation.SyntaxTrees.ToList();
                var programTree = trees.Single(t=>t.FilePath=="Program.cs");
                var libraryTree = trees.Single(t=>t.FilePath=="Library.cs");

                var stepStatement = libraryTree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().Single();

                var rewriter = new Rewriter(stepStatement);

                context.ReplaceSyntaxTree(programTree, programTree.WithRootAndOptions(rewriter.Visit(programTree.GetRoot()), programTree.Options));
            }

            class Rewriter : CSharpSyntaxRewriter
            {
                private readonly Func<int, StatementSyntax> getStepStatement = null!;

                public Rewriter(StatementSyntax stepStatement)
                {
                    if (stepStatement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
                        getStepStatement = i => stepStatement.ReplaceNode(invocation, invocation.AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))));
                }

                public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
                {
                    var statements = new List<StatementSyntax>();

                    statements.Add(ParseStatement("Console.WriteLine(\"start\");"));

                    for (int i = 0; i < node.Body!.Statements.Count; i++)
                    {
                        statements.Add(getStepStatement(i));
                        statements.Add(node.Body.Statements[i]);
                    }

                    return node.WithBody(Block(statements));
                }
            }
        }

        [Fact]
        public void ExpressionBodiedPropertyToStatementBodied()
        {
            string code = @"
class C
{
    string p;
    string P => ""foo"";
}
".NormalizeLineEndings();

            var tree = ParseSyntaxTree(code, path: "C.cs", encoding: Encoding.UTF8);

            Compilation comp = CSharpCompilation.Create("Compilation", new[] { tree }, new[] { MscorlibRef }, options: TestOptions.DebugDll);

            comp = MetalamaCompilerTest.ExecuteTransformer(comp, new LazyPropertyTransformer());

            var result = comp.Emit(Stream.Null, pdbStream: Stream.Null);
            result.Diagnostics.Verify();
            Assert.True(result.Success);

            var codeHash = CryptographicHashProvider.ComputeSha1(Encoding.UTF8.GetBytesWithPreamble(tree.ToString())).ToArray();

            // none of the statements in the generated code can be mapped to original source, so there are no sequence points
            
            // #29513 RoslynEx 4.0: ENC debug info is emitted where sequence points are not expected
            // Originally, there's been `<methods/>` instead of `<methods>...</methods>`.
            
            comp.VerifyPdb($@"
<symbols>
  <files>
    <file id=""1"" name=""C.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""{BitConverter.ToString(codeHash)}"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""1"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        class LazyPropertyTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                var rewriter = new Rewriter();
                var compilation = context.Compilation;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    context.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options));
                }
            }

            class Rewriter : CSharpSyntaxRewriter
            {
                public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
                {
                    string fieldName = char.ToLowerInvariant(node.Identifier.ValueText[0]) + node.Identifier.ValueText[1..];

                    var expression = node.ExpressionBody!.Expression;

                    // if (field == null)
                    //     field = expression;
                    // return field;

                    var block = Block(
                        IfStatement(
                            BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(fieldName), LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(fieldName), expression))),
                        ReturnStatement(IdentifierName(fieldName)));

                    var newNode = node.WithExpressionBody(null).WithSemicolonToken(default)
                        .AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, block));

                    return newNode;
                }
            }
        }
    }
}
