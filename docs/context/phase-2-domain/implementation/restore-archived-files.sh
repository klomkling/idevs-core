#!/bin/bash

set -e

IMPL_DIR="/Users/sarawut/GitHub/Packages/nuget/warp-idevs-core/docs/context/phase-2-domain/implementation"
ARCHIVE_DIR="$IMPL_DIR/archive"

echo "🔄 Restoring archived implementation files..."
echo ""

# List of files to restore
declare -a FILES=(
    "02-value-objects.md"
    "03-result-patterns.md"
    "04-cqrs-contracts.md"
    "05-aggregates-entities.md"
    "06-tenant-user-context.md"
    "07-repository-uow.md"
    "08-domain-events.md"
    "09-specifications.md"
)

# Restore each file
for file in "${FILES[@]}"; do
    if [ -f "$ARCHIVE_DIR/$file" ]; then
        echo "✅ Restoring $file from archive..."
        git mv "$ARCHIVE_DIR/$file" "$IMPL_DIR/$file"
        
        # Get line count and code block count
        lines=$(wc -l < "$IMPL_DIR/$file")
        blocks=$(grep -c '```' "$IMPL_DIR/$file" || echo 0)
        echo "   📊 $lines lines, $blocks code blocks"
    else
        echo "⚠️  $file not found in archive"
    fi
done

echo ""
echo "🗑️  Removing split files..."

# Remove split files that are now redundant
declare -a SPLIT_FILES=(
    "02-value-objects-base.md"
    "02-value-objects-examples.md"
    "03-result-pattern-core.md"
    "03-result-pattern-extensions.md"
    "04-cqrs-contracts-basic.md"
    "04-cqrs-contracts-advanced.md"
    "05-aggregates-base.md"
    "05-aggregates-examples.md"
    "06-tenant-context.md"
    "06-user-context.md"
    "07-repository-pattern.md"
    "07-unit-of-work.md"
    "08-domain-events-contracts.md"
    "08-domain-events-impl.md"
    "09-specifications-pattern.md"
    "09-specifications-examples.md"
)

for file in "${SPLIT_FILES[@]}"; do
    if [ -f "$IMPL_DIR/$file" ]; then
        echo "🗑️  Removing $file"
        git rm "$IMPL_DIR/$file"
    fi
done

echo ""
echo "✅ All files restored!"
echo ""
echo "📁 Current implementation files:"
ls -1 "$IMPL_DIR"/*.md | grep -v "README\|SPLIT\|CHECKLIST" | xargs -I{} basename {}
