#!/usr/bin/env bash

set -e

# ==============================
# Argument validation
# ==============================
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <source_dir> <target_dir>"
    exit 1
fi

SOURCE_DIR="$1"
TARGET_DIR="$2"

# ==============================
# Check if protoc is installed
# ==============================
if ! command -v protoc >/dev/null 2>&1; then
    echo "Error: protoc is not installed."
    exit 1
fi

# ==============================
# Check source directory exists
# ==============================
if [ ! -d "$SOURCE_DIR" ]; then
    echo "Error: source directory does not exist: $SOURCE_DIR"
    exit 1
fi

# ==============================
# Create target directory
# ==============================
mkdir -p "$TARGET_DIR"

# ==============================
# Scan and compile .proto files
# ==============================
echo "Scanning for .proto files in $SOURCE_DIR..."

PROTO_FILES=$(find "$SOURCE_DIR" -type f -name "*.proto")

if [ -z "$PROTO_FILES" ]; then
    echo "No .proto files found."
    exit 0
fi

echo "Compiling..."

for proto in $PROTO_FILES; do
    echo "→ $proto"
    
    protoc \
        --proto_path="$SOURCE_DIR" \
        --python_out="$TARGET_DIR" \
        "$proto"
done

echo "Compilation completed."