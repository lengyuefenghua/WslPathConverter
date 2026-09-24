# Agent Instructions

## Project boundaries

- Single legacy C# WinForms tray app targeting .NET Framework 4.7.2; not SDK-style, so the `dotnet` CLI (`build`/`test`/`run`) does not apply.
- `Program.Main` (single-instance via a named mutex) starts `TrayApplicationContext`, which coordinates clipboard inspection, conversion, and pasting. `PathConverter` does text formatting only; `WslPathService` shells out to `wsl.exe wslpath`.
- `WslPathConverter.csproj` uses an explicit `<Compile>` list: any new `.cs` file must be added there or it is silently not compiled.
- `LangVersion` is pinned to 5. Do not use C# 6+ syntax (string interpolation, `nameof`, `?.`, expression-bodied members).

## Build and verification

- Build Release from this directory:
  `& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" .\WslPathConverter.csproj /t:Build /p:Configuration=Release`
- Compiling is the only automated check. Output: `bin\Release\WslPathConverter.exe`. `bin\` and `obj\` are generated (gitignored), never edit them.
- No test runner, test project, CI workflow, formatter, or package manager is configured. Do not run `dotnet test`.
- `Tests\PathConverterTests.cs` is a non-compiled, hand-written assertion helper with no entry point; it must not be described as an automated test suite.

## Runtime constraints

- Requires Windows + .NET Framework 4.7.2+ + an installed WSL distro; conversion depends on `wsl.exe wslpath` being available.
- It is a tray process (no console window). Settings live in `%APPDATA%\Wsl Path Converter CSharp\settings.ini`; clipboard images default to `%TEMP%\wsl-path-converter`. Restart the app to apply source changes.
- `wsl.exe` is launched with `UseShellExecute = false` and quoted args; Windows paths are normalized to forward slashes before conversion. Preserve this or backslash forwarding breaks.
- The conversion hotkey inspects the clipboard first; if nothing converts it re-sends `Ctrl+Shift+V`. Do not add an unconditional `Ctrl+V` hook.
