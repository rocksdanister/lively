# Taskbar utility integration

Modern Windows 11 taskbar theming is provided by the out-of-process `Lively.Utility.Taskbar.exe` helper from the `rocksdanister/lively-taskbar` repository.

## Runtime layout

Lively Core loads the helper from the app base directory:

```text
plugins/taskbar/Lively.Utility.Taskbar.exe
plugins/taskbar/TaskbarBridge/<arch>/Lively.TaskbarBridge.dll
```

`<arch>` is `x64` for x64 and AnyCPU desktop builds, and `arm64` for arm64 builds.

## Build contract

Build and publish `lively-taskbar` first, then pass its publish directory to Lively Core:

```powershell
dotnet publish .\src\Lively.Utility.Taskbar\Lively.Utility.Taskbar.csproj -c Release -p:Platform=x64

dotnet publish ..\lively\src\Lively\Lively\Lively.csproj -c Release -p:Platform=x64 `
  -p:TaskbarUtilityPublishDir="<lively-taskbar-publish-dir>"
```

When `TaskbarUtilityPublishDir` is set, Lively validates that the helper executable and native bridge DLL exist before publishing. Official package builds can also set `TaskbarUtilityRequired=true` to fail early if the helper is not staged.

MSIX staging validates the same layout under:

```text
src/Lively/Lively.UI.WinUI/Build/plugins/taskbar/
```

## IPC contract

Lively starts the helper with `--parent-pid <lively-pid>` and communicates over UTF-8 JSON lines through stdin/stdout. Diagnostic output must stay on stderr. The shared request and response models live under `Lively.Models/Taskbar`.
