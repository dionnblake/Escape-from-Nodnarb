# Escape from Nodnarb

Original portrait mobile horde shooter built in Unity. Drag the crashed-ship captain left and right while the squad auto-fires. Shoot left-side weapon targets or right-side `+1` crew targets, then survive the alien pressure long enough to reach extraction.

The supplied Hero Wars advertisement is a mechanical reference only. No Hero Wars names, assets, characters, story, audio, UI, or code are included.

## Current build target

- Unity 6.3 LTS `6000.3.21f1`
- Android first, portrait, 60 FPS target
- Built-in Render Pipeline with original low-poly Captain/enemy FBX assets, generated world landmarks, data-driven terrain, and VFX
- Provisional debug package: `com.alphaleverage.escapefromnodnarb`
- Platform-neutral gameplay architecture intended for an iOS port; macOS/Xcode build remains unverified

## Controls

- Touch drag or mouse drag: move the captain and squad horizontally
- Left/right arrows or A/D: desktop movement fallback
- `RAPID FIRE` button or Space: temporary squad fire-rate boost
- Android Back or Escape: pause/resume

## Project shape

- `Assets/EscapeFromNodnarb/Runtime/Core/` - campaign data, run rules, local progress, visual tokens
- `Assets/EscapeFromNodnarb/Runtime/Gameplay/` - boot, arena, squad, enemies, projectiles, audio
- `Assets/EscapeFromNodnarb/Runtime/Resources/` - packaged shader, imported Captain/enemy/world FBX assets, and approved title artwork
- `Assets/EscapeFromNodnarb/Runtime/link.xml` - IL2CPP preservation for runtime-created primitive components
- `Assets/EscapeFromNodnarb/Reference/` - visual-bible references only; references are not runtime assets
- `Assets/EscapeFromNodnarb/Runtime/UI/` - title, sector map, story, HUD, pause, results, loadout
- `Assets/EscapeFromNodnarb/Editor/` - deterministic project setup and Android debug builder
- `Assets/EscapeFromNodnarb/Tests/` - edit-mode rules and play-mode smoke coverage

This repository contains a ten-stage Android-first prototype with a visible route-shaped sector map, original low-poly character and world-landmark assets, readable pickup targets, local progression, stable failure/victory result flow, replay, route beats, boss behavior briefs, pooled combat feedback, enemy health bands, and explicit pickup payoff. Stages 1-10 have distinct environment framing and escape objectives, with later-stage near-field identity for Crystal Fault, Hive Trench, Night Shelf, Beacon Plain, and Extraction Ring. Core terrain remains procedural and needs a final art approval pass before store release. The projectile pool warms 16 slots and grows on demand to keep title startup lighter.

This is an actively developed prototype, not a store-ready release. The Android build has been installed and exercised on a Galaxy S24 Ultra. One fresh-install launch still needs additional startup hardening before release, while normal relaunch and gameplay flow are working. iOS remains an export target until it is built with Xcode on macOS.

## Local commands

Configure the project:

```powershell
$unity = "<path-to-unity>\Editor\Unity.exe"
$arguments = "-batchmode -nographics -quit -projectPath `"$PWD`" -executeMethod EscapeFromNodnarb.Editor.ProjectBootstrapper.Configure -logFile `"$PWD\Logs\bootstrap.log`""
Start-Process $unity -ArgumentList $arguments -Wait
```

Build a local debug APK:

```powershell
$unity = "<path-to-unity>\Editor\Unity.exe"
$env:UNITY_ANDROID_SDK_ROOT = "<path-to-android-sdk>"
$env:UNITY_ANDROID_NDK_ROOT = "<path-to-android-ndk>"
$env:UNITY_JAVA_HOME = "<path-to-openjdk-17>"
$arguments = "-batchmode -nographics -quit -projectPath `"$PWD`" -executeMethod EscapeFromNodnarb.Editor.AndroidBuilder.BuildDebug -logFile `"$PWD\Logs\android-build.log`""
Start-Process $unity -ArgumentList $arguments -Wait
```

Output: `Builds/Android/EscapeFromNodnarb-debug.apk` (ignored from source control).

Generate or revise Blender assets headlessly:

```powershell
blender --background --python "blender escape from nodnarb\escape_from_nodnarb_captain_blender4.py"
blender --background --python "blender escape from nodnarb\nodnarb_world_props_blender.py"
```

Blender 5.2 LTS is required for the generator scripts. Put `blender` on your PATH or replace the command with its full executable path.

## Public repository boundary

The repository intentionally excludes Unity caches, local builds, screenshots/logs, editor settings, private agent instructions, the design transcript, and reference-only material. Generated runtime assets and their source scripts remain included. Review `LICENSES.md` before using the project commercially; no open-source license has been granted yet.

## Local verification snapshot

- Unity 6000.3.21f1 EditMode: 25/25 passed.
- Unity 6000.3.21f1 PlayMode: 31/31 passed.
- Android debug build: ARM64, min SDK 26, target SDK 36, package `com.alphaleverage.escapefromnodnarb`.
- Device smoke run: title, story, combat, squad growth, pause/resume, failure result, replay, and three cold relaunches were exercised on a Galaxy S24 Ultra.
- iOS/macOS/Xcode: not verified.

This repository does not include the local APK, screenshots, logs, or generic verifier output. The generic verifier does not recognize Unity-only projects, so Unity-native tests and Android packaging are the applicable checks. First-install startup hardening, broader device coverage, final art approval, and rights review remain open.

No ads, analytics, purchases, telemetry, accounts, or remote services are enabled.
