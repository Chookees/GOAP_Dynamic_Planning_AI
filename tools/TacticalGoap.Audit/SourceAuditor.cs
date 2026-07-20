using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TacticalGoap.Audit;

/// <summary>
/// Walks production C# sources with Roslyn and emits Power-of-Ten findings.
/// </summary>
/// <remarks>
/// Limitations (false positives / negatives):
/// <list type="bullet">
/// <item><description>POT001 logical-line counts approximate brace/body lines and ignore shared partials across files.</description></item>
/// <item><description>POT007 flags any <c>new List/Dictionary/HashSet</c> in Runtime, including constructors and non-frozen init paths.</description></item>
/// <item><description>POT008 XML-doc heuristic only checks for a <c>///</c> trivia immediately above public members.</description></item>
/// <item><description>POT010 / POT011 are syntactic heuristics and miss indirect recursion or counters in outer scopes.</description></item>
/// <item><description>POT012 / POT013 inspect attribute names textually; POT013 indexes struct/class names from <c>src/</c> but cannot resolve external BCL aliases without a semantic model.</description></item>
/// <item><description>Analysis is per-file syntax only; no full compilation / semantic model is built.</description></item>
/// </list>
/// </remarks>
public sealed class SourceAuditor
{
    private const int MaximumLogicalLines = 60;
    private readonly string _repoRoot;
    private readonly List<AuditFinding> _findings = new List<AuditFinding>();
    private readonly HashSet<string> _structNames = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _classNames = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes an auditor for the given repository root.
    /// </summary>
    /// <param name="repoRoot">Absolute repository root.</param>
    public SourceAuditor(string repoRoot)
    {
        _repoRoot = repoRoot ?? throw new ArgumentNullException(nameof(repoRoot));
    }

    /// <summary>
    /// Gets the findings accumulated by the last <see cref="Run"/> call.
    /// </summary>
    public IReadOnlyList<AuditFinding> Findings => _findings;

    /// <summary>
    /// Audits all production sources under <c>src/</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collected findings.</returns>
    public IReadOnlyList<AuditFinding> Run(CancellationToken cancellationToken = default)
    {
        _findings.Clear();
        _structNames.Clear();
        _classNames.Clear();
        IReadOnlyList<string> files = RepoLocator.EnumerateProductionSources(_repoRoot);
        for (int i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IndexTypeKinds(files[i]);
        }

        for (int i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AnalyzeFile(files[i]);
        }

        return _findings;
    }

    private void IndexTypeKinds(string absolutePath)
    {
        string text = File.ReadAllText(absolutePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceText.From(text), path: absolutePath);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            string name = type.Identifier.Text;
            if (type is StructDeclarationSyntax
                || (type is RecordDeclarationSyntax record && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
            {
                _structNames.Add(name);
                continue;
            }

            if (type is ClassDeclarationSyntax
                || type is RecordDeclarationSyntax)
            {
                _classNames.Add(name);
            }
        }
    }

    private void AnalyzeFile(string absolutePath)
    {
        string relative = RepoLocator.ToRelative(_repoRoot, absolutePath);
        string text = File.ReadAllText(absolutePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceText.From(text), path: absolutePath);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        bool isRuntime = RepoLocator.IsRuntimePath(relative);

        AnalyzePragmas(relative, text);
        AnalyzeUsings(relative, root, isRuntime);
        AnalyzeFileBody(relative, root, isRuntime);

        foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            CheckMissingXmlDocs(relative, member);
        }

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            AnalyzeMethod(relative, method, isRuntime);
        }
    }

    private void AnalyzeFileBody(string relative, CompilationUnitSyntax root, bool isRuntime)
    {
        if (!isRuntime)
        {
            return;
        }

        foreach (IdentifierNameSyntax id in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (id.Parent is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            if (memberAccess.Parent is not InvocationExpressionSyntax)
            {
                continue;
            }

            if (IsLinqCall(id))
            {
                Add(relative, LineOf(id), "POT006", AuditSeverity.Error, $"Possible LINQ call '{id.Identifier.Text}' in Runtime.");
            }
        }

        foreach (ObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            CheckRuntimeCollectionGrowth(relative, creation);
        }

        foreach (ImplicitObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>())
        {
            CheckRuntimeCollectionGrowth(relative, creation);
        }
    }

    private void AnalyzePragmas(string relative, string text)
    {
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("#pragma warning disable", StringComparison.Ordinal)
                || trimmed.Contains("NoWarn", StringComparison.Ordinal))
            {
                // NoWarn in source comments / attrs is rare; pragma disable is the primary signal.
                if (trimmed.StartsWith("#pragma warning disable", StringComparison.Ordinal))
                {
                    Add(relative, i + 1, "POT009", AuditSeverity.Warning, "Warning suppression via #pragma warning disable.");
                }
            }
        }
    }

    private void AnalyzeUsings(string relative, CompilationUnitSyntax root, bool isRuntime)
    {
        if (!isRuntime)
        {
            return;
        }

        foreach (UsingDirectiveSyntax directive in root.Usings)
        {
            string name = directive.Name?.ToString() ?? string.Empty;
            if (string.Equals(name, "System.Linq", StringComparison.Ordinal)
                || name.StartsWith("System.Linq.", StringComparison.Ordinal))
            {
                Add(relative, LineOf(directive), "POT006", AuditSeverity.Error, "System.Linq import in Runtime.");
            }
        }
    }

    private void AnalyzeMethod(string relative, MethodDeclarationSyntax method, bool isRuntime)
    {
        CheckMethodLength(relative, method);
        CheckGoto(relative, method);
        CheckDynamic(relative, method);
        CheckUnsafe(relative, method);

        if (isRuntime)
        {
            CheckAsync(relative, method);
        }

        CheckWhileHeuristic(relative, method);
        CheckRecursionHeuristic(relative, method);

        bool frozen = HasFrozenAttribute(method) || HasFrozenAttribute(method.Parent);
        if (frozen)
        {
            CheckFrozenInterpolated(relative, method);
            CheckFrozenAllocations(relative, method);
        }
    }

    private void CheckMethodLength(string relative, MethodDeclarationSyntax method)
    {
        if (method.Body is null)
        {
            return;
        }

        int logical = CountLogicalLines(method.Body);
        if (logical > MaximumLogicalLines)
        {
            Add(
                relative,
                LineOf(method),
                "POT001",
                AuditSeverity.Warning,
                $"Method '{method.Identifier.Text}' has approximately {logical} logical lines (limit {MaximumLogicalLines}).");
        }
    }

    private void CheckGoto(string relative, MethodDeclarationSyntax method)
    {
        foreach (GotoStatementSyntax gotoStatement in method.DescendantNodes().OfType<GotoStatementSyntax>())
        {
            Add(relative, LineOf(gotoStatement), "POT002", AuditSeverity.Error, "goto statement is forbidden.");
        }
    }

    private void CheckDynamic(string relative, MethodDeclarationSyntax method)
    {
        foreach (IdentifierNameSyntax id in method.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (id.Identifier.Text == "dynamic")
            {
                Add(relative, LineOf(id), "POT003", AuditSeverity.Error, "dynamic typing is forbidden.");
            }
        }

        foreach (VariableDeclarationSyntax declaration in method.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            if (declaration.Type is IdentifierNameSyntax typeName && typeName.Identifier.Text == "dynamic")
            {
                Add(relative, LineOf(declaration), "POT003", AuditSeverity.Error, "dynamic local declaration is forbidden.");
            }
        }
    }

    private void CheckUnsafe(string relative, MethodDeclarationSyntax method)
    {
        if (method.Modifiers.Any(SyntaxKind.UnsafeKeyword))
        {
            Add(relative, LineOf(method), "POT004", AuditSeverity.Error, "unsafe method is forbidden.");
        }

        foreach (UnsafeStatementSyntax unsafeStatement in method.DescendantNodes().OfType<UnsafeStatementSyntax>())
        {
            Add(relative, LineOf(unsafeStatement), "POT004", AuditSeverity.Error, "unsafe block is forbidden.");
        }

        foreach (PointerTypeSyntax pointer in method.DescendantNodes().OfType<PointerTypeSyntax>())
        {
            Add(relative, LineOf(pointer), "POT004", AuditSeverity.Error, "pointer type is forbidden.");
        }
    }

    private void CheckAsync(string relative, MethodDeclarationSyntax method)
    {
        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            Add(relative, LineOf(method), "POT005", AuditSeverity.Error, "async methods are forbidden in Runtime.");
        }

        string returnType = method.ReturnType.ToString();
        if (returnType is "Task" or "ValueTask"
            || returnType.StartsWith("Task<", StringComparison.Ordinal)
            || returnType.StartsWith("ValueTask<", StringComparison.Ordinal)
            || returnType.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal))
        {
            Add(relative, LineOf(method), "POT005", AuditSeverity.Error, $"Task-based return type '{returnType}' is forbidden in Runtime.");
        }
    }

    private void CheckRuntimeCollectionGrowth(string relative, ExpressionSyntax creation)
    {
        string typeName = ExtractCreatedTypeName(creation);
        if (typeName.Length == 0)
        {
            return;
        }

        if (IsGrowthCollection(typeName))
        {
            Add(
                relative,
                LineOf(creation),
                "POT007",
                AuditSeverity.Warning,
                $"Potential Runtime collection growth via 'new {typeName}'. Prefer fixed arrays sized at freeze.");
        }
    }

    private void CheckMissingXmlDocs(string relative, MemberDeclarationSyntax member)
    {
        if (!IsPublicMember(member))
        {
            return;
        }

        if (HasXmlDoc(member))
        {
            return;
        }

        string name = DescribeMember(member);
        Add(
            relative,
            LineOf(member),
            "POT008",
            AuditSeverity.Warning,
            $"Public member '{name}' appears to lack XML documentation trivia.");
    }

    private void CheckWhileHeuristic(string relative, MethodDeclarationSyntax method)
    {
        foreach (WhileStatementSyntax whileStatement in method.DescendantNodes().OfType<WhileStatementSyntax>())
        {
            if (HasLoopCounterHeuristic(whileStatement))
            {
                continue;
            }

            Add(
                relative,
                LineOf(whileStatement),
                "POT010",
                AuditSeverity.Warning,
                "while-loop without an obvious local counter increment heuristic.");
        }
    }

    private void CheckRecursionHeuristic(string relative, MethodDeclarationSyntax method)
    {
        string name = method.Identifier.Text;
        if (name is "Equals" or "GetHashCode" or "CompareTo" or "ToString")
        {
            return;
        }

        foreach (InvocationExpressionSyntax invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is IdentifierNameSyntax id && id.Identifier.Text == name)
            {
                Add(relative, LineOf(invocation), "POT011", AuditSeverity.Warning, $"Possible direct recursion calling '{name}'.");
            }

            if (invocation.Expression is MemberAccessExpressionSyntax member
                && member.Name.Identifier.Text == name
                && member.Expression is ThisExpressionSyntax)
            {
                Add(relative, LineOf(invocation), "POT011", AuditSeverity.Warning, $"Possible direct recursion calling this.{name}.");
            }
        }
    }

    private void CheckFrozenInterpolated(string relative, MethodDeclarationSyntax method)
    {
        foreach (InterpolatedStringExpressionSyntax interpolated in method.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            Add(
                relative,
                LineOf(interpolated),
                "POT012",
                AuditSeverity.Error,
                $"Interpolated string in [FrozenRuntimePath] method '{method.Identifier.Text}'.");
        }
    }

    private void CheckFrozenAllocations(string relative, MethodDeclarationSyntax method)
    {
        foreach (ObjectCreationExpressionSyntax creation in method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            string typeName = StripNamespace(creation.Type.ToString());
            if (IsLikelyValueTypeCreation(typeName))
            {
                continue;
            }

            AuditSeverity severity = _classNames.Contains(typeName) || IsKnownReferenceTypeName(typeName)
                ? AuditSeverity.Error
                : AuditSeverity.Warning;

            Add(
                relative,
                LineOf(creation),
                "POT013",
                severity,
                $"Possible managed allocation 'new {creation.Type}' inside [FrozenRuntimePath] method '{method.Identifier.Text}'.");
        }

        foreach (ImplicitObjectCreationExpressionSyntax creation in method.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>())
        {
            Add(
                relative,
                LineOf(creation),
                "POT013",
                AuditSeverity.Warning,
                $"Implicit 'new()' inside [FrozenRuntimePath] method '{method.Identifier.Text}' (type unresolved without semantic model).");
        }

        foreach (ArrayCreationExpressionSyntax arrayCreation in method.DescendantNodes().OfType<ArrayCreationExpressionSyntax>())
        {
            Add(
                relative,
                LineOf(arrayCreation),
                "POT013",
                AuditSeverity.Error,
                $"Array allocation inside [FrozenRuntimePath] method '{method.Identifier.Text}'.");
        }

        foreach (ImplicitArrayCreationExpressionSyntax implicitArray in method.DescendantNodes().OfType<ImplicitArrayCreationExpressionSyntax>())
        {
            Add(
                relative,
                LineOf(implicitArray),
                "POT013",
                AuditSeverity.Error,
                $"Implicit array allocation inside [FrozenRuntimePath] method '{method.Identifier.Text}'.");
        }
    }

    private void Add(string relative, int line, string ruleId, AuditSeverity severity, string description)
    {
        _findings.Add(new AuditFinding(relative, line, ruleId, severity, description));
    }

    private static int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static int CountLogicalLines(BlockSyntax body)
    {
        int count = 0;
        foreach (StatementSyntax statement in body.Statements)
        {
            count += CountStatementLines(statement);
        }

        return count;
    }

    private static int CountStatementLines(StatementSyntax statement)
    {
        if (statement is BlockSyntax block)
        {
            int nested = 0;
            foreach (StatementSyntax child in block.Statements)
            {
                nested += CountStatementLines(child);
            }

            return nested;
        }

        if (statement is IfStatementSyntax ifStatement)
        {
            int total = 1 + CountStatementLines(ifStatement.Statement);
            if (ifStatement.Else is not null)
            {
                total += CountStatementLines(ifStatement.Else.Statement);
            }

            return total;
        }

        if (statement is ForStatementSyntax forStatement)
        {
            return 1 + CountStatementLines(forStatement.Statement);
        }

        if (statement is WhileStatementSyntax whileStatement)
        {
            return 1 + CountStatementLines(whileStatement.Statement);
        }

        if (statement is ForEachStatementSyntax forEach)
        {
            return 1 + CountStatementLines(forEach.Statement);
        }

        if (statement is SwitchStatementSyntax switchStatement)
        {
            int total = 1;
            foreach (SwitchSectionSyntax section in switchStatement.Sections)
            {
                foreach (StatementSyntax child in section.Statements)
                {
                    total += CountStatementLines(child);
                }
            }

            return total;
        }

        if (statement is TryStatementSyntax tryStatement)
        {
            int total = CountStatementLines(tryStatement.Block);
            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                total += CountStatementLines(catchClause.Block);
            }

            if (tryStatement.Finally is not null)
            {
                total += CountStatementLines(tryStatement.Finally.Block);
            }

            return total;
        }

        return 1;
    }

    private static bool HasFrozenAttribute(SyntaxNode? node)
    {
        if (node is not MemberDeclarationSyntax member)
        {
            return false;
        }

        foreach (AttributeListSyntax list in member.AttributeLists)
        {
            foreach (AttributeSyntax attribute in list.Attributes)
            {
                string name = attribute.Name.ToString();
                if (name is "FrozenRuntimePath" or "FrozenRuntimePathAttribute"
                    || name.EndsWith(".FrozenRuntimePath", StringComparison.Ordinal)
                    || name.EndsWith(".FrozenRuntimePathAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsLinqCall(IdentifierNameSyntax id)
    {
        string name = id.Identifier.Text;
        return name is "Select" or "Where" or "SelectMany" or "Any" or "All" or "First"
            or "FirstOrDefault" or "Single" or "SingleOrDefault" or "Last" or "LastOrDefault"
            or "Count" or "Sum" or "Average" or "Min" or "Max" or "OrderBy" or "OrderByDescending"
            or "ThenBy" or "ThenByDescending" or "GroupBy" or "Join" or "Distinct" or "ToArray"
            or "ToList" or "ToDictionary" or "ToHashSet" or "Aggregate" or "Skip" or "Take"
            or "DefaultIfEmpty" or "Concat" or "Zip" or "AsEnumerable";
    }

    private static string ExtractCreatedTypeName(ExpressionSyntax creation)
    {
        if (creation is ObjectCreationExpressionSyntax objectCreation)
        {
            return objectCreation.Type.ToString();
        }

        return string.Empty;
    }

    private static bool IsGrowthCollection(string typeName)
    {
        return typeName.StartsWith("List<", StringComparison.Ordinal)
            || typeName.StartsWith("Dictionary<", StringComparison.Ordinal)
            || typeName.StartsWith("HashSet<", StringComparison.Ordinal)
            || typeName is "List" or "Dictionary" or "HashSet"
            || typeName.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal)
            || typeName.StartsWith("System.Collections.Generic.Dictionary<", StringComparison.Ordinal)
            || typeName.StartsWith("System.Collections.Generic.HashSet<", StringComparison.Ordinal);
    }

    private static bool IsPublicMember(MemberDeclarationSyntax member)
    {
        if (member.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return true;
        }

        // Nested types without explicit modifiers may still be public when parent is public;
        // the heuristic intentionally stays conservative to reduce false negatives.
        return false;
    }

    private static bool HasXmlDoc(MemberDeclarationSyntax member)
    {
        foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                return true;
            }

            if (trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia))
            {
                return true;
            }
        }

        string leading = member.GetLeadingTrivia().ToFullString();
        return leading.Contains("///", StringComparison.Ordinal);
    }

    private static string DescribeMember(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            TypeDeclarationSyntax type => type.Identifier.Text,
            EnumDeclarationSyntax enumeration => enumeration.Identifier.Text,
            DelegateDeclarationSyntax del => del.Identifier.Text,
            EventDeclarationSyntax evt => evt.Identifier.Text,
            ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "field",
            _ => member.Kind().ToString(),
        };
    }

    private static bool HasLoopCounterHeuristic(WhileStatementSyntax whileStatement)
    {
        string condition = whileStatement.Condition.ToString();
        if (condition.Contains('<', StringComparison.Ordinal)
            || condition.Contains("<=", StringComparison.Ordinal)
            || condition.Contains("Count", StringComparison.Ordinal)
            || condition.Contains("Length", StringComparison.Ordinal)
            || condition.Contains("Maximum", StringComparison.Ordinal)
            || condition.Contains("AiHardLimits", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (SyntaxNode node in whileStatement.Statement.DescendantNodesAndSelf())
        {
            if (node.IsKind(SyntaxKind.PostIncrementExpression)
                || node.IsKind(SyntaxKind.PostDecrementExpression)
                || node.IsKind(SyntaxKind.PreIncrementExpression)
                || node.IsKind(SyntaxKind.PreDecrementExpression))
            {
                return true;
            }

            if (node is AssignmentExpressionSyntax assignment
                && assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLikelyValueTypeCreation(string typeName)
    {
        if (_structNames.Contains(typeName))
        {
            return true;
        }

        if (typeName.EndsWith("Id", StringComparison.Ordinal)
            || typeName.EndsWith("Result", StringComparison.Ordinal)
            || typeName.EndsWith("Status", StringComparison.Ordinal)
            || typeName.EndsWith("Record", StringComparison.Ordinal)
            || typeName.EndsWith("Tick", StringComparison.Ordinal)
            || typeName.EndsWith("Span", StringComparison.Ordinal)
            || typeName.Contains("Struct", StringComparison.Ordinal))
        {
            return true;
        }

        return typeName is "Int2" or "WorldStateMask" or "ActionCandidate" or "PlanStep";
    }

    private static bool IsKnownReferenceTypeName(string typeName)
    {
        return typeName is "string" or "object" or "Exception" or "ArgumentException"
            or "ArgumentOutOfRangeException" or "InvalidOperationException"
            or "NotSupportedException" or "NotImplementedException"
            || typeName.StartsWith("List<", StringComparison.Ordinal)
            || typeName.StartsWith("Dictionary<", StringComparison.Ordinal)
            || typeName.StartsWith("HashSet<", StringComparison.Ordinal)
            || typeName.StartsWith("StringBuilder", StringComparison.Ordinal);
    }

    private static string StripNamespace(string typeName)
    {
        int generic = typeName.IndexOf('<', StringComparison.Ordinal);
        string head = generic >= 0 ? typeName[..generic] : typeName;
        int dot = head.LastIndexOf('.');
        string simple = dot >= 0 ? head[(dot + 1)..] : head;
        return generic >= 0 ? simple + typeName[generic..] : simple;
    }
}
