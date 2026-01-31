# Source Generator Plan: Replace InitializerValue with Generated Compiled Configs

## Overview

Replace the dynamic `InitializerValue` approach with compile-time generated strongly-typed config classes using a Roslyn source generator.

## Key Design Decisions

- **Discriminated Unions**: For polymorphic types like `IAuthenticationConfig`
- **Composition**: Nested configs use composition, not recursive generation
- **Compiler Handles XML/Validation**: Generator produces data classes; compilers handle XML generation and validation
- **Separate Generator Project**: New internal `src/Generators` project (not in existing Analyzers)
- **Non-Incremental First**: Normal ISourceGenerator, optimize to incremental later if needed
- **ExpressionValue<T>**: Wraps values that can be constants or expressions

## New Types

### ExpressionValue<T> (src/Core/Compiling/ExpressionValue.cs)

```csharp
public readonly struct ExpressionValue<T>
{
    public bool IsExpression { get; }
    public T? ConstantValue { get; }
    public string? Expression { get; }
    
    public static ExpressionValue<T> FromConstant(T value);
    public static ExpressionValue<T> FromExpression(string expression);
    
    public string ToXmlValue(); // Returns expression or constant.ToString()
}
```

### [GenerateCompiledConfig] Attribute (src/Authoring/Attributes/)

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GenerateCompiledConfigAttribute : Attribute { }
```

### [XmlName] Attribute (src/Authoring/Attributes/)

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class XmlNameAttribute : Attribute
{
    public string Name { get; }
    public XmlNameAttribute(string name) => Name = name;
}
```

## Generator Output Examples

### Input Config

```csharp
[GenerateCompiledConfig]
public class SetHeaderConfig
{
    public required string Name { get; init; }
    public string? Value { get; init; }
    public ExistsAction? ExistsAction { get; init; }
}
```

### Generated Compiled Config

```csharp
public partial class SetHeaderCompiledConfig
{
    public required ExpressionValue<string> Name { get; init; }
    public ExpressionValue<string>? Value { get; init; }
    public ExpressionValue<ExistsAction>? ExistsAction { get; init; }
}
```

### Discriminated Union Example

For interface types:

```csharp
public interface IAuthenticationConfig { }

[GenerateCompiledConfig]
public class BasicAuthConfig : IAuthenticationConfig { ... }

[GenerateCompiledConfig]  
public class CertificateAuthConfig : IAuthenticationConfig { ... }
```

Generator produces union:

```csharp
public abstract class AuthenticationConfigUnion
{
    public sealed class Basic : AuthenticationConfigUnion
    {
        public BasicCompiledConfig Config { get; }
    }
    
    public sealed class Certificate : AuthenticationConfigUnion
    {
        public CertificateCompiledConfig Config { get; }
    }
    
    public T Match<T>(Func<BasicCompiledConfig, T> onBasic, Func<CertificateCompiledConfig, T> onCertificate);
}
```

## Implementation Steps

### Phase A: Infrastructure (Steps 1-3)

1. **Create `src/Generators` project**
   - Target: netstandard2.0
   - Dependencies: Microsoft.CodeAnalysis.CSharp
   - Add as analyzer reference to Core.csproj
   - Add to solution

2. **Create `ExpressionValue<T>` struct**
   - Location: src/Core/Compiling/ExpressionValue.cs
   - Properties: IsExpression, ConstantValue, Expression
   - Methods: FromConstant, FromExpression, ToXmlValue

3. **Create attributes**
   - `[GenerateCompiledConfig]` in src/Authoring/Attributes/
   - `[XmlName]` in src/Authoring/Attributes/

### Phase B: Generator Core (Steps 4-9)

4. **Create `PolicyConfigGenerator` class**
   - Implements ISourceGenerator
   - Entry point for generation

5. **Create `ConfigAnalyzer`**
   - Finds classes with [GenerateCompiledConfig]
   - Extracts property info (name, type, nullability)
   - Identifies interface types for union generation

6. **Create `CompiledConfigEmitter`**
   - Generates `{ClassName}CompiledConfig` partial class
   - Wraps each property in ExpressionValue<T>
   - Handles required/optional distinction

7. **Create `UnionTypeEmitter`**
   - Finds all implementers of interface types
   - Generates discriminated union base class
   - Generates Match method

8. **Create `MethodArgsEmitter`**
   - For policies with direct method parameters (no config class)
   - Generates `{PolicyName}Args` record

9. **Add generator tests**
   - Create test/Test.Generators project
   - Verify generated code compiles
   - Verify correct property wrapping

### Phase C: Migration (Steps 10-16)

10. **Add [GenerateCompiledConfig] to existing configs**
    - Start with simple configs (SetHeaderConfig, SetStatusConfig)
    - Verify generation works

11. **Update ExpressionProcessor**
    - Add overload returning ExpressionValue<T>
    - Keep existing Result<string> for backward compat

12. **Migrate simple compilers**
    - SetHeaderCompiler, SetStatusCompiler
    - Use generated compiled configs
    - Compiler handles XML element creation

13. **Migrate authentication compilers**
    - Use discriminated union for IAuthenticationConfig
    - Match on union type to generate correct XML

14. **Migrate remaining compilers**
    - Work through all 63+ compilers
    - Group by config complexity

15. **Migrate direct-parameter policies**
    - Use generated Args records
    - Update compiler signatures

16. **Remove backward compat from ExpressionProcessor**
    - All compilers now use ExpressionValue<T>

### Phase D: Cleanup (Steps 17-18)

17. **Delete obsolete code**
    - InitializerValue.cs
    - InitializerProcessor.cs
    - ConfigurationExtractor.cs
    - Old ExpressionProcessor overloads

18. **Final verification**
    - All 569+ tests pass
    - No InitializerValue references remain
    - Generator tests comprehensive

## Files to Create

| File | Purpose |
|------|---------|
| src/Generators/Generators.csproj | Generator project |
| src/Generators/PolicyConfigGenerator.cs | Entry point |
| src/Generators/ConfigAnalyzer.cs | Config analysis |
| src/Generators/CompiledConfigEmitter.cs | Config generation |
| src/Generators/UnionTypeEmitter.cs | Union generation |
| src/Generators/MethodArgsEmitter.cs | Args generation |
| src/Core/Compiling/ExpressionValue.cs | Value wrapper |
| src/Authoring/Attributes/GenerateCompiledConfigAttribute.cs | Marker attribute |
| src/Authoring/Attributes/XmlNameAttribute.cs | XML name override |
| test/Test.Generators/Test.Generators.csproj | Generator tests |

## Files to Delete (After Migration)

| File | Reason |
|------|--------|
| src/Core/Compiling/InitializerValue.cs | Replaced by generated configs |
| src/Core/Compiling/InitializerProcessor.cs | No longer needed |
| src/Core/Compiling/ConfigurationExtractor.cs | No longer needed |

## Success Criteria

- [ ] Generator produces correct compiled config classes
- [ ] ExpressionValue<T> properly handles constants and expressions
- [ ] Discriminated unions work for polymorphic configs
- [ ] All 63+ compilers migrated
- [ ] All existing tests pass
- [ ] No InitializerValue references remain
- [ ] Generator has comprehensive tests
