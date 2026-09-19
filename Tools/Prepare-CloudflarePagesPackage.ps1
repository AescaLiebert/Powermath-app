[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BuildRoot,

    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"

$resolvedBuildRoot = (Resolve-Path -LiteralPath $BuildRoot).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path (Split-Path -Parent $resolvedBuildRoot) `
        ((Split-Path -Leaf $resolvedBuildRoot) + "-PagesUpload")
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$cloudflarePublic = Join-Path $projectRoot "Cloudflare\public"
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)

if ($resolvedOutput -eq $resolvedBuildRoot) {
    throw "OutputDirectory must be different from BuildRoot."
}

if (Test-Path -LiteralPath $resolvedOutput) {
    $existing = Get-ChildItem -LiteralPath $resolvedOutput -Force
    if ($existing.Count -gt 0) {
        throw "OutputDirectory already exists and is not empty: $resolvedOutput"
    }
} else {
    New-Item -ItemType Directory -Path $resolvedOutput | Out-Null
}

$requiredFiles = @(
    "index.html",
    "manifest.webmanifest",
    "ServiceWorker.js"
)

foreach ($fileName in $requiredFiles) {
    $source = Join-Path $resolvedBuildRoot $fileName
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required WebGL shell file is missing: $source"
    }
    Copy-Item -LiteralPath $source -Destination $resolvedOutput
}

$templateData = Join-Path $resolvedBuildRoot "TemplateData"
if (-not (Test-Path -LiteralPath $templateData -PathType Container)) {
    throw "Required TemplateData folder is missing: $templateData"
}
Copy-Item -LiteralPath $templateData -Destination $resolvedOutput -Recurse

$rootFavicon = Join-Path $templateData "favicon.ico"
if (Test-Path -LiteralPath $rootFavicon -PathType Leaf) {
    Copy-Item -LiteralPath $rootFavicon -Destination (Join-Path $resolvedOutput "favicon.ico") -Force
}

$rootTouchIcon = Join-Path $templateData "icons\mathworld-180.png"
if (Test-Path -LiteralPath $rootTouchIcon -PathType Leaf) {
    Copy-Item -LiteralPath $rootTouchIcon -Destination (Join-Path $resolvedOutput "apple-touch-icon.png") -Force
    Copy-Item -LiteralPath $rootTouchIcon -Destination (Join-Path $resolvedOutput "apple-touch-icon-precomposed.png") -Force
}

foreach ($fileName in @("version.json", "_headers")) {
    $source = Join-Path $cloudflarePublic $fileName
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required Cloudflare source file is missing: $source"
    }
    Copy-Item -LiteralPath $source -Destination $resolvedOutput
}

$index = Get-Content -LiteralPath (Join-Path $resolvedOutput "index.html") -Raw
if ($index -notmatch [regex]::Escape("https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev")) {
    throw "index.html is not configured for the expected R2 public URL."
}

$version = Get-Content -LiteralPath (Join-Path $resolvedOutput "version.json") -Raw |
    ConvertFrom-Json
if (-not $version.clientVersion -or -not $version.minSupportedVersion -or
    [int]$version.schemaVersion -lt 1) {
    throw "version.json does not contain a valid release policy."
}

$migratorPath = Join-Path $projectRoot "Assets\Project\Script\PlayerData\PlayerSchemaMigrator.cs"
if (Test-Path -LiteralPath $migratorPath -PathType Leaf) {
    $migratorContent = Get-Content -LiteralPath $migratorPath -Raw
    if ($migratorContent -match 'CurrentSchemaVersion\s*=\s*(\d+);') {
        $codeSchema = [int]$matches[1]
        if ([int]$version.schemaVersion -ne $codeSchema) {
            throw "version.json schemaVersion ($($version.schemaVersion)) does not match PlayerSchemaMigrator.CurrentSchemaVersion ($codeSchema)."
        }
    }
}

Write-Host "Cloudflare Pages package is ready: $resolvedOutput"
Write-Host "Upload only this generated folder to Cloudflare Pages."
