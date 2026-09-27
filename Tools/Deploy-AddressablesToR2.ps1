[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BucketName = "math-world",

    [Parameter(Mandatory = $false)]
    [string]$AccountId = "71a44fae9d44883a0a24a0e26bc2ce4d",

    [Parameter(Mandatory = $false)]
    [string]$BuildTarget = "WebGL",

    [Parameter(Mandatory = $false)]
    [string]$ServerDataDir
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

if (-not $ServerDataDir) {
    $ServerDataDir = Join-Path $projectRoot "ServerData\$BuildTarget"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Cloudflare R2 Unity Addressables Uploader (AWS CLI)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verify AWS CLI
$awsCmd = Get-Command aws -ErrorAction SilentlyContinue
$awsExe = if ($awsCmd) { 
    $awsCmd.Source 
} elseif (Test-Path "C:\Program Files\Amazon\AWSCLIV2\aws.exe") { 
    "C:\Program Files\Amazon\AWSCLIV2\aws.exe" 
} else { 
    $null 
}

if (-not $awsExe) {
    Write-Host " [ERROR] AWS CLI ('aws') is not found in your PATH." -ForegroundColor Red
    Write-Host " Please install AWS CLI by running in an Administrator PowerShell terminal:" -ForegroundColor Yellow
    Write-Host "   winget install Amazon.AWSCLI" -ForegroundColor Green
    Write-Host " Then restart your PowerShell terminal and run this script again." -ForegroundColor Yellow
    exit 1
}

# 2. Check ServerData existence
if (-not (Test-Path -LiteralPath $ServerDataDir)) {
    Write-Host " [ERROR] ServerData directory not found: $ServerDataDir" -ForegroundColor Red
    Write-Host " Please build your Addressables in Unity first:" -ForegroundColor Yellow
    Write-Host "   Unity Menu -> PowerMath -> Addressables -> Build Addressables Content" -ForegroundColor Green
    Write-Host "   (or Addressables Groups -> New Build -> Default Build Script)" -ForegroundColor Green
    exit 1
}

$files = Get-ChildItem -LiteralPath $ServerDataDir -File
if ($files.Count -eq 0) {
    Write-Host " [ERROR] No bundle files found in: $ServerDataDir" -ForegroundColor Red
    exit 1
}

Write-Host " Found $($files.Count) file(s) in $ServerDataDir" -ForegroundColor Green

# 3. Prompt for missing parameters if not passed
if (-not $BucketName) {
    $BucketName = Read-Host " Enter your Cloudflare R2 Bucket Name (default: math-world)"
    if ([string]::IsNullOrWhiteSpace($BucketName)) { $BucketName = "math-world" }
}

if (-not $AccountId) {
    $AccountId = Read-Host " Enter your Cloudflare Account ID (default: 71a44fae9d44883a0a24a0e26bc2ce4d)"
    if ([string]::IsNullOrWhiteSpace($AccountId)) { $AccountId = "71a44fae9d44883a0a24a0e26bc2ce4d" }
}

$endpointUrl = "https://$AccountId.r2.cloudflarestorage.com"
$r2Dest = "s3://$BucketName/Addressables/$BuildTarget"

Write-Host ""
Write-Host " Source:      $ServerDataDir" -ForegroundColor Cyan
Write-Host " Destination: $r2Dest" -ForegroundColor Cyan
Write-Host " Endpoint:    $endpointUrl" -ForegroundColor Cyan
Write-Host ""

Write-Host " Starting sync via AWS CLI..." -ForegroundColor Yellow
$cmdString = "& `"$awsExe`" s3 sync `"$ServerDataDir`" `"$r2Dest`" --endpoint-url `"$endpointUrl`""
Write-Host " Executing: $cmdString" -ForegroundColor DarkGray

& "$awsExe" s3 sync "$ServerDataDir" "$r2Dest" --endpoint-url "$endpointUrl"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host " [SUCCESS] Addressables successfully synced to Cloudflare R2!" -ForegroundColor Green
    Write-Host ""
    
    $testScript = Join-Path $PSScriptRoot "Test-CloudflareR2Deployment.ps1"
    if (Test-Path -LiteralPath $testScript) {
        $runVerify = Read-Host " Would you like to run Test-CloudflareR2Deployment.ps1 now? (Y/n)"
        if ($runVerify -eq "" -or $runVerify -match "^[Yy]") {
            & $testScript
        }
    }
} else {
    Write-Host ""
    Write-Host " [ERROR] AWS CLI sync failed with exit code $LASTEXITCODE." -ForegroundColor Red
    Write-Host " Please check your AWS credentials (aws configure) or Account ID / Bucket Name." -ForegroundColor Yellow
    exit $LASTEXITCODE
}
