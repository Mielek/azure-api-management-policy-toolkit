# Plan: Full CompilerUtils Refactor with Result Types and Interface Segregation

A clean-break refactor splitting the 290-line `CompilerUtils` god class into focused, single-responsibility classes. Introduces hand-rolled `Result<T>` with multi-diagnostic accumulation, split interfaces following ISP (pure processors vs semantic-analysis-aware), and comprehensive unit tests. The 63+ policy compiler consumers will adopt the new pattern, with existing integration tests catching regressions during migration.

## Steps

1. **Create `Result<T>` infrastructure** in [Core/Results/Result.cs](src/Core/) - Hand-roll `Result<T>` as a readonly struct with `T? Value`, `IReadOnlyList<Diagnostic> Diagnostics`, `bool IsSuccess` (true when no diagnostics or all are non-errors). Include factories: `Success(T value)`, `Failure(params Diagnostic[] diagnostics)`, `Failure(IEnumerable<Diagnostic> diagnostics)`. Add combinators: `Map<TResult>(Func<T, TResult>)` for transforming success values, `Bind<TResult>(Func<T, Result<TResult>>)` for chaining operations, `Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<Diagnostic>, TResult> onFailure)` for exhaustive handling. Add static `Combine(params Result<T>[] results)` that accumulates all diagnostics and returns success only if all inputs succeed. Include implicit conversion `operator Result<T>(T value)` for ergonomic usage.

2. **Create `ResultExtensions`** in [Core/Results/ResultExtensions.cs](src/Core/) - Add `Collect<T>(this IEnumerable<Result<T>>)` returning `Result<IReadOnlyList<T>>` that accumulates all diagnostics and values. Add `ReportAll(this Result<T>, IDocumentCompilationContext)` that calls `context.Report()` for each diagnostic, bridging functional results to the side-effect world at policy compiler boundaries. Add `GetValueOrDefault<T>(this Result<T>, T defaultValue)` for cases where fallback is acceptable.

3. **Create `ICompilationContext` interface** in [Core/Compiling/ICompilationContext.cs](src/Core/Compiling/) - Minimal interface with single property `Compilation Compilation { get; }` exposing the Roslyn `Microsoft.CodeAnalysis.Compilation` for semantic model access. This interface exists solely for processors that need symbol resolution (method bodies, constant field values).

4. **Update `IDocumentCompilationContext`** in [IDocumentCompilationContext.cs](src/Core/Compiling/IDocumentCompilationContext.cs) - Add `: ICompilationContext` to interface declaration. No other changes needed; `DocumentCompilationContext` implementation already has `Compilation` property. Policy compiler method signatures remain unchanged, receiving `IDocumentCompilationContext` which now satisfies `ICompilationContext` through inheritance.

5. **Extract `InitializerValue` to own file** in [Core/Compiling/InitializerValue.cs](src/Core/Compiling/) - Move the class from CompilerUtils.cs line 289-318 to dedicated file. Convert to `record` for value semantics and immutability. Keep existing properties: `string? Name`, `string? Value`, `string? Type`, `IReadOnlyList<InitializerValue>? Values`, `IReadOnlyDictionary<string, InitializerValue>? NamedValues`, `SyntaxNode? Syntax`. Add factory methods for clarity: `static ForScalar(string value, SyntaxNode syntax)`, `static ForArray(IReadOnlyList<InitializerValue> values, SyntaxNode syntax)`, `static ForObject(IReadOnlyDictionary<string, InitializerValue> namedValues, string typeName, SyntaxNode syntax)`.

6. **Create `InitializerProcessor`** in [Core/Compiling/InitializerProcessor.cs](src/Core/Compiling/) - Pure static class with **no context parameter** since it only performs syntax-level processing. Extract all `ProcessInitializer` overloads: `Process(ObjectCreationExpressionSyntax)` returning `Result<InitializerValue>` with object's named values, `Process(ArrayCreationExpressionSyntax)` returning array's sequential values, `Process(CollectionExpressionSyntax)` for collection expressions, `Process(ImplicitArrayCreationExpressionSyntax)` for implicit arrays. Add dispatcher `Process(ExpressionSyntax)` that routes by expression type, returning `Failure` with appropriate diagnostic for unsupported expression types.

7. **Create `CodeExtractor`** in [Core/Compiling/CodeExtractor.cs](src/Core/Compiling/) - Takes `ICompilationContext` parameter for semantic model access to resolve symbols. Extract constants: `const string ExpressionBodyPrefix = "@"`, `const string ExpressionStatementPrefix = "@("`, `const string ExpressionStatementSuffix = ")"`. Implement `Extract(InvocationExpressionSyntax syntax, ICompilationContext context)` returning `Result<string>` - uses `context.Compilation.GetSemanticModel()` to resolve method symbol, find method declaration, extract body, format as `@{body}` or `@(expression)`. Implement `Extract(MemberAccessExpressionSyntax syntax, ICompilationContext context)` returning `Result<string>` - resolves field symbol, extracts constant value. Return `Failure` with `CompilationErrors.NotSupportedExpressionType` for unresolvable symbols.

8. **Create `ExpressionProcessor`** in [Core/Compiling/ExpressionProcessor.cs](src/Core/Compiling/) - Central dispatcher taking `ICompilationContext` to pass through when needed. Implement `Process(ExpressionSyntax expression, ICompilationContext context)` returning `Result<string>` with switch on expression type: `LiteralExpressionSyntax` → return `Success(syntax.Token.ValueText)` (pure, no context needed), `InvocationExpressionSyntax` → delegate to `CodeExtractor.Extract()`, `MemberAccessExpressionSyntax` → delegate to `CodeExtractor.Extract()`, `default` → return `Failure` with `CompilationErrors.NotSupportedExpressionType`. This replaces `ProcessParameter` extension method.

9. **Create `ConfigurationExtractor`** in [Core/Compiling/ConfigurationExtractor.cs](src/Core/Compiling/) - Pure static class with **no context parameter**. Implement `Extract<TConfig>(InvocationExpressionSyntax node)` returning `Result<IReadOnlyDictionary<string, InitializerValue>>` - validates argument count is exactly 1, validates argument is `ObjectCreationExpressionSyntax`, validates type name matches `typeof(TConfig).Name`, delegates to `InitializerProcessor.Process()` for actual extraction. Implement `ExtractFromExpression<TConfig>(ExpressionSyntax expression)` for direct expression processing. Remove the rogue `throw new InvalidOperationException("Invalid expression")` from original code - return `Failure` with diagnostic instead. All validation failures return `Failure` with appropriate `CompilationErrors` diagnostic.

10. **Create `XElementExtensions`** in [Core/Compiling/XElementExtensions.cs](src/Core/Compiling/) - Move `AddAttribute(this XElement element, IReadOnlyDictionary<string, InitializerValue> values, string name, string attributeName)` helper that returns `bool` indicating if attribute was added. Move `Cleared(this SyntaxNode node)` that uses `TriviaRemoverRewriter` to strip trivia and normalize whitespace. These are thin utilities that don't warrant their own processor classes.

11. **Migrate simple policy compilers first** - Update [SetStatusCompiler.cs](src/Core/Compiling/Policy/), [SetVariableCompiler.cs](src/Core/Compiling/Policy/), [AuthenticationBasicCompiler.cs](src/Core/Compiling/Policy/), [SetBodyCompiler.cs](src/Core/Compiling/Policy/) to validate the new pattern. Replace `node.ArgumentList.Arguments[0].Expression.ProcessParameter(context)` with `ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context)`. Handle result: `if (!result.IsSuccess) { result.ReportAll(context); return; }` then use `result.Value`. Run integration tests after each compiler to catch regressions.

12. **Migrate medium-complexity policy compilers** - Update [SetHeaderCompiler.cs](src/Core/Compiling/Policy/), [CacheLookupCompiler.cs](src/Core/Compiling/Policy/), [AuthenticationCertificateCompiler.cs](src/Core/Compiling/Policy/), and similar compilers that use `TryExtractingConfigParameter`. Replace with `ConfigurationExtractor.Extract<TConfig>(node)` and result handling. Update static helper methods like `HandleBasicAuthentication` to accept `Result` or extracted dictionary rather than context.

13. **Migrate complex policy compilers** - Update [SendRequestCompiler.cs](src/Core/Compiling/Policy/), [CacheLookupValueCompiler.cs](src/Core/Compiling/Policy/), [ReturnResponseCompiler.cs](src/Core/Compiling/Policy/), [WaitCompiler.cs](src/Core/Compiling/Policy/) that orchestrate multiple sub-operations. Use `Result.Combine()` or `Collect()` to accumulate diagnostics from multiple extractions before reporting. Ensure child context creation for nested blocks continues to work.

14. **Migrate remaining policy compilers** in [Core/Compiling/Policy/](src/Core/Compiling/Policy/) - Systematic update of all remaining compilers following established pattern. Approximately 50+ files including quota, rate-limit, cors, validate-jwt, mock-response, etc.

15. **Update `IfStatementCompiler`** in [Core/Compiling/Syntax/IfStatementCompiler.cs](src/Core/Compiling/Syntax/) - This compiler directly calls `CompilerUtils.FindCode()` static method. Update to use `CodeExtractor.Extract()` with result handling.

16. **Delete CompilerUtils.cs** - Remove [CompilerUtils.cs](src/Core/Compiling/CompilerUtils.cs) after all 63+ consumers migrated and integration tests pass. Verify no remaining references via grep search.

17. **Add unit tests for Result infrastructure** - Create [Test.Core/Results/ResultTests.cs](test/Test.Core/) testing: `Success` factory preserves value and has empty diagnostics, `Failure` factory captures all diagnostics, `IsSuccess` returns correct value, `Map` transforms value on success and preserves diagnostics on failure, `Bind` chains operations correctly, `Match` calls correct branch, `Combine` accumulates all diagnostics and succeeds only when all inputs succeed, implicit conversion works, `Collect` aggregates enumerable results correctly, `ReportAll` calls context.Report for each diagnostic.

18. **Add unit tests for InitializerProcessor** - Create [Test.Core/Compiling/InitializerProcessorTests.cs](test/Test.Core/) testing: object initializer with named properties extracts to dictionary, nested object initializers handled correctly, array initializer extracts sequential values, collection expression extracts values, implicit array extracts values, empty initializers return empty collections, unsupported expression types return failure with diagnostic, syntax node preserved in result for error location reporting.

19. **Add unit tests for CodeExtractor** - Create [Test.Core/Compiling/CodeExtractorTests.cs](test/Test.Core/) with mock `ICompilationContext` testing: expression-bodied method extracts with `@` prefix, block-bodied method extracts with `@()` wrapper, const field access extracts literal value, non-const field returns failure, unresolvable symbol returns failure with diagnostic, multiline method body normalized correctly, lambda expressions handled.

20. **Add unit tests for ExpressionProcessor** - Create [Test.Core/Compiling/ExpressionProcessorTests.cs](test/Test.Core/) testing: string literal extracts value text, numeric literal extracts value, boolean literal extracts value, invocation delegates to CodeExtractor, member access delegates to CodeExtractor, unsupported expression type (e.g., binary expression) returns failure, null literal handled appropriately.

21. **Add unit tests for ConfigurationExtractor** - Create [Test.Core/Compiling/ConfigurationExtractorTests.cs](test/Test.Core/) testing: valid config object extracts successfully, wrong argument count returns failure, non-object-creation argument returns failure, wrong type name returns failure with expected type in diagnostic message, generic constraint validation works, nested config objects extracted.

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────┐
│ Policy Compilers (receive IDocumentCompilationContext)      │
│   - Validate arguments                                      │
│   - Call processors, handle Result                          │
│   - result.ReportAll(context) on failure                    │
│   - context.AddPolicy(xml) on success                       │
└──────────────────────────┬──────────────────────────────────┘
                           │ passes context (satisfies ICompilationContext)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ ExpressionProcessor.Process(expr, ICompilationContext)      │
│   ├── LiteralExpressionSyntax → Result.Success(token.Value) │
│   ├── InvocationExpressionSyntax → CodeExtractor.Extract()  │
│   └── MemberAccessExpressionSyntax → CodeExtractor.Extract()│
│   Returns: Result<string>                                   │
└─────────────────────────────────────────────────────────────┘
        │                                       │
        ▼                                       ▼
┌───────────────────────┐             ┌─────────────────────────┐
│ CodeExtractor         │             │ ConfigurationExtractor  │
│ (ICompilationContext) │             │ (pure, no context)      │
│ - Semantic model      │             │ - Validates type name   │
│ - Symbol resolution   │             │ - Delegates to          │
│ - Method body extract │             │   InitializerProcessor  │
│ Returns: Result<str>  │             │ Returns: Result<Dict>   │
└───────────────────────┘             └───────────┬─────────────┘
                                                  │
                                                  ▼
                                      ┌─────────────────────────┐
                                      │ InitializerProcessor    │
                                      │ (pure, no context)      │
                                      │ - Object initializers   │
                                      │ - Array initializers    │
                                      │ - Collection expressions│
                                      │ Returns: Result<InitVal>│
                                      └─────────────────────────┘
```

## File Structure After Refactor

```
src/Core/
├── Results/
│   ├── Result.cs                     # Result<T> struct with combinators
│   └── ResultExtensions.cs           # Collect, ReportAll, GetValueOrDefault
├── Compiling/
│   ├── ICompilationContext.cs        # Minimal interface: Compilation property
│   ├── IDocumentCompilationContext.cs # Extends ICompilationContext
│   ├── DocumentCompilationContext.cs  # Unchanged implementation
│   ├── InitializerValue.cs           # Extracted record with factories
│   ├── InitializerProcessor.cs       # Pure syntax processing
│   ├── CodeExtractor.cs              # Semantic analysis, needs ICompilationContext
│   ├── ExpressionProcessor.cs        # Dispatcher, routes to pure/semantic
│   ├── ConfigurationExtractor.cs     # Pure, validates and extracts config
│   ├── XElementExtensions.cs         # AddAttribute, Cleared helpers
│   └── Policy/                       # 63+ updated consumers
test/Test.Core/
├── Results/
│   └── ResultTests.cs                # Result infrastructure tests
└── Compiling/
    ├── InitializerProcessorTests.cs  # Pure processor tests
    ├── CodeExtractorTests.cs         # Mock ICompilationContext tests
    ├── ExpressionProcessorTests.cs   # Dispatcher tests
    └── ConfigurationExtractorTests.cs # Config extraction tests
```

## Migration Pattern for Policy Compilers

**Before (current):**
```csharp
var username = node.ArgumentList.Arguments[0].Expression.ProcessParameter(context);
var password = node.ArgumentList.Arguments[1].Expression.ProcessParameter(context);
// Continues even if ProcessParameter failed and returned ""
context.AddPolicy(new XElement("authentication-basic", 
    new XAttribute("username", username),
    new XAttribute("password", password)));
```

**After (new pattern):**
```csharp
var usernameResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[0].Expression, context);
var passwordResult = ExpressionProcessor.Process(node.ArgumentList.Arguments[1].Expression, context);
var combined = Result.Combine(usernameResult, passwordResult);
if (!combined.IsSuccess)
{
    combined.ReportAll(context);
    return;
}
context.AddPolicy(new XElement("authentication-basic",
    new XAttribute("username", usernameResult.Value),
    new XAttribute("password", passwordResult.Value)));
```
