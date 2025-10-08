# Phase 6 Documentation Restructuring Plan

## Current Problem

- Single file: `phase-6-release.md` (1,385 lines, 34KB)
- Too many nested heading levels (up to H6)
- Difficult to maintain and lint
- Mixing overview content with detailed implementation

## Proposed Structure

```text
docs/context/phase-6-release/
├── phase-6-release.md              # Main overview (300-400 lines)
├── RESTRUCTURE-PLAN.md             # This file
└── implementation/
    ├── 01-api-documentation.md     # XML docs, DocFX (200 lines)
    ├── 02-getting-started.md       # Quick start guide (200 lines)
    ├── 03-sample-applications.md   # Sample apps (300 lines)
    ├── 04-package-configuration.md # NuGet packaging (200 lines)
    ├── 05-release-pipeline.md      # CI/CD workflows (200 lines)
    ├── 06-contribution-guide.md    # CONTRIBUTING template (200 lines)
    ├── 07-migration-guides.md      # Migration templates (200 lines)
    └── 08-performance-benchmarks.md # Benchmarks (150 lines)
```

## Benefits

1. **Better Maintainability**: Each file focuses on one topic
2. **Proper Heading Levels**: No need for H5/H6, each doc starts fresh
3. **Easier Linting**: Smaller files, clearer structure
4. **Parallel Work**: Multiple people can work on different sections
5. **Better Navigation**: Clear separation of concerns
6. **Reusability**: Sections can be referenced independently

## Main Overview File Structure

The main `phase-6-release.md` will contain:

1. Header & metadata
2. Table of contents with links to implementation docs
3. Purpose & objectives
4. High-level key activities (with links to details)
5. Deliverables summary
6. Success metrics
7. Risks & mitigations
8. Exit criteria
9. Dependencies & relationships
10. Review schedule
11. References

## Implementation File Guidelines

Each implementation doc should:

- Start with H1 title
- Use H2 for major sections
- Use H3 for subsections
- Avoid going deeper than H3
- Include "Back to main plan" link at top
- Stay under 300 lines ideally

## Migration Steps

1. ✅ Create `implementation/` directory
2. ✅ Create this plan document
3. ⏳ Extract each section into separate files
4. ⏳ Update main file with links
5. ⏳ Update table of contents
6. ⏳ Verify all links work
7. ⏳ Run linting on all files
8. ⏳ Archive old monolithic file
9. ⏳ Commit changes

## Example: Getting Started Structure

```markdown
# Phase 6: Getting Started Guide

[← Back to Phase 6 Overview](../phase-6-release.md)

## Overview

Quick start guide for Idevs framework...

## Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL 12+

## Installation Steps

### 1. Install NuGet Packages

(content here - now using H3 instead of H6!)

### 2. Configure Services

(content here)

...

## Next Steps

- [Sample Applications](03-sample-applications.md)
- [API Documentation](01-api-documentation.md)
```

## Naming Convention

- Prefix with numbers for logical ordering: `01-`, `02-`, etc.
- Use kebab-case for filenames
- Keep names descriptive but concise
- Match section titles from main doc

## Validation Checklist

- [ ] All code examples have proper language tags
- [ ] No heading deeper than H3 in implementation docs
- [ ] All internal links work
- [ ] Each doc under 300 lines
- [ ] Markdown linting passes
- [ ] No MD036 (emphasis as heading) errors
- [ ] Consistent formatting across all files
