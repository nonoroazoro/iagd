[CmdletBinding()]
param(
    [switch]$InstallDependencies,
    [switch]$SkipHookBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RequiredCommand {
    param(
        [string]$Name,
        [string]$Description
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $command) {
        throw "$Description was not found in PATH."
    }

    return $command.Source
}

function Resolve-MSBuild {
    $command = Get-Command 'msbuild' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -ne $command) {
        return $command.Source
    }

    $vsWhereCommand = Get-Command 'vswhere' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    $vsWherePath = if ($null -ne $vsWhereCommand) {
        $vsWhereCommand.Source
    }
    else {
        $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
        Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
    }

    if (Test-Path -LiteralPath $vsWherePath -PathType Leaf) {
        $candidates = @(& $vsWherePath -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')
        if ($LASTEXITCODE -eq 0) {
            $candidate = $candidates |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
                Select-Object -First 1
            if ($null -ne $candidate) {
                return $candidate
            }
        }
    }

    throw 'Visual Studio MSBuild was not found in PATH or through vswhere.'
}

$repositoryRoot = $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot 'IAGrim-core.sln'
$hookSolutionPath = Join-Path $repositoryRoot 'HookDll\Hook\GDIAHook.sln'
$webUiRoot = Join-Path $repositoryRoot 'WebUI'
$webBuildRoot = Join-Path $webUiRoot 'build'
$managedOutput = Join-Path $repositoryRoot 'IAGrim\bin\Release\net10.0-windows\win-x64'
$retailHook = Join-Path $repositoryRoot 'HookDll\Hook\x64\Release\ItemAssistantHook_x64.dll'
$playtestHook = Join-Path $repositoryRoot 'HookDll\Hook\x64\Release-playtest\ItemAssistantHook_x64.dll'
$artifactRoot = Join-Path $repositoryRoot 'artifacts'

$pathDotnet = $null
$pathDotnetCommand = Get-Command 'dotnet' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -ne $pathDotnetCommand) {
    $pathDotnetVersion = & $pathDotnetCommand.Source --version
    if ($LASTEXITCODE -eq 0 -and
        -not [string]::IsNullOrWhiteSpace($pathDotnetVersion) -and
        [Version]$pathDotnetVersion -ge [Version]'10.0') {
        $pathDotnet = $pathDotnetCommand.Source
    }
}

$repositoryDotnet = Join-Path $repositoryRoot '.tools\dotnet\dotnet.exe'
if ($null -eq $pathDotnet -and (Test-Path -LiteralPath $repositoryDotnet -PathType Leaf)) {
    $pathDotnet = (Resolve-Path -LiteralPath $repositoryDotnet).Path
}

if ($null -eq $pathDotnet) {
    throw '.NET SDK 10 or newer was not found in PATH or .tools\dotnet.'
}

$dotnetVersion = & $pathDotnet --version
if ($LASTEXITCODE -ne 0 -or [Version]$dotnetVersion -lt [Version]'10.0') {
    throw 'The repository-local .NET SDK is invalid or too old.'
}
$pathNpm = Resolve-RequiredCommand -Name 'npm' -Description 'npm'
$pathMsBuild = $null
$pathBoost = $null
$boostLibraryPath = $null
$windowsSdkVersion = $null
if (-not $SkipHookBuild) {
    $pathMsBuild = Resolve-MSBuild
    $pathBoost = [Environment]::GetEnvironmentVariable('BOOST', 'Process')
    if ([string]::IsNullOrWhiteSpace($pathBoost)) {
        $boostCandidates = @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.tools') -Directory -Filter 'boost_*' -ErrorAction SilentlyContinue)
        if ($boostCandidates.Count -eq 1) {
            $pathBoost = $boostCandidates[0].FullName
        }
    }

    if ([string]::IsNullOrWhiteSpace($pathBoost) -or
        -not (Test-Path -LiteralPath $pathBoost -PathType Container)) {
        throw 'BOOST must point to a Boost 1.78.0 directory.'
    }

    $boostVersionHeader = Join-Path $pathBoost 'boost\version.hpp'
    if (-not (Test-Path -LiteralPath $boostVersionHeader -PathType Leaf)) {
        throw 'BOOST does not contain the required headers.'
    }

    $boostVersion = Get-Content -LiteralPath $boostVersionHeader |
        Select-String -Pattern '^#define BOOST_VERSION\s+107800$'
    if ($null -eq $boostVersion) {
        throw 'Boost 1.78.0 is required.'
    }

    $boostLibraryPath = [Environment]::GetEnvironmentVariable('BOOST_LIBRARYDIR', 'Process')
    if ([string]::IsNullOrWhiteSpace($boostLibraryPath)) {
        $boostLibraryCandidates = @(Get-ChildItem -LiteralPath $pathBoost -Directory -Filter 'lib64-msvc-*')
        if ($boostLibraryCandidates.Count -ne 1) {
            throw 'Set BOOST_LIBRARYDIR when BOOST does not contain exactly one lib64-msvc-* directory.'
        }

        $boostLibraryPath = $boostLibraryCandidates[0].FullName
    }

    if (-not (Test-Path -LiteralPath $boostLibraryPath -PathType Container)) {
        throw 'BOOST_LIBRARYDIR does not point to a directory.'
    }

    $windowsKits = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue
    if ($null -eq $windowsKits -or [string]::IsNullOrWhiteSpace($windowsKits.KitsRoot10)) {
        throw 'Windows 10 SDK was not found.'
    }

    $windowsSdkCandidates = @(Get-ChildItem -LiteralPath (Join-Path $windowsKits.KitsRoot10 'Include') -Directory |
        Where-Object { $_.Name -match '^10\.\d+\.\d+\.\d+$' } |
        Sort-Object { [Version]$_.Name } -Descending)
    if ($windowsSdkCandidates.Count -eq 0) {
        throw 'Windows 10 SDK include files were not found.'
    }

    $windowsSdkVersion = $windowsSdkCandidates[0].Name
}

$repositoryFullPath = [IO.Path]::GetFullPath($repositoryRoot).TrimEnd('\', '/')
$artifactFullPath = [IO.Path]::GetFullPath($artifactRoot).TrimEnd('\', '/')
$artifactParentPath = [IO.Directory]::GetParent($artifactFullPath).FullName.TrimEnd('\', '/')
if (-not $artifactParentPath.Equals($repositoryFullPath, [StringComparison]::OrdinalIgnoreCase) -or
    -not [IO.Path]::GetFileName($artifactFullPath).Equals('artifacts', [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear an unexpected artifact directory: $artifactFullPath"
}

if (Test-Path -LiteralPath $artifactFullPath -PathType Container) {
    Remove-Item -LiteralPath $artifactFullPath -Recurse -Force
}
[IO.Directory]::CreateDirectory($artifactFullPath) | Out-Null

& $pathDotnet clean $solutionPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw ".NET clean failed with exit code $LASTEXITCODE."
}

& $pathDotnet build $solutionPath --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw ".NET build failed with exit code $LASTEXITCODE."
}

Push-Location $webUiRoot
try {
    if ($InstallDependencies) {
        & $pathNpm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci failed with exit code $LASTEXITCODE."
        }
    }

    & $pathNpm exec eslint -- 'src/**/*.{js,jsx,ts,tsx}'
    if ($LASTEXITCODE -ne 0) {
        throw "WebUI lint failed with exit code $LASTEXITCODE."
    }

    & $pathNpm run build
    if ($LASTEXITCODE -ne 0) {
        throw "WebUI build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not $SkipHookBuild) {
    & $pathMsBuild $hookSolutionPath /m /t:Rebuild /p:Configuration=Release /p:Platform=x64 "/p:BOOST=$pathBoost" "/p:BOOST_LIBRARYDIR=$boostLibraryPath" "/p:WindowsTargetPlatformVersion=$windowsSdkVersion"
    if ($LASTEXITCODE -ne 0) {
        throw "Retail hook build failed with exit code $LASTEXITCODE."
    }

    & $pathMsBuild $hookSolutionPath /m /t:Rebuild /p:Configuration=Release-playtest /p:Platform=x64 "/p:BOOST=$pathBoost" "/p:BOOST_LIBRARYDIR=$boostLibraryPath" "/p:WindowsTargetPlatformVersion=$windowsSdkVersion"
    if ($LASTEXITCODE -ne 0) {
        throw "Playtest hook build failed with exit code $LASTEXITCODE."
    }
}

foreach ($requiredPath in @($managedOutput, $webBuildRoot, $retailHook, $playtestHook)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required build output is missing: $requiredPath"
    }
}

$managedVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $managedOutput 'IAGrim.dll')).FileVersion
$hookVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($retailHook).ProductVersion

$webFiles = @(Get-ChildItem -LiteralPath $webBuildRoot -Recurse -File)
$webTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in $webFiles) {
    $relativePath = [IO.Path]::GetRelativePath($webBuildRoot, $file.FullName)
    $webTargets.Add((Join-Path 'Resources' $relativePath)) | Out-Null
}

foreach ($file in Get-ChildItem -LiteralPath $managedOutput -Recurse -File) {
    $relativePath = [IO.Path]::GetRelativePath($managedOutput, $file.FullName)
    if ($file.Extension -ieq '.pdb' -or
        $relativePath.StartsWith('UserData\', [StringComparison]::OrdinalIgnoreCase) -or
        $relativePath.StartsWith('Resources\assets\', [StringComparison]::OrdinalIgnoreCase) -or
        $webTargets.Contains($relativePath)) {
        continue
    }

    $destinationPath = Join-Path $artifactRoot $relativePath
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destinationPath)) | Out-Null
    [IO.File]::Copy($file.FullName, $destinationPath, $true)
}

foreach ($file in $webFiles) {
    $relativePath = [IO.Path]::GetRelativePath($webBuildRoot, $file.FullName)
    $destinationPath = Join-Path $artifactRoot (Join-Path 'Resources' $relativePath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destinationPath)) | Out-Null
    [IO.File]::Copy($file.FullName, $destinationPath, $true)
}

[IO.File]::Copy($retailHook, (Join-Path $artifactRoot 'ItemAssistantHook_x64.dll'), $true)
[IO.File]::Copy($playtestHook, (Join-Path $artifactRoot 'ItemAssistantHook_playtest_x64.dll'), $true)
[IO.File]::Copy((Join-Path $repositoryRoot 'LICENSE'), (Join-Path $artifactRoot 'LICENSE'), $true)
[IO.File]::WriteAllText(
    (Join-Path $artifactRoot 'dllver.txt'),
    "$hookVersion$([Environment]::NewLine)",
    [Text.UTF8Encoding]::new($false)
)

$requiredFiles = @(
    'IAGrim.exe',
    'IAGrim.dll',
    'ItemAssistantHook_x64.dll',
    'ItemAssistantHook_playtest_x64.dll',
    'dllver.txt',
    'Resources\index.html',
    'Resources\assets\index.js',
    'Resources\assets\index.css'
)
foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $artifactRoot $requiredFile) -PathType Leaf)) {
        throw "Artifact verification failed. Missing file: $requiredFile"
    }
}

$unexpectedFiles = @(Get-ChildItem -LiteralPath $artifactRoot -Recurse -File |
    Where-Object {
        $_.Extension -ieq '.pdb' -or
        [IO.Path]::GetRelativePath($artifactRoot, $_.FullName).StartsWith('UserData\', [StringComparison]::OrdinalIgnoreCase)
    })
if ($unexpectedFiles.Count -gt 0) {
    throw "Artifact verification failed. Unexpected file: $($unexpectedFiles[0].FullName)"
}

$artifactFiles = @(Get-ChildItem -LiteralPath $artifactRoot -Recurse -File)
$artifactSize = ($artifactFiles | Measure-Object -Property Length -Sum).Sum
Write-Output "Artifacts: $artifactRoot"
Write-Output "Version: $managedVersion"
Write-Output "Files: $($artifactFiles.Count)"
Write-Output "Size: $artifactSize bytes"
