# Building and Packaging

## Requirements

- Windows
- PowerShell
- Node.js and npm available through `PATH`
- Visual Studio MSBuild available through `PATH`
- Visual C++ build tools and a Windows 10 SDK
- .NET SDK 10
- Boost 1.78.0 headers and x64 compiled libraries

The package script uses `dotnet` from `PATH` when it is .NET SDK 10 or newer. It falls back to `.tools/dotnet/dotnet.exe` only when the PATH SDK is too old.

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
