param(
  [string]$Configuration = "Release",
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Join-Path $repoRoot "bin\Workshop\RelicToastWorkshop"
$contentRoot = Join-Path $workspaceRoot "content"
$modContentRoot = Join-Path $contentRoot "RelicToast"
$modIdPath = Join-Path $repoRoot "workshop\mod_id.txt"

if (-not $SkipBuild) {
  dotnet build (Join-Path $repoRoot "RelicToast.csproj") -c $Configuration
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}

$dllPath = Join-Path $repoRoot "bin\$Configuration\RelicToast.dll"
$manifestPath = Join-Path $repoRoot "bin\$Configuration\RelicToast.json"

if (-not (Test-Path -LiteralPath $dllPath)) {
  throw "Could not find built DLL at: $dllPath"
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
  throw "Could not find built manifest at: $manifestPath"
}

if (Test-Path -LiteralPath $workspaceRoot) {
  $resolvedRepo = (Resolve-Path -LiteralPath $repoRoot).Path
  $resolvedWorkspace = (Resolve-Path -LiteralPath $workspaceRoot).Path
  if (-not $resolvedWorkspace.StartsWith($resolvedRepo, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a directory outside the repository: $resolvedWorkspace"
  }

  Remove-Item -LiteralPath $workspaceRoot -Recurse -Force
}

New-Item -ItemType Directory -Force $modContentRoot | Out-Null

Copy-Item -LiteralPath (Join-Path $repoRoot "workshop\workshop.json") -Destination (Join-Path $workspaceRoot "workshop.json")
Copy-Item -LiteralPath (Join-Path $repoRoot "workshop\image.png") -Destination (Join-Path $workspaceRoot "image.png")
Copy-Item -LiteralPath $dllPath -Destination (Join-Path $modContentRoot "RelicToast.dll")
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $modContentRoot "RelicToast.json")

if (Test-Path -LiteralPath $modIdPath) {
  Copy-Item -LiteralPath $modIdPath -Destination (Join-Path $workspaceRoot "mod_id.txt")
}

Write-Host "Workshop workspace created at: $workspaceRoot"
