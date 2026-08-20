# Building and Packaging

## Requirements

- Windows
- PowerShell
- Node.js and npm available through `PATH`
- Visual Studio MSBuild available through `PATH` or discoverable through `vswhere`
- Visual C++ build tools and a Windows 10 SDK
- .NET SDK 10
- Boost 1.78.0 headers and x64 compiled libraries

The package script uses `dotnet` from `PATH` when it is .NET SDK 10 or newer. It falls back to `.tools/dotnet/dotnet.exe` only when the PATH SDK is too old.

The package script uses `MSBuild` from `PATH` when available. Otherwise, it uses the Visual Studio Installer's `vswhere` tool to locate MSBuild.

For Boost, either place one `boost_*` directory under `.tools`, or set `BOOST` to the Boost root. If the Boost root contains more than one `lib64-msvc-*` directory, set `BOOST_LIBRARYDIR` to the directory matching the active compiler.

The `.tools` directory is ignored by Git and must not be committed.

## Build

Run from the repository root:

```powershell
.\build-package.ps1
```

The script builds the managed application, lints and builds WebUI, rebuilds both x64 hook variants, and writes the verified runnable files directly under `artifacts`.

Dependency installation is opt-in:

```powershell
.\build-package.ps1 -InstallDependencies
```

To reuse existing hook DLLs without rebuilding native code:

```powershell
.\build-package.ps1 -SkipHookBuild
```

Before building, the script verifies and clears only the repository `artifacts` directory. Generated artifacts do not include `UserData` or PDB files.

## GitHub Release

The release workflow performs the same production build on a clean Windows runner. It installs its own .NET, Node.js, and Boost dependencies and never depends on the local `.tools` directory.

Trigger a release from the repository default branch with:

```powershell
gh workflow run release.yml --ref master -f command=release
```

The workflow reads the generated IAGD production version, creates a GitHub Release with that version as its tag, and uploads `iagd-<version>-win-x64.zip`. The ZIP root contains the normal files produced under `artifacts`, without an extra wrapper directory.
