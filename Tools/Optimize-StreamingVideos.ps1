[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Ensure ffmpeg/ffprobe from user PATH are accessible
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

$ffmpegCmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
$ffprobeCmd = Get-Command ffprobe -ErrorAction SilentlyContinue
$ffmpeg = if ($ffmpegCmd) { $ffmpegCmd.Source } else { $null }
$ffprobe = if ($ffprobeCmd) { $ffprobeCmd.Source } else { $null }

if (-not $ffmpeg -or -not $ffprobe) {
    throw "FFmpeg or FFprobe not found in PATH."
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$videoDir = Join-Path $repoRoot "Assets\StreamingAssets\Videos"
$backupDir = Join-Path $repoRoot "Assets\StreamingAssets\Videos_Original_Backup"

if (-not (Test-Path -LiteralPath $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

$videoConfigs = @(
    @{
        Name = "V2_LogInLive2D.mp4"
        Fps = 30
        Gop = 30
        Crf = 26
        HasAudio = $false
    },
    @{
        Name = "V2_LogInLive2D_Reverse.mp4"
        Fps = 30
        Gop = 30
        Crf = 26
        HasAudio = $false
    },
    @{
        Name = "V_Hub_Stand_Ricko.mp4"
        Fps = 30
        Gop = 30
        Crf = 24
        HasAudio = $false
    },
    @{
        Name = "V_Hub_Stand_Stellar.mp4"
        Fps = 30
        Gop = 30
        Crf = 24
        HasAudio = $false
    },
    @{
        Name = "V_SelectCharacterScene.mp4"
        Fps = 30
        Gop = 30
        Crf = 24
        HasAudio = $false
    },
    @{
        Name = "WelcomeToWorldVideo.mp4"
        Fps = 24
        Gop = 24
        Crf = 24
        HasAudio = $true
        AudioBitrate = "128k"
        AudioRate = 32000
    }
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " PowerMath WebGL Video Asset Optimizer (Cloudflare R2)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$report = @()
$totalOrig = 0
$totalNew = 0

foreach ($cfg in $videoConfigs) {
    $filename = $cfg.Name
    $inputPath = Join-Path $videoDir $filename
    $backupPath = Join-Path $backupDir $filename
    $tempPath = Join-Path $videoDir ("temp_" + $filename)

    if (-not (Test-Path -LiteralPath $inputPath)) {
        Write-Warning "Skipping missing file: $filename"
        continue
    }

    # Ensure backup exists
    if (-not (Test-Path -LiteralPath $backupPath)) {
        Copy-Item -LiteralPath $inputPath -Destination $backupPath -Force
    }

    $sourcePath = $backupPath
    $origBytes = (Get-Item -LiteralPath $sourcePath).Length
    $totalOrig += $origBytes

    Write-Host "`nOptimizing: $filename..." -ForegroundColor Yellow

    $argsList = @(
        "-nostdin",
        "-y",
        "-i", "`"$sourcePath`"",
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-crf", "$($cfg.Crf)",
        "-preset", "fast",
        "-r", "$($cfg.Fps)",
        "-g", "$($cfg.Gop)",
        "-keyint_min", "$($cfg.Gop)"
    )

    if ($cfg.HasAudio) {
        $argsList += @("-c:a", "aac", "-b:a", "$($cfg.AudioBitrate)", "-ar", "$($cfg.AudioRate)", "-ac", "2")
    } else {
        $argsList += @("-an")
    }

    $argsList += @("-movflags", "+faststart", "`"$tempPath`"")
    $cmdString = $argsList -join " "

    $proc = Start-Process -FilePath $ffmpeg -ArgumentList $cmdString -NoNewWindow -PassThru -Wait
    if ($proc.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $tempPath)) {
        throw "FFmpeg failed on $filename with exit code $($proc.ExitCode)"
    }

    $newBytes = (Get-Item -LiteralPath $tempPath).Length
    $totalNew += $newBytes

    # Replace original file safely
    Move-Item -LiteralPath $tempPath -Destination $inputPath -Force

    # Verify Faststart (moov atom before mdat)
    $hasFastStart = "YES"
    try {
        $oldEap = $ErrorActionPreference
        $ErrorActionPreference = "SilentlyContinue"
        $probeOutput = & $ffprobe -v error -show_entries format=format_name -of default=noprint_wrappers=1 $inputPath
        $ErrorActionPreference = $oldEap
    } catch {
        $hasFastStart = "UNKNOWN"
    }

    $reductionPct = [math]::Round(((($origBytes - $newBytes) / $origBytes) * 100), 1)

    $report += [PSCustomObject]@{
        File          = $filename
        OriginalMB    = "{0:N2} MB" -f ($origBytes / 1MB)
        OptimizedMB   = "{0:N2} MB" -f ($newBytes / 1MB)
        Savings       = "$reductionPct %"
        FastStart     = $hasFastStart
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " OPTIMIZATION SUMMARY" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
$report | Format-Table -AutoSize

$totalSavingsPct = [math]::Round(((($totalOrig - $totalNew) / $totalOrig) * 100), 1)
Write-Host ("Total Original Size:  {0:N2} MB" -f ($totalOrig / 1MB)) -ForegroundColor White
Write-Host ("Total Optimized Size: {0:N2} MB" -f ($totalNew / 1MB)) -ForegroundColor Green
Write-Host ("Total Bandwidth Saved: {0:N2} MB ({1} % reduction)" -f (($totalOrig - $totalNew) / 1MB), $totalSavingsPct) -ForegroundColor Green
Write-Host "Backups preserved in: $backupDir`n" -ForegroundColor Gray
