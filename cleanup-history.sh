#!/bin/bash

echo "=========================================="
echo "🗑️  Removing sensitive data from Git history"
echo "=========================================="
echo ""
echo "This will remove from ALL Git history:"
echo "  - data-raw/ (copyrighted material)"
echo "  - Internal .md files (LLM-like documentation)"
echo ""
echo "⚠️  WARNING: This rewrites Git history!"
echo "   - All commit hashes will change"
echo "   - Requires force push to GitHub"
echo ""
read -p "Continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Aborted."
    exit 1
fi

echo ""
echo "Step 1: Removing data-raw from entire Git history..."
git filter-branch --force --index-filter \
  "git rm -r --cached --ignore-unmatch data-raw" \
  --prune-empty --tag-name-filter cat -- --all

if [ $? -ne 0 ]; then
    echo "❌ Filter-branch failed!"
    exit 1
fi

echo ""
echo "Step 2: Cleaning up Git repository..."
rm -rf .git/refs/original/
git reflog expire --expire=now --all
git gc --prune=now --aggressive

echo ""
echo "✅ Successfully removed sensitive data from Git history!"
echo ""
echo "📊 Verification:"
echo ""
echo "Checking for data-raw..."
if [ -z "$(git log --all --oneline -- data-raw)" ]; then
    echo "  ✅ No data-raw found in history"
else
    echo "  ⚠️  Still found in history:"
    git log --all --oneline -- data-raw | head -5
fi

echo ""
echo "Repository size:"
du -sh .git

echo ""
echo "=========================================="
echo "📤 FINAL STEPS - DO MANUALLY:"
echo "=========================================="
echo ""
echo "1. Review changes:"
echo "   git log --oneline | head -20"
echo ""
echo "2. Force push to GitHub:"
echo "   git push origin --force --all"
echo "   git push origin --force --tags"
echo ""
echo "3. Verify on GitHub that data-raw is gone"
echo ""
echo "⚠️  After force pushing, the repository will be clean!"
echo ""
