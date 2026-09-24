# WSL Path Converter C#

This is an independent C# implementation of the tray application. The original AutoHotkey implementation remains in the repository.

## Requirements

- Windows 7 SP1 or later
- .NET Framework 4.7.2 or later
- WSL installed for `wsl.exe wslpath` conversion

## Build

From a Developer Command Prompt or PowerShell:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" .\WslPathConverter.csproj /t:Build /p:Configuration=Release
```

The output is `bin\Release\WslPathConverter.exe`.

## Usage

Start `WslPathConverter.exe`. It runs in the system tray and registers `Ctrl+Shift+V` by default.

Copy a Windows path and press the conversion hotkey in a WSL terminal:

```text
C:\Users\me\project -> /mnt/c/Users/me/project
```

The C# implementation calls `wsl.exe wslpath`. Windows paths are normalized to forward slashes before invocation so that `wsl.exe` argument forwarding does not remove backslashes.

When the clipboard contains an image, the image is saved as `%TEMP%\wsl-path-converter\clipboard.png`, converted with `wslpath`, and the WSL path is pasted. If the target file is locked, a timestamped file is used and a tray notification shows its path.

Use the tray menu to configure image support, the file name, save directory, path format, and hotkey. Normal `Ctrl+V` is not intercepted, and the clipboard is inspected only after the conversion hotkey is pressed.

Settings are stored in `%APPDATA%\Wsl Path Converter CSharp\settings.ini`.
