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

function Add-ZipFile {
    param(
        [IO.Compression.ZipArchive]$Archive,
        [string]$SourcePath,
        [string]$EntryPath
    )

    $normalizedEntryPath = $EntryPath.Replace('\', '/')
    $entry = $Archive.CreateEntry($normalizedEntryPath, [IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = (Get-Item -LiteralPath $SourcePath).LastWriteTime
    $source = [IO.File]::OpenRead($SourcePath)
    $destination = $entry.Open()
    try {
        $source.CopyTo($destination)
    }
    finally {
        $destination.Dispose()
        $source.Dispose()
    }
}

function Add-ZipText {
    param(
        [IO.Compression.ZipArchive]$Archive,
        [string]$Text,
        [string]$EntryPath
    )

    $normalizedEntryPath = $EntryPath.Replace('\', '/')
    $entry = $Archive.CreateEntry($normalizedEntryPath, [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false), 1024, $true)
    try {
        $writer.WriteLine($Text)
        $writer.Flush()
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
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

$repositoryDotnet = Join-Path $repositoryRoot '.tools\dotnet\dotnet.exe'
if (Test-Path -LiteralPath $repositoryDotnet -PathType Leaf) {
    $pathDotnet = (Resolve-Path -LiteralPath $repositoryDotnet).Path
}
else {
    $pathDotnet = Resolve-RequiredCommand -Name 'dotnet' -Description '.NET SDK'
}
$pathNpm = Resolve-RequiredCommand -Name 'npm' -Description 'npm'
$pathMsBuild = $null
$pathBoost = $null
$boostLibraryPath = $null
$windowsSdkVersion = $null
if (-not $SkipHookBuild) {
    $pathMsBuild = Resolve-RequiredCommand -Name 'msbuild' -Description 'Visual Studio MSBuild'
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
$zipPath = Join-Path $artifactRoot "GDItemAssistant-custom-$managedVersion-win-x64.zip"
if (Test-Path -LiteralPath $zipPath) {
    throw "Output already exists and will not be overwritten: $zipPath"
}

[IO.Directory]::CreateDirectory($artifactRoot) | Out-Null
$webFiles = @(Get-ChildItem -LiteralPath $webBuildRoot -Recurse -File)
$webTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in $webFiles) {
    $relativePath = [IO.Path]::GetRelativePath($webBuildRoot, $file.FullName)
    $webTargets.Add((Join-Path 'Resources' $relativePath)) | Out-Null
}

$memory = [IO.MemoryStream]::new()
$archive = [IO.Compression.ZipArchive]::new($memory, [IO.Compression.ZipArchiveMode]::Create, $true)
try {
    foreach ($file in Get-ChildItem -LiteralPath $managedOutput -Recurse -File) {
        $relativePath = [IO.Path]::GetRelativePath($managedOutput, $file.FullName)
        if ($file.Extension -ieq '.pdb' -or
            $relativePath.StartsWith('UserData\', [StringComparison]::OrdinalIgnoreCase) -or
            $relativePath.StartsWith('Resources\assets\', [StringComparison]::OrdinalIgnoreCase) -or
            $webTargets.Contains($relativePath)) {
            continue
        }

        Add-ZipFile -Archive $archive -SourcePath $file.FullName -EntryPath (Join-Path 'GDItemAssistant' $relativePath)
    }

    foreach ($file in $webFiles) {
        $relativePath = [IO.Path]::GetRelativePath($webBuildRoot, $file.FullName)
        Add-ZipFile -Archive $archive -SourcePath $file.FullName -EntryPath (Join-Path 'GDItemAssistant\Resources' $relativePath)
    }

    Add-ZipFile -Archive $archive -SourcePath $retailHook -EntryPath 'GDItemAssistant\ItemAssistantHook_x64.dll'
    Add-ZipFile -Archive $archive -SourcePath $playtestHook -EntryPath 'GDItemAssistant\ItemAssistantHook_playtest_x64.dll'
    Add-ZipFile -Archive $archive -SourcePath (Join-Path $repositoryRoot 'LICENSE') -EntryPath 'GDItemAssistant\LICENSE'
    Add-ZipText -Archive $archive -Text $hookVersion -EntryPath 'GDItemAssistant\dllver.txt'
}
finally {
    $archive.Dispose()
}

try {
    [IO.File]::WriteAllBytes($zipPath, $memory.ToArray())
}
finally {
    $memory.Dispose()
}

$verificationArchive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entryNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $verificationArchive.Entries) {
        $entryNames.Add($entry.FullName.Replace('\', '/')) | Out-Null
    }

    $requiredEntries = @(
        'GDItemAssistant/IAGrim.exe',
        'GDItemAssistant/IAGrim.dll',
        'GDItemAssistant/ItemAssistantHook_x64.dll',
        'GDItemAssistant/ItemAssistantHook_playtest_x64.dll',
        'GDItemAssistant/dllver.txt',
        'GDItemAssistant/Resources/index.html',
        'GDItemAssistant/Resources/assets/index.js',
        'GDItemAssistant/Resources/assets/index.css'
    )
    foreach ($requiredEntry in $requiredEntries) {
        if (-not $entryNames.Contains($requiredEntry)) {
            throw "ZIP verification failed. Missing entry: $requiredEntry"
        }
    }

    foreach ($entryName in $entryNames) {
        if ($entryName -match '(^|/)UserData/' -or $entryName.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase)) {
            throw "ZIP verification failed. Unexpected entry: $entryName"
        }
    }
}
finally {
    $verificationArchive.Dispose()
}

$zipFile = Get-Item -LiteralPath $zipPath
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Output "Package: $($zipFile.FullName)"
Write-Output "Size: $($zipFile.Length) bytes"
Write-Output "SHA-256: $zipHash"
