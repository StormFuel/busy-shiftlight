[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0',

    [string]$MSBuildPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repoRoot 'release'
$projectPath = Join-Path $repoRoot 'BusylightShiftLight.csproj'
$testProjectPath = Join-Path $repoRoot 'Tests\BusylightShiftLight.Tests.csproj'

if (-not $MSBuildPath) {
    $msbuildCommand = Get-Command 'MSBuild.exe' -ErrorAction SilentlyContinue
    if ($msbuildCommand) {
        $MSBuildPath = $msbuildCommand.Source
    }
}

if (-not $MSBuildPath) {
    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswherePath) {
        $MSBuildPath = & $vswherePath -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
}

if (-not $MSBuildPath -or -not (Test-Path -LiteralPath $MSBuildPath)) {
    throw 'MSBuild.exe was not found. Install Visual Studio with .NET Framework 4.8 build tools, or pass -MSBuildPath.'
}

Write-Host "Building plugin with $MSBuildPath"
& $MSBuildPath $projectPath /nologo /restore /t:Rebuild /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }

Write-Host 'Building and running logic tests'
& $MSBuildPath $testProjectPath /nologo /restore /t:Rebuild /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }

$testExe = Join-Path $repoRoot 'Tests\bin\Release\BusylightShiftLight.Tests.exe'
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Logic tests failed.' }

$pluginDll = Join-Path $repoRoot 'bin\Release\BusylightShiftLight.dll'
if (-not (Test-Path -LiteralPath $pluginDll)) {
    throw "Expected build output was not found: $pluginDll"
}

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($pluginDll).Version
$expectedAssemblyVersion = [Version]"$Version.0"
if ($assemblyVersion -ne $expectedAssemblyVersion) {
    throw "Release version $Version does not match assembly version $assemblyVersion."
}

[System.IO.Directory]::CreateDirectory($releaseRoot) | Out-Null
$packageName = "BusylightShiftLight-v$Version"
$archivePath = Join-Path $releaseRoot "$packageName.zip"
$stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("busylight-release-" + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stageRoot $packageName

try {
    [System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null
    Copy-Item -LiteralPath $pluginDll -Destination $packageRoot
    Copy-Item -LiteralPath (Join-Path $repoRoot 'INSTALL.md') -Destination $packageRoot
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $packageRoot

    if ([System.IO.File]::Exists($archivePath)) {
        [System.IO.File]::Delete($archivePath)
    }

    Compress-Archive -LiteralPath $packageRoot -DestinationPath $archivePath -CompressionLevel Optimal
}
finally {
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $resolvedStageRoot = [System.IO.Path]::GetFullPath($stageRoot)
    if ($resolvedStageRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedStageRoot)) {
        Remove-Item -LiteralPath $resolvedStageRoot -Recurse -Force
    }
}

$checksumPath = Join-Path $releaseRoot 'SHA256SUMS.txt'
$checksumLines = Get-ChildItem -LiteralPath $releaseRoot -Filter '*.zip' -File |
    Sort-Object Name |
    ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    }
[System.IO.File]::WriteAllLines($checksumPath, $checksumLines, [System.Text.UTF8Encoding]::new($false))

Write-Host "Created $archivePath"
Write-Host "Updated $checksumPath"
