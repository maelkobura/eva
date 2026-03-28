<#
.SYNOPSIS
    Scan a directory for .proto files and compile them to Python using protoc.

.PARAMETER SourceDir
    The source directory containing .proto files.

.PARAMETER TargetDir
    The directory where the generated Python files will be placed.

.EXAMPLE
    .\Compile-Protos.ps1 -SourceDir ".\protos" -TargetDir ".\generated"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$SourceDir,

    [Parameter(Mandatory=$true)]
    [string]$TargetDir
)

# Check protoc
if (-not (Get-Command protoc -ErrorAction SilentlyContinue)) {
    Write-Error "Error: protoc is not installed or not in PATH."
    exit 1
}

# Resolve full paths
$SourceDirFull = (Resolve-Path $SourceDir).Path
if (-not $SourceDirFull.EndsWith('\')) { $SourceDirFull += '\' }

$TargetDirFull = (Resolve-Path $TargetDir -ErrorAction SilentlyContinue)
if (-not $TargetDirFull) {
    New-Item -ItemType Directory -Path $TargetDir | Out-Null
    $TargetDirFull = (Resolve-Path $TargetDir).Path
}

# Find all .proto files recursively
Write-Host "Scanning for .proto files in $SourceDirFull..."
$ProtoFiles = Get-ChildItem -Path $SourceDirFull -Recurse -Filter "*.proto"

if ($ProtoFiles.Count -eq 0) {
    Write-Host "No .proto files found."
    exit 0
}

Write-Host "Compiling..."

foreach ($proto in $ProtoFiles) {
    # Compute relative path manually
    $relativePath = $proto.FullName.Substring($SourceDirFull.Length)
    $relativePath = $relativePath -replace '\\','/'  # protoc prefers /

    Write-Host "→ $relativePath"

    # Compile with protoc using the relative path
    protoc --proto_path="$SourceDirFull" --python_out="$TargetDirFull" "$relativePath"
}

Write-Host "Compilation completed."