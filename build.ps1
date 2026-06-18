param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
$projectDir = "$root\TomatoRadar"
$publishDir = "$projectDir\bin\Release\net6.0-windows\publish"
$installerScript = "$root\installer.nsi"
$nsis = "C:\Program Files (x86)\NSIS\makensis.exe"
$artifactsDir = "$root\artifacts"
$downloadBaseUrl = "https://dl.localizedkorabli.org/tomatoradar/app"
$csprojFile = "$projectDir\TomatoRadar.csproj"
$settingsFile = "$projectDir\Properties\Settings.settings"
$designerFile = "$projectDir\Properties\Settings.Designer.cs"
$appConfigFile = "$projectDir\App.config"

# 从 .csproj 读取版本号
if (-not (Test-Path $csprojFile)) { throw "File not found: $csprojFile" }
$csprojContent = [System.IO.File]::ReadAllText($csprojFile)
if ($csprojContent -notmatch '<AssemblyVersion>([\d.]+)</AssemblyVersion>') {
    throw "Version not found in $csprojFile"
}
$version = $Matches[1]
$build = (Get-Date).ToString("yyMMddHHmmss")
$tag = "v$version-$build"

Write-Host "=== Version: $version  Build: $build ===" -ForegroundColor Cyan

Write-Host "==> Syncing version into Settings.settings..." -ForegroundColor Cyan
$xml = [xml](Get-Content $settingsFile -Encoding UTF8)
$ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/VisualStudio/2004/01/settings")
$xml.SelectSingleNode("//ns:Setting[@Name='SoftwareVersion']/ns:Value", $ns).InnerText = $version
$xml.SelectSingleNode("//ns:Setting[@Name='SoftwareDate']/ns:Value", $ns).InnerText = $build
$xml.Save($settingsFile)

Write-Host "==> Syncing version into App.config..." -ForegroundColor Cyan
foreach ($file in @($appConfigFile)) {
    $cfg = [xml](Get-Content $file -Encoding UTF8)
    $cfg.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareVersion']/value").InnerText = $version
    $cfg.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareDate']/value").InnerText = $build
    $cfg.Save($file)
}

Write-Host "==> Syncing Settings.Designer.cs default value..." -ForegroundColor Cyan
$lines = Get-Content $designerFile -Encoding UTF8
$newLines = @()
$swFound = $false
foreach ($line in $lines) {
    if ($line -match 'DefaultSettingValueAttribute\("') {
        if ($swFound) {
            $line = $line -replace '"[^"]*"', "`"$build`""
        } else {
            $line = $line -replace '"[^"]*"', "`"$version`""
            $swFound = $true
        }
    }
    $newLines += $line
}
Set-Content -Path $designerFile -Value $newLines -Encoding UTF8

Write-Host "==> Publishing..." -ForegroundColor Cyan
dotnet publish "$projectDir\TomatoRadar.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

# 构建后，将版本同步到已发布的配置
$publishedConfigPath = Join-Path $publishDir "TomatoRadar.dll.config"
if (Test-Path $publishedConfigPath) {
    $cfg = [xml](Get-Content $publishedConfigPath -Encoding UTF8)
    $cfg.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareVersion']/value").InnerText = $version
    $cfg.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareDate']/value").InnerText = $build
    $cfg.Save($publishedConfigPath)
    Write-Host "  Updated: $publishedConfigPath"
}

Write-Host "==> Building installer..." -ForegroundColor Cyan
& $nsis "/DPRODUCT_VERSION=$version" "/DPRODUCT_BUILD=$build" $installerScript
if ($LASTEXITCODE -ne 0) { throw "NSIS failed" }

if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

$exe = Get-ChildItem $artifactsDir -Filter "*_Setup.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $exe) { throw "Installer not found" }
Write-Host "Installer: $($exe.Name)" -ForegroundColor Cyan

Write-Host "==> Computing SHA256..." -ForegroundColor Cyan
$sha256 = (Get-FileHash -Path $exe.FullName -Algorithm SHA256).Hash.ToUpper()

$meta = @{
    update_server_enabled    = $true
    software_latest_version  = $version
    software_latest_date     = $build
    software_latest_url      = "$downloadBaseUrl/$($exe.Name)"
    software_latest_sha256   = $sha256
    shiplist_metadata        = @{
        wg    = "https://dl.localizedkorabli.org/tomatoradar/ships/wg.json"
        lesta = "https://dl.localizedkorabli.org/tomatoradar/ships/lesta.json"
    }
}
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$metadataPath = "$root\metadata.json"
[System.IO.File]::WriteAllText($metadataPath, ($meta | ConvertTo-Json -Depth 3), $utf8NoBom)
Write-Host "Metadata: $metadataPath" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "Dry-run: would create release $tag" -ForegroundColor Yellow
    exit 0
}

Write-Host "==> Staging version files..." -ForegroundColor Cyan
git add $settingsFile $designerFile $appConfigFile $metadataPath

Write-Host "==> Stashing other working tree changes..." -ForegroundColor Cyan
git stash --keep-index --include-untracked
$hadStash = ($LASTEXITCODE -eq 0)

Write-Host "==> Committing version bump..." -ForegroundColor Cyan
git diff --cached --quiet
if ($LASTEXITCODE -eq 1) {
    git commit -m "chore: bump version to $version ($build)"
    if ($LASTEXITCODE -ne 0) { throw "Commit failed" }

    # check if remote origin exists
    $hasRemote = git remote | Select-String -Pattern "^origin$"
    if ($hasRemote) {
        git pull --rebase origin main
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Warning: pull failed, skipping push. You may need to pull manually." -ForegroundColor Yellow
        } else {
            git push origin main
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Warning: push failed. You may need to push manually." -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "No remote 'origin' configured, skipping pull/push." -ForegroundColor Yellow
    }
} else {
    Write-Host "No version changes to commit" -ForegroundColor Yellow
}

if ($hadStash) {
    Write-Host "==> Restoring working tree..." -ForegroundColor Cyan
    git stash pop
}

Write-Host "==> Creating GitHub Release $tag..." -ForegroundColor Cyan
gh release create $tag "$($exe.FullName)" `
    --title "$tag" `
    --notes "See CHANGELOG or commit history for details." `
    --target main

if ($LASTEXITCODE -eq 0) {
    Write-Host "Release $tag created successfully!" -ForegroundColor Green
    Write-Host "GitHub Actions will now publish to R2 and update metadata." -ForegroundColor Cyan
}
