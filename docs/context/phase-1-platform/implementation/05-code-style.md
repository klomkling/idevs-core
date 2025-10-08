# Phase 1: Code Style & Analyzers

[← Back to Phase 1 Overview](../phase-1-platform.md)

## Overview

Define consistent code style and static analysis rules using EditorConfig.

## Create .editorconfig

**File:** `.editorconfig` (repository root)

See [phase-1-platform-ORIGINAL.md](../phase-1-platform-ORIGINAL.md#5-code-style--analyzers) lines 321-457 for complete configuration.

### Key Sections

```ini
root = true

# All files
[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

# C# files
[*.cs]
# Organize usings
dotnet_sort_system_directives_first = true

# Naming: Private fields _camelCase
dotnet_naming_style.camel_case_underscore_style.required_prefix = _

# Naming: Interfaces IPascalCase
dotnet_naming_style.i_prefix_pascal_case_style.required_prefix = I

# Pattern matching preferences
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion

# Code block preferences
csharp_prefer_braces = true:warning
```

## Enforcement

```bash
# Check code style
dotnet format --verify-no-changes

# Auto-fix style violations
dotnet format
```

## Custom Analyzers (Future)

- StyleCop.Analyzers (stricter rules)
- Roslynator (refactorings)
- SonarAnalyzer.CSharp (security)

## Next Steps

- **[Versioning](06-versioning.md)** - GitVersion strategy
- **[CI/CD Workflow](07-cicd-workflow.md)** - Automated pipeline

---

[← Back to Phase 1 Overview](../phase-1-platform.md) | [Next: Versioning →](06-versioning.md)
