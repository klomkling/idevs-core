#!/usr/bin/env bash
set -e

# Automated File Splitting Script for Phase 2 Domain
# This script splits oversized implementation files based on documented split points

ROOT="/Users/sarawut/GitHub/Packages/nuget/warp-idevs-core/docs/context/phase-2-domain"
IMPL="$ROOT/implementation"
ARCHIVE="$IMPL/archive"

cd "$ROOT"

echo "🔪 Phase 2 Domain - Automated File Splitting"
echo "=============================================="
echo

# Color codes for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to add navigation header
add_nav_header() {
    local file=$1
    local part_num=$2
    local base_name=$3
    local part1_file=$4
    local part2_file=$5
    
    cat > "$file" << EOF
# ${base_name} - Part ${part_num}

> **Navigation:** [Index](README.md) • [Part 1](${part1_file}) • [Part 2](${part2_file})

EOF
}

# Function to add navigation footer
add_nav_footer() {
    local file=$1
    local part1_file=$2
    local part2_file=$3
    local is_part1=$4
    
    if [ "$is_part1" = "true" ]; then
        cat >> "$file" << EOF

---

**[← Back to Phase 2](../phase-2-domain.md)** | **[Next: Part 2 →](${part2_file})**
EOF
    else
        cat >> "$file" << EOF

---

**[← Part 1](${part1_file})** | **[Back to Phase 2](../phase-2-domain.md)**
EOF
    fi
}

# Function to split a file
split_file() {
    local source=$1
    local split_line=$2
    local part1_file=$3
    local part2_file=$4
    local base_name=$5
    
    if [ ! -f "$IMPL/$source" ]; then
        echo -e "${YELLOW}⚠️  Source file not found: $source${NC}"
        return 1
    fi
    
    echo -e "${BLUE}📄 Splitting: $source${NC}"
    echo "   Split point: line $split_line"
    echo "   → $part1_file"
    echo "   → $part2_file"
    
    # Extract part 1 (lines 1 to split_line)
    head -n "$split_line" "$IMPL/$source" > "$IMPL/$part1_file.tmp"
    
    # Extract part 2 (lines split_line+1 to end)
    tail -n +"$((split_line + 1))" "$IMPL/$source" > "$IMPL/$part2_file.tmp"
    
    # Add navigation to part 1
    add_nav_header "$IMPL/$part1_file" "1" "$base_name" "$part1_file" "$part2_file"
    # Remove original title and add content
    tail -n +2 "$IMPL/$part1_file.tmp" >> "$IMPL/$part1_file"
    add_nav_footer "$IMPL/$part1_file" "$part1_file" "$part2_file" "true"
    
    # Add navigation to part 2
    add_nav_header "$IMPL/$part2_file" "2" "$base_name" "$part1_file" "$part2_file"
    # Add content (keeping the heading from part 2)
    cat "$IMPL/$part2_file.tmp" >> "$IMPL/$part2_file"
    add_nav_footer "$IMPL/$part2_file" "$part1_file" "$part2_file" "false"
    
    # Clean up temp files
    rm "$IMPL/$part1_file.tmp" "$IMPL/$part2_file.tmp"
    
    # Show line counts
    local lines1=$(wc -l < "$IMPL/$part1_file")
    local lines2=$(wc -l < "$IMPL/$part2_file")
    echo "   Part 1: $lines1 lines"
    echo "   Part 2: $lines2 lines"
    
    # Verify both under 450
    if [ "$lines1" -gt 450 ] || [ "$lines2" -gt 450 ]; then
        echo -e "${YELLOW}   ⚠️  WARNING: One or both parts exceed 450 lines!${NC}"
    else
        echo -e "${GREEN}   ✅ Both parts under 450 lines${NC}"
    fi
    
    # Archive original
    git mv "$IMPL/$source" "$ARCHIVE/$source" 2>/dev/null || mv "$IMPL/$source" "$ARCHIVE/$source"
    
    # Add new files to git
    git add "$IMPL/$part1_file" "$IMPL/$part2_file" 2>/dev/null || true
    
    echo
}

echo "Starting automated file splitting..."
echo

# File 02: Value Objects (742 lines) → split at line 269
split_file \
    "02-value-objects.md" \
    269 \
    "02-value-objects-base.md" \
    "02-value-objects-examples.md" \
    "02: Value Objects"

# File 03: Result Patterns (630 lines) → split at line 315
split_file \
    "03-result-patterns.md" \
    315 \
    "03-result-pattern-core.md" \
    "03-result-pattern-extensions.md" \
    "03: Result Patterns"

# File 04: CQRS Contracts (626 lines) → split at line 310
split_file \
    "04-cqrs-contracts.md" \
    310 \
    "04-cqrs-contracts-basic.md" \
    "04-cqrs-contracts-advanced.md" \
    "04: CQRS Contracts"

# File 06: Tenant & User Context (738 lines) → split at line 370
split_file \
    "06-tenant-user-context.md" \
    370 \
    "06-tenant-context.md" \
    "06-user-context.md" \
    "06: Tenant & User Context"

# File 07: Repository & UoW (672 lines) → split at line 335
split_file \
    "07-repository-uow.md" \
    335 \
    "07-repository-pattern.md" \
    "07-unit-of-work.md" \
    "07: Repository & Unit of Work"

# File 08: Domain Events (690 lines) → split at line 345
split_file \
    "08-domain-events.md" \
    345 \
    "08-domain-events-contracts.md" \
    "08-domain-events-impl.md" \
    "08: Domain Events"

# File 09: Specifications (800 lines) → split at line 400
split_file \
    "09-specifications.md" \
    400 \
    "09-specifications-pattern.md" \
    "09-specifications-examples.md" \
    "09: Specifications"

echo "=============================================="
echo -e "${GREEN}✅ File splitting complete!${NC}"
echo
echo "Summary:"
find "$IMPL" -name "*.md" -not -path "*/archive/*" -not -name "SPLIT*.md" -not -name "README.md" | wc -l | xargs echo "  Total implementation files:"
echo
echo "Verification:"
echo "  Checking for files over 450 lines..."
OVER_LIMIT=$(find "$IMPL" -name "*.md" -not -path "*/archive/*" -exec bash -c 'c=$(wc -l < "$1"); if [ "$c" -gt 450 ]; then echo "$c $1"; fi' _ {} \;)
if [ -z "$OVER_LIMIT" ]; then
    echo -e "  ${GREEN}✅ All files under 450 lines!${NC}"
else
    echo -e "  ${YELLOW}⚠️  Files exceeding limit:${NC}"
    echo "$OVER_LIMIT"
fi
echo
echo "Next steps:"
echo "  1. Review the split files in implementation/"
echo "  2. Run: git status"
echo "  3. Run: git commit -m \"refactor(docs): auto-split remaining implementation files\""
echo "  4. Continue with navigation and cross-reference updates"
