# NirSoft PAD URL Batch Update Script
# Populates padUrl field for NirSoft tools in tools.json

$toolsJsonPath = "C:\Users\frend\AppData\Local\Packages\91237e0e-511e-4aca-9f74-bb4bf9e966cf_mtdsxkypkab5p\LocalCache\Roaming\VistaLauncher\tools.json"

# 1. Download PAD links list
Write-Host "Downloading NirSoft PAD links..." -ForegroundColor Cyan
$padLinksUrl = "https://www.nirsoft.net/pad/pad-links.txt"
$padLinks = (Invoke-WebRequest -Uri $padLinksUrl -UseBasicParsing).Content -split "`n"

# 2. Build PAD name -> URL mapping
Write-Host "Building PAD mapping table..." -ForegroundColor Cyan
$padMap = @{}
foreach ($link in $padLinks) {
    $link = $link.Trim()
    if ($link -and $link -match "\.xml$") {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($link).ToLower()
        $padMap[$name] = $link
    }
}
Write-Host "Found $($padMap.Count) PAD files" -ForegroundColor Green

# 3. Read tools.json
Write-Host "Reading tools.json..." -ForegroundColor Cyan
$toolsData = Get-Content -Path $toolsJsonPath -Raw | ConvertFrom-Json

# 4. Match NirSoft tools with padUrl
$matchedCount = 0
$unmatchedNames = @()

foreach ($tool in $toolsData.tools) {
    # Check if it's a NirSoft tool
    $isNirSoft = ($tool.executablePath -match "NirSoft") -or ($tool.homepageUrl -match "nirsoft\.net")

    if ($isNirSoft) {
        # Extract exe name from executablePath
        $exeName = [System.IO.Path]::GetFileNameWithoutExtension($tool.executablePath).ToLower()

        # Find matching PAD URL
        $padUrl = $padMap[$exeName]

        if ($padUrl) {
            # Add padUrl property
            if (-not ($tool.PSObject.Properties.Name -contains "padUrl")) {
                $tool | Add-Member -MemberType NoteProperty -Name "padUrl" -Value $padUrl
            } else {
                $tool.padUrl = $padUrl
            }

            # Set updateSource to "NirSoft" (value 1)
            if (-not ($tool.PSObject.Properties.Name -contains "updateSource")) {
                $tool | Add-Member -MemberType NoteProperty -Name "updateSource" -Value 1
            } else {
                $tool.updateSource = 1
            }

            $matchedCount++
            Write-Host "  [OK] $($tool.name) -> $padUrl" -ForegroundColor Green
        } else {
            $unmatchedNames += "$($tool.name) (exe: $exeName)"
            Write-Host "  [--] $($tool.name) ($exeName) - PAD not found" -ForegroundColor Yellow
        }
    }
}

# 5. Save updated tools.json
Write-Host ""
Write-Host "Saving tools.json..." -ForegroundColor Cyan
$toolsData | ConvertTo-Json -Depth 10 | Set-Content -Path $toolsJsonPath -Encoding UTF8

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Matched: $matchedCount tools" -ForegroundColor Green

if ($unmatchedNames.Count -gt 0) {
    Write-Host "Unmatched: $($unmatchedNames.Count) tools" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Unmatched tools:" -ForegroundColor Yellow
    foreach ($name in $unmatchedNames) {
        Write-Host "  - $name" -ForegroundColor Yellow
    }
}
