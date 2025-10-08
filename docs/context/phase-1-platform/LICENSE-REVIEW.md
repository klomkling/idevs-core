# Package License Review - Phase 1

**Document Owner**: Legal & Platform Team  
**Last Updated**: 2025-10-04  
**Status**: Approved for Commercial Use

---

## Purpose

This document reviews all NuGet packages used in Phase 1 to ensure they have permissive licenses suitable for commercial use. All packages have been verified to use **MIT**, **Apache 2.0**, or **BSD** licenses.

---

## ✅ Approved Packages

### Core Framework (Microsoft - MIT License)

| Package | Version | License | Status |
|---------|---------|---------|--------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.Extensions.Configuration.Abstractions` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.EntityFrameworkCore` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.AspNetCore.Mvc.Core` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.AspNetCore.Mvc.Abstractions` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.SourceLink.GitHub` | 8.0.0+ | MIT | ✅ Safe |
| `Microsoft.NET.Test.Sdk` | 17.8.0+ | MIT | ✅ Safe |

**Verification**: All Microsoft packages are MIT licensed  
**Source**: <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT>

---

### Database (PostgreSQL - PostgreSQL License / MIT-like)

| Package | Version | License | Status |
|---------|---------|---------|--------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.0+ | PostgreSQL License | ✅ Safe |

**Verification**: PostgreSQL License is permissive and similar to MIT/BSD  
**Source**: <https://github.com/npgsql/npgsql>  
**Note**: Npgsql is explicitly safe for commercial use

---

### Observability (Apache 2.0 License)

| Package | Version | License | Status |
|---------|---------|---------|--------|
| `Serilog` | 3.1.1+ | Apache 2.0 | ✅ Safe |
| `Serilog.AspNetCore` | 8.0.0+ | Apache 2.0 | ✅ Safe |
| `Serilog.Sinks.Console` | 5.0.0+ | Apache 2.0 | ✅ Safe |
| `Serilog.Sinks.File` | 5.0.0+ | Apache 2.0 | ✅ Safe |
| `OpenTelemetry` | 1.7.0+ | Apache 2.0 | ✅ Safe |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.7.0+ | Apache 2.0 | ✅ Safe |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.0.0-beta+ | Apache 2.0 | ✅ Safe |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | 1.7.0+ | Apache 2.0 | ✅ Safe |

**Verification**: CNCF projects use Apache 2.0  
**Source**: <https://github.com/open-telemetry/opentelemetry-dotnet>

---

### Testing (Apache 2.0 / MIT License)

| Package | Version | License | Status |
|---------|---------|---------|--------|
| `xunit` | 2.6.2+ | Apache 2.0 | ✅ Safe |
| `xunit.runner.visualstudio` | 2.5.4+ | Apache 2.0 | ✅ Safe |
| `NSubstitute` | 5.1.0+ | BSD-3-Clause | ✅ Safe |
| `Shouldly` | 4.2.1+ | BSD-2-Clause | ✅ Safe |
| `NetArchTest.Rules` | 1.3.2+ | MIT | ✅ Safe |
| `coverlet.collector` | 6.0.0+ | MIT | ✅ Safe |

**Verification**:

- xUnit: <https://github.com/xunit/xunit/blob/main/LICENSE>
- NSubstitute: <https://github.com/nsubstitute/NSubstitute/blob/main/LICENSE.txt>
- Shouldly: <https://github.com/shouldly/shouldly/blob/master/LICENSE.txt>
- NetArchTest: <https://github.com/BenMorris/NetArchTest/blob/master/LICENSE>

---

## ⚠️ Packages Excluded from Core Framework

### App.Metrics (Not Used)

**Package**: `App.Metrics.AspNetCore.Mvc`  
**License**: Apache 2.0  
**Issue**: Project is archived and unmaintained  
**Alternative**: Built-in `System.Diagnostics.Metrics` (MIT, part of .NET runtime)  
**Benefit**: No external dependency, better long-term support

### Scrutor (Not Used)

**Package**: `Scrutor`  
**License**: MIT  
**Issue**: Provides assembly scanning feature (conflicts with "Explicit Over Magic" principle)  
**Alternative**: Manual factory registration using built-in MEDI  
**Benefit**: Zero external dependencies, explicit registration, better debugging

### Autofac (Not Required)

**Package**: `Autofac`, `Autofac.Extensions.DependencyInjection`  
**License**: MIT  
**Issue**: Requires replacing entire DI container, has assembly scanning  
**Alternative**: Built-in `Microsoft.Extensions.DependencyInjection`  
**Optional**: `Idevs.DependencyInjection.Autofac` adapter package for consumers who want it  
**Benefit**: Lower adoption barrier, standard .NET patterns

---

## Built-in .NET Libraries (MIT License)

These are included in .NET 8.0 runtime and require no additional packages:

| Library | Purpose | License |
|---------|---------|---------|
| `System.Diagnostics.Metrics` | Metrics collection | MIT |
| `System.Diagnostics.DiagnosticSource` | Activity tracing | MIT |
| `System.Diagnostics.Activity` | Distributed tracing | MIT |
| `System.Collections.Concurrent` | Thread-safe collections | MIT |

---

## License Compatibility Matrix

### Compatible with MIT

All packages in this project are compatible with MIT license (the framework's chosen license):

```text
Idevs Framework (MIT)
├── Can use: MIT ✅
├── Can use: Apache 2.0 ✅
├── Can use: BSD (2-clause, 3-clause) ✅
└── Can use: PostgreSQL License ✅
```text

### Not Compatible (Avoided)

These licenses are NOT used in this project:

- ❌ **GPL** (copyleft - requires derivative works to be GPL)
- ❌ **AGPL** (copyleft + network use clause)
- ❌ **SSPL** (restrictive for cloud providers)
- ❌ **Commons Clause** (not open source)
- ❌ **Proprietary** (commercial licenses)

---

## Dependency Audit Process

### Before Adding Any New Package

1. **Check License on NuGet.org**

   - Visit package page
   - Look for "License" section
   - Verify it's MIT, Apache 2.0, BSD, or similar

1. **Review GitHub Repository**

   ```bash
   # Check for LICENSE file
   curl https://raw.githubusercontent.com/org/repo/main/LICENSE
   ```

1. **Use Automated Tools**

   ```bash
   # Install license checker
   dotnet tool install --global dotnet-project-licenses
   
   # Scan project
   dotnet-project-licenses --input /path/to/project.csproj
   ```

1. **Verify with Legal Team**

   - If license is unclear
   - If license is not MIT/Apache/BSD
   - If commercial use clause exists

---

## Verification Commands

### List All Package Licenses

```bash
# Using dotnet-project-licenses tool
dotnet tool install --global dotnet-project-licenses
dotnet-project-licenses --input src/Idevs/Idevs.csproj -f json -o licenses.json

# Manual check via NuGet
dotnet list package --include-transitive
```text

### Check for Known Vulnerabilities

```bash
# Security audit
dotnet list package --vulnerable --include-transitive

# Update packages to address vulnerabilities
dotnet outdated
```text

---

## License Compliance Guidelines

### DO ✅

- Use packages with MIT, Apache 2.0, BSD licenses
- Include LICENSE file in root directory
- Include NOTICE file if using Apache 2.0 dependencies
- Document all dependencies in `Directory.Packages.props`
- Run license audit before each release

### DON'T ❌

- Use GPL/AGPL licensed packages
- Use packages without clear license information
- Add dependencies without license review
- Copy code without attribution
- Use packages with "non-commercial" clauses

---

## Attribution Requirements

### Apache 2.0 Packages

When using Apache 2.0 licensed packages, include this notice:

```text
This product includes software developed by:
- Serilog (Apache 2.0) - https://serilog.net/
- OpenTelemetry (Apache 2.0) - https://opentelemetry.io/
- xUnit (Apache 2.0) - https://xunit.net/
```text

Place in `NOTICE.txt` file at repository root.

### MIT/BSD Packages

MIT and BSD licenses require copyright notice in distributions. This is automatically satisfied by including the original LICENSE file from each package (NuGet does this automatically).

---

## Annual License Review

**Schedule**: Review all dependencies annually (every January)

**Checklist**:

- [ ] Re-verify licenses of all packages
- [ ] Check for license changes in updated packages
- [ ] Review new dependencies added during the year
- [ ] Update LICENSE-REVIEW.md with any changes
- [ ] Generate updated license report for compliance

**Command**:

```bash
dotnet-project-licenses --input idevs-core.sln --export-license-texts
```text

---

## Contact

For license questions or concerns:

**Internal**: Platform Team Lead  
**Legal**: <legal@idevs.work>  
**External**: OSS community (GitHub issues)

---

## Approved By

- [x] Platform Team Lead
- [x] Legal Review (if applicable)
- [x] Security Team

**Approval Date**: 2025-10-04

---

## Appendix: License Texts

### MIT License (Framework License)

```text
MIT License

Copyright (c) 2025 idevs.work

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```text

### Key Takeaway

✅ **All packages used in Phase 1 are safe for commercial use**  
✅ **No GPL/AGPL/copyleft licenses**  
✅ **Compatible with MIT license chosen for Idevs framework**
