[CmdletBinding()]
param(
    [string]$R2Url = "https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev",
    [string]$LocalR2Root
)

$ErrorActionPreference = "Stop"

$cleanR2Url = $R2Url.TrimEnd('/')
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

if (-not $LocalR2Root) {
    $parentDir = Split-Path -Parent $projectRoot
    $candidateR2 = Join-Path $parentDir "R2"
    if (Test-Path -LiteralPath $candidateR2 -PathType Container) {
        $LocalR2Root = $candidateR2
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Cloudflare R2 WebGL Asset & Video Deployment Verifier" -ForegroundColor Cyan
Write-Host " Target R2 Bucket URL: $cleanR2Url" -ForegroundColor Cyan
if ($LocalR2Root -and (Test-Path -LiteralPath $LocalR2Root)) {
    Write-Host " Local Source R2 Dir:  $LocalR2Root" -ForegroundColor Cyan
}
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

$filesToCheck = @(
    @{
        Category = "Build"
        Path = "Build/MathWorld-1.2.loader.js"
        ExpectedType = "application/javascript"
    },
    @{
        Category = "Build"
        Path = "Build/MathWorld-1.2.framework.js"
        ExpectedType = "application/javascript"
    },
    @{
        Category = "Build"
        Path = "Build/MathWorld-1.2.data"
        ExpectedType = "application/octet-stream"
    },
    @{
        Category = "Build"
        Path = "Build/MathWorld-1.2.wasm"
        ExpectedType = "application/wasm"
    },
    @{
        Category = "Addressables"
        Path = "Addressables/WebGL/catalog_1.2.0.0.bin"
        ExpectedType = "application/octet-stream"
    },
    @{
        Category = "Addressables"
        Path = "Addressables/WebGL/catalog_1.2.0.0.hash"
        ExpectedType = "text/plain"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/V2_LogInLive2D.mp4"
        ExpectedType = "video/mp4"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/V2_LogInLive2D_Reverse.mp4"
        ExpectedType = "video/mp4"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/V_Hub_Stand_Ricko.mp4"
        ExpectedType = "video/mp4"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/V_Hub_Stand_Stellar.mp4"
        ExpectedType = "video/mp4"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/V_SelectCharacterScene.mp4"
        ExpectedType = "video/mp4"
    },
    @{
        Category = "StreamingAssets (Video)"
        Path = "StreamingAssets/Videos/WelcomeToWorldVideo.mp4"
        ExpectedType = "video/mp4"
    }
)

$results = @()
$missingCount = 0
$okCount = 0

foreach ($item in $filesToCheck) {
    $relPath = $item.Path
    $targetUrl = "$cleanR2Url/$relPath"
    $category = $item.Category

    $localPath = if ($LocalR2Root) { Join-Path $LocalR2Root ($relPath -replace '/', '\') } else { $null }
    $localExists = if ($localPath) { Test-Path -LiteralPath $localPath -PathType Leaf } else { $false }
    $localSize = if ($localExists) {
        $bytes = (Get-Item -LiteralPath $localPath).Length
        "{0:N2} MB" -f ($bytes / 1MB)
    } else {
        "N/A"
    }

    $httpStatus = "ERR"
    $cors = "No"
    $contentType = "N/A"
    $remoteSize = "N/A"

    try {
        $response = Invoke-WebRequest -Uri $targetUrl -Method Head -UseBasicParsing -TimeoutSec 10
        $statusCode = [int]$response.StatusCode
        $httpStatus = "$statusCode"

        if ($response.Headers["Access-Control-Allow-Origin"]) {
            $cors = $response.Headers["Access-Control-Allow-Origin"]
        }
        if ($response.Headers["Content-Type"]) {
            $contentType = $response.Headers["Content-Type"]
        }
        if ($response.Headers["Content-Length"]) {
            $len = [long]$response.Headers["Content-Length"]
            $remoteSize = "{0:N2} MB" -f ($len / 1MB)
        }

        if ($statusCode -ge 200 -and $statusCode -lt 300) {
            $okCount++
            $statusLabel = "OK"
        } else {
            $missingCount++
            $statusLabel = "FAIL ($statusCode)"
        }
    }
    catch {
        $ex = $_.Exception
        if ($ex.Response -ne $null -and $ex.Response.StatusCode -ne $null) {
            $code = [int]$ex.Response.StatusCode
            $httpStatus = "$code"
            if ($code -eq 404) {
                $statusLabel = "MISSING (404)"
            } else {
                $statusLabel = "FAIL ($code)"
            }
        } else {
            $httpStatus = "CONN_ERR"
            $statusLabel = "FAIL (Unreachable)"
        }
        $missingCount++
    }

    $results += [PSCustomObject]@{
        Category     = $category
        File         = (Split-Path -Leaf $relPath)
        RemoteStatus = $statusLabel
        HTTPCode     = $httpStatus
        RemoteSize   = $remoteSize
        CORS         = $cors
        LocalFile    = if ($localExists) { "Present ($localSize)" } else { "Missing locally" }
    }
}

$results | Format-Table -AutoSize -Property Category, File, RemoteStatus, HTTPCode, RemoteSize, CORS, LocalFile

Write-Host ""
if ($missingCount -eq 0) {
    Write-Host " SUCCESS: All $okCount required files are available on Cloudflare R2!" -ForegroundColor Green
    Write-Host " WebGL builds and in-game video streaming will load correctly." -ForegroundColor Green
    exit 0
} else {
    Write-Host " WARNING: $missingCount required file(s) are missing (404) on Cloudflare R2!" -ForegroundColor Red
    Write-Host ""
    if ($LocalR2Root -and (Test-Path -LiteralPath (Join-Path $LocalR2Root "StreamingAssets"))) {
        Write-Host " To resolve missing StreamingAssets videos:" -ForegroundColor Yellow
        Write-Host "   1. Open Cloudflare Dashboard -> R2 -> Select your bucket." -ForegroundColor Yellow
        Write-Host "   2. Upload the entire 'StreamingAssets' folder from:" -ForegroundColor Yellow
        Write-Host "      $LocalR2Root\StreamingAssets" -ForegroundColor Yellow
        Write-Host "   3. Ensure bucket path is: StreamingAssets/Videos/*.mp4" -ForegroundColor Yellow
        Write-Host "   4. Re-run this script to verify: .\Tools\Test-CloudflareR2Deployment.ps1" -ForegroundColor Yellow
    }
    Write-Host ""
    exit 1
}
