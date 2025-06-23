#!/usr/bin/env bash

# Clean Architecture Code Generator Wrapper Script
# Usage: ./generate.sh [entity] [options]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GENERATOR_DIR="$SCRIPT_DIR/tools/CodeGenerator"

# Check if in correct directory
if [ ! -d "$GENERATOR_DIR" ]; then
    echo "❌ Error: CodeGenerator not found at $GENERATOR_DIR"
    echo "Please run this script from the project root directory."
    exit 1
fi

# If no arguments, just list entities
if [ $# -eq 0 ]; then
    echo "🔍 Listing available entities..."
    dotnet run --project "$GENERATOR_DIR"
    exit 0
fi

# Run code generator with all arguments
echo "🚀 Running Code Generator..."
dotnet run --project "$GENERATOR_DIR" -- "$@"

# Show success message
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Code generation completed successfully!"
    echo "💡 Next steps:"
    echo "   1. dotnet build"
    echo "   2. dotnet run --project src/API"
    echo "   3. Open https://localhost:7049/swagger"
else
    echo ""
    echo "❌ Code generation failed!"
fi