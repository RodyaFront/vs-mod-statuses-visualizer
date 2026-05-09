# PlayerStatusStrip Dev Environment

This file documents machine-local values needed for build and IDE reference resolution.

## Required path

- `VINTAGE_STORY`: `D:\Games\Vintagestory`

The directory must contain:

- `VintagestoryAPI.dll`
- `Lib/cairo-sharp.dll`

## Current project behavior

`PlayerStatusStrip/Directory.Build.props` provides a fallback:

- if `VINTAGE_STORY` is not set, MSBuild uses `D:\Games\Vintagestory`.

## Recommended setup

Set `VINTAGE_STORY` as a user environment variable so all tools (IDE, build, tests, scripts) use one source of truth.

PowerShell example:

```powershell
[Environment]::SetEnvironmentVariable("VINTAGE_STORY", "D:\Games\Vintagestory", "User")
```

Restart IDE/terminal after changing environment variables.
