# Stair Builder

Version **0.9.14**. A Cities: Skylines II code mod for customizable stairs and ramps.

- [Download on Paradox Mods](https://mods.paradoxplaza.com/mods/166123/Windows)
- [English guide](docs/description.en.md)
- [한국어 사용법](docs/description.ko.md)
- [Required asset pack](https://mods.paradoxplaza.com/mods/166122/Any)
- [Questions and bug reports](https://forum.paradoxplaza.com/forum/threads/mod-stair-builder-custom-stairs-and-ramps-discussion-feedback.1948247/)

## Build

Use Windows with Cities: Skylines II's official code modding toolchain installed and its `CSII_*` environment variables configured. You also need .NET and Node.js 18 or later with npm.

```powershell
dotnet build src/StairBuilder.csproj -c Release
cd ui
npm ci
npm run build
```

Build C# first, then UI: the game toolchain replaces the deployment directory during the C# build. The output is `local/StairBuilder`. NuGet restores Harmony 2.3.3. The UI uses the game's React/API modules and bundles its own styles and icons. A standalone checkout uses `ui/node_modules`; the sibling-project fallback in webpack is only for the author's shared development workspace.

For local use, close the game and copy `StairBuilder.dll`, `StairBuilder_win_x86_64.dll`, `0Harmony.dll`, `StairBuilder.mjs`, `StairBuilder.css`, and `images` from the output to the game's `Mods/StairBuilder` directory. Include `lang` and `LICENSE-Harmony.txt` from this repository. Avoid loading both the subscribed code mod and a local copy.

## Runtime dependencies

Enable the required asset pack in the same playset. Models and textures are distributed separately and are not included here. Gizmo XYZ and Move It integrations are optional; neither assembly is a build dependency.

Performance logging is opt-in and off by default. The CityBridge adapter code is included to match the published assembly; CityBridge is not required for normal use. Mutation probes require the adapter's protected test-session checks.

## Data and limitations

Stair designs are serialized into city saves. Templates are separate local data in `ModsData/StairBuilder/templates.json` under the game's user data folder. Back up that file along with your city when moving to another PC. Disabling the mod is not a supported way to preserve its rendered staircases.

Pedestrian access depends on endpoint connections. Gizmo integration supports translation only. Move It support is limited to a single staircase via the custom bridge, without native mixed selections, marquee selection, rotation, or undo/redo.

The 0.9.14 C#/Burst and UI builds passed. The latest selection-stage Gizmo shortcuts and template workflow have not yet been rechecked in a live game. Build success is not a claim of full mod-combination compatibility.

## Source and third-party code

This repository contains the published code mod's source and UI, not game assemblies, decompiled game code, asset-pack models, personal logs, or saves. Harmony is third-party software; its license is in `LICENSE-Harmony.txt`. No additional license grant is declared for the project source.
