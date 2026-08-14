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
- Move the captain to the matching rail, then tap that card during the choice window
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
- `STORYBOARD.md` - ten-stage still-panel story beats and editable radio direction
- `AUDIO_DESIGN.md` - sound-effect inventory, layered music direction, and provenance rules
- `Assets/EscapeFromNodnarb/Editor/` - deterministic project setup and Android debug/release-candidate builders
- `Assets/EscapeFromNodnarb/Tests/` - edit-mode rules and play-mode smoke coverage
- `Tools/Android/` - local APK identity audit and repeatable cold-start evidence helpers
- `Tools/validate-asset-provenance.ps1` - local runtime-asset inventory and license-row coverage check

This repository contains a ten-stage Android-first prototype with a visible route-shaped sector map, original low-poly character and world-landmark assets, readable pickup targets, local progression, stable failure/victory result flow, replay, route beats, ecology-specific encounters, still-panel storyboard beats, layered generated audio, pooled combat feedback, enemy health bands, explicit pickup payoff, and procedural movement feedback for the Captain and visible crew. Stages 1-10 have distinct wilderness framing, ecology, route identity, and escape objectives, with later-stage identity for Crystal Fault, Hive Trench, Night Shelf, Beacon Plain, and Extraction Ring. The Captain leads the crew toward the incoming horde while the crew trails toward the camera. Core terrain remains procedural and needs a final art approval pass before store release. The projectile pool warms 16 slots and grows on demand, while combat actors and paired cards now reuse bounded visual pools. Android boot now stages title construction after the first frame and defers the remaining screen hierarchy before enabling title interaction. Local replay counters, comeback pacing, captions, high contrast, and reduced-motion controls are included without remote telemetry.

This is an actively developed prototype, not a store-ready release. Production sprint 15 adds direct card selection with a visible source report and terminal pickup payoff, throttles avoidable music and haptic churn, adds development-build profiler boundaries, and provides local Android package/cold-start audit helpers. Production sprint 16 adds one shared gesture policy, action-driven onboarding, phase-attributed runtime budgets, pool trimming/stability evidence, campaign pacing guards, bounded progression values, v3 save migration, safe-area normalization, and asset-provenance coverage. Production sprint 17 adds explicit gameplay/first-volley startup markers, per-phase worst timings, terminal readability/balance reports, v1/v2 migration rehearsal, progression monotonicity guards, a Unity-specific source verifier, anonymous replay-sheet validation, and local Android startup/performance capture helpers. Production sprint 19 implements forty local mitigations and evidence checks; the current source passes 68/68 EditMode and 56/56 PlayMode, and the exact final debug APK is installed over wireless ADB on the physical S24 Ultra. The physical trace is diagnostic, not a 60 FPS signoff. Graybox/procedural art, authored animation, final audio, rights approval, broader campaign replay, outside testing, physical touch/accessibility matrix, store identity/signing/privacy, and iOS remain open release gates.

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

Build local non-development release-candidate artifacts for performance and package verification:

```powershell
$unity = "<path-to-unity>\Editor\Unity.exe"
$arguments = "-batchmode -nographics -quit -projectPath `"$PWD`" -executeMethod EscapeFromNodnarb.Editor.AndroidBuilder.BuildReleaseArtifacts -logFile `"$PWD\Logs\android-build-release-local.log`""
Start-Process $unity -ArgumentList $arguments -Wait
```

Outputs: `Builds/Android/EscapeFromNodnarb-release-local.apk` and `.aab`. These are local non-development artifacts, not production-signed store submissions.

Audit the exact local APK:

```powershell
& ".\Tools\Android\production-sprint-15-audit.ps1" -ApkPath ".\Builds\Android\EscapeFromNodnarb-release-local.apk"
```

Run three local cold-start checks when a device is connected. The helper reports `BLOCKED` when no device is available:

```powershell
& ".\Tools\Android\production-sprint-15-cold-starts.ps1"
```

Check that required runtime assets have matching inventory rows. This is coverage evidence, not rights approval:

```powershell
& ".\Tools\validate-asset-provenance.ps1"
```

Run the Unity-specific source boundary check. It complements the generic verifier, which remains `UNVERIFIED` for this Unity-only checkout:

```powershell
& ".\Tools\verify-unity-project.ps1" -TestResultsDirectory ".\Artifacts\TestResults" -OutputPath ".\Artifacts\verify-unity-project.txt"
```

Run the current APK startup gate or collect device performance data when Android hardware is connected. Both scripts keep missing hardware `BLOCKED` and do not grant release approval:

```powershell
& ".\Tools\Android\production-sprint-17-device-gate.ps1"
& ".\Tools\Android\production-sprint-17-performance-capture.ps1" -Launch -DurationSeconds 30
```

Generate or revise Blender assets headlessly:

```powershell
blender --background --python "blender escape from nodnarb\escape_from_nodnarb_captain_blender4.py"
blender --background --python "blender escape from nodnarb\nodnarb_world_props_blender.py"
```

Blender 5.2 LTS is required for the generator scripts. Put `blender` on your PATH or replace the command with its full executable path.

## Public repository boundary

The repository intentionally excludes Unity caches, local builds, screenshots/logs, editor settings, private agent instructions, the design transcript, and reference-only material. Generated runtime assets and their source scripts remain included. Review `LICENSES.md` before using the project commercially; no open-source license has been granted yet.

## Local verification snapshot

- Complete-all4 current local closeout: the non-development release candidate rebuilt successfully as `Builds/Android/EscapeFromNodnarb-release-local.apk` (28,312,591 bytes, SHA-256 `9265D8AE02AE6E8B78809B382B09185E2361F54EA150E4E68E060E133616B593`) and `.aab` (SHA-256 `C93A903586FF31C2223FD2225E8E4C7790B3CBB366723EABAAA2E620CA9C2A22`). The APK audit passes package `com.alphaleverage.escapefromnodnarb`, version `0.1.0`, version code `1`, target SDK `36`, and ARM64. EditMode `65/65` and PlayMode `55/55` pass in `Artifacts/TestResults/editmode-complete-all4.xml` and `Artifacts/TestResults/playmode-complete-all4.xml`; the Unity-specific verifier is `PASS`, provenance is `PASS`, and the generic verifier is `UNVERIFIED` because this Unity checkout has no recognized generic manifest.
- The exact final APK installed with `Success` on `emulator-5556` and reached live N-02 combat with `RUN_START`, `gameplay_ready`, `first_volley`, three shooters, auto-fire, active enemies, and both aligned cards. Captures and logs are under `Artifacts/device-release-local-optimized/`, including `combat-final.png`, `combat-after-card.png`, and `install-final.txt`. Mobile rendering now disables MSAA and caps the existing low-resolution key shadow distance at 18 world units. The emulator performance gate remains open: the 90-second helper and the longer direct sample did not reach terminal budget markers, so no 60 FPS claim is made.
- The physical Galaxy check remains blocked because `R5CX15CV09L` was not connected to ADB. The one-attempt emulator startup matrix passed and classified `emulator-5556` as an emulator. Release readiness is `BLOCKED`: the local APK/AAB and provenance pass, but signing is open, the identity is provisional, replay is blocked, rights/store/privacy/final-art approval is open, iOS is unverified, and nothing was uploaded or published.
- Stage 3 visual slice: `ART_DIRECTION.md` defines the original low-poly presentation contract. `Assets/EscapeFromNodnarb/Runtime/Resources/World/SporeArch.fbx` is generated by the local Blender source and is used only as a visual landmark in N-03 / Unknown March. The exact r2 Android build reached a clean `CONTINUE // SECTOR 03` title state on the emulator; preceding slice captures under `Artifacts/device-production-sprint-18-visual-slice/` show the N-03 story and live combat path.
- Stage 3 visual-slice verification: EditMode `65/65` in `Artifacts/TestResults/editmode-visual-slice-r2.xml`; PlayMode `55/55` in `Artifacts/TestResults/playmode-visual-slice-r2.xml`; Unity asset/provenance verification `PASS` in `Artifacts/verify-unity-project-visual-slice-r2.txt` and `Artifacts/asset-provenance-validation-visual-slice-r2.txt`. Android build/audit evidence is in `Logs/android-build-visual-slice-r2.log` and `Artifacts/android-audit-visual-slice-r2.txt`; the current ARM64 APK is 142,420,530 bytes with SHA-256 `92D933859775F66C35E2D651CB9C66E3858C0B318549D5B315BB65975D26CE7E`.
- Stage 3 device performance remains open: the pre-r2 visual-slice terminal baseline is in `Artifacts/device-production-sprint-18-visual-slice/runtime-terminal-markers.txt` and measured `worst_frame_ms=2133.3`, `frame_p95_ms=516.7`, and `over_budget_frames=681`. This is diagnostic evidence only, not a 60 FPS signoff. The original emulator save was restored after the temporary Stage 3 fixture run.
- The Android/iOS runtime now disables MSAA and caps the existing low-resolution key shadow distance at 18 world units on mobile; this is a targeted rendering-budget optimization, not a device performance signoff.
- Production sprint 18 repair/evidence pass: EditMode `65/65` in `Artifacts/TestResults/editmode-production-sprint-18-repair8.xml` and PlayMode `55/55` in `Artifacts/TestResults/playmode-production-sprint-18-repair8.xml`. The repaired source preserves paused squad rail state and target, finalizes terminal runs before lifecycle save, bounds feedback-pool growth, rejects malformed enemy/card checkpoints, and preserves valid progress when dropping an invalid pending run. The Unity-specific verifier is `PASS` in `Artifacts/verify-unity-project-production-sprint-18-repair8.txt`.
- Production sprint 18 Android/device evidence: `Logs/android-build-production-sprint-18-repair8.log` reports a successful ARM64 APK; `Artifacts/android-audit-production-sprint-18-repair8.txt` records 142,418,830 bytes and SHA-256 `AEA055F095257403134781E7EBBA2C9819228FF0C5D221C024F03B742567C96E`. Exact install passed on `emulator-5556` in `Artifacts/device-production-sprint-18-repair8-install-emulator.txt`; the physical Galaxy `R5CX15CV09L` was unavailable to adb. `Artifacts/device-production-sprint-18-repair8-matrix-run.txt` records emulator startup `PASS` and physical-device `BLOCKED`. The current exact-build title/runtime-ready capture is under `Artifacts/device-production-sprint-18-repair8-runtime/`.
- Production sprint 18 remaining gates: the current 50-second emulator performance capture is `BLOCKED` because no terminal combat markers were reached, so no 60 FPS or dense-combat claim is made. Physical touch/accessibility and lifecycle, ten-stage hardware replay, replay metrics, outside testing, final art/animation/audio approval, rights/store identity, signing, iOS/macOS/Xcode, and store release remain open. Campaign, provenance, replay, release, and generic checks are recorded in the corresponding `Artifacts/*production-sprint-18-repair8.txt` files; the generic verifier remains `UNVERIFIED` for this Unity-only checkout. Historical repair2 Galaxy captures remain retained but are not current repair8 evidence.

- Production sprint 17 local closeout: EditMode `61/61` in `Artifacts/TestResults/editmode-production-sprint-17-r2.xml`, PlayMode `50/50` in `Artifacts/TestResults/playmode-production-sprint-17-r1.xml`, Unity-specific verification `PASS` in `Artifacts/verify-unity-project-production-sprint-17-final.txt`, and Android build/audit/install evidence in `Logs/android-build-production-sprint-17-r1.log`, `Artifacts/android-audit-production-sprint-17-r1.txt`, and `Artifacts/device-production-sprint-17-r1-install-emulator.txt`. The exact APK SHA-256 is `376B4D57FC26CC8AA1858FB9FB41D739BC625ED20EEF722A46D055BEFB652CCB`.
- Production sprint 17 device evidence: `Artifacts/device-production-sprint-17-r11-startup-summary.txt` records three clean emulator startup passes with launch exit `0`, runtime-ready, foreground/resumed ownership, and no fatal markers. `Artifacts/device-production-sprint-17-r9-combat-logcat.txt` records `gameplay_ready` and `first_volley`; `Artifacts/device-production-sprint-17-r10-terminal-logcat.txt` records terminal budget/balance/readability/stability markers. The current terminal run measured `worst_frame_ms=1450.0`, `frame_p95_ms=133.3`, and `over_budget_frames=820`, so no 60 FPS claim is made. Physical Galaxy, thermal endurance, and full ten-stage hardware gates remain open.

- Production sprint 16 current-source pass: Unity 6000.3.21f1 EditMode `57/57` in `Artifacts/TestResults/editmode-production-sprint-16-r3.xml`; PlayMode `50/50` in `Artifacts/TestResults/playmode-production-sprint-16-r3.xml`. Coverage includes the shared gesture policy, action-driven first-run guidance, phase-attributed budget reporting, repeated-run pool trimming, campaign pacing guards, bounded economy values, v2-to-v3 save migration, safe-area normalization, corrupt-backup repair, and stability evidence.
- Production sprint 16 Android evidence: `Logs/android-build-production-sprint-16-r3.log` reports `result=Succeeded errors=0 warnings=1 apkBytes=142395735`; the exact APK SHA-256 is `159E36C5D885981BF7BDAE07BECC0E873EE25A86AC63C46DBAD7B7872297AFC9`. `Artifacts/android-audit-production-sprint-16-r3.txt` confirms package `com.alphaleverage.escapefromnodnarb`, version `0.1.0`, version code `1`, target SDK `36`, and `arm64=true`. The exact APK installed with `Success` on `emulator-5556`; `Artifacts/device-production-sprint-16-r3-cold-start-summary.txt` records three clean cold starts, and `Artifacts/device-production-sprint-16-r3-title.png` is the current title capture. `Tools/validate-asset-provenance.ps1` reports `PASS` for 9 required files and 5 inventory rows while keeping `rights_review=OPEN`.
- Production sprint 16 remaining gates: the emulator path does not close physical Galaxy reliability, thermal/performance signoff, dense-combat readability, or the full ten-stage hardware replay. Outside testing, final art/animation/audio approval, rights/store identity, signing, and iOS/macOS/Xcode remain open. `Artifacts/verify-production-sprint-16.txt` is `UNVERIFIED` because the Unity checkout has no recognized generic manifest.

- Production sprint 15 current-source pass: Unity 6000.3.21f1 EditMode `46/46` in `Artifacts/TestResults/editmode-production-sprint-15-r2.xml`; PlayMode `49/49` in `Artifacts/TestResults/playmode-production-sprint-15-r2.xml`. Coverage includes off-rail card rejection, direct touch card selection with `source=Touch`, pickup/card-choice persistence through background resume, the visible crew/weapon pickup result row, profiler sample seams guarded for development builds, music parameter-write throttling, and bounded haptic pulses.
- Production sprint 15 Android evidence: `Logs/android-build-production-sprint-15-r2.log` reports `result=Succeeded errors=0 warnings=1 apkBytes=142381480`; the exact APK SHA-256 is `14B0C379C27B901FB927E09367BD2BEB80EC64383BBE11F7925DFA7283B74399`. `Artifacts/android-audit-production-sprint-15-r2.txt` confirms package `com.alphaleverage.escapefromnodnarb`, target SDK 36, and arm64. `Artifacts/device-production-sprint-15-emulator-install-r4.txt` records exact-APK install success on `emulator-5556`; `Artifacts/device-production-sprint-15-cold-start-summary-r3.txt` records three cold starts reaching `runtime_ready` with no filtered fatal markers. Current repaired-build title evidence is `Artifacts/device-production-sprint-15-emulator-r2/title-late.png`, with startup `runtime_ready elapsed_ms=3897` in `title-late-logcat.txt`.
- Production sprint 15 remaining gates: the current emulator path does not close physical Galaxy reliability or performance; fresh in-run performance signoff, full ten-stage hardware replay, outside replay metrics, final art/animation/audio approval, rights/store identity, and iOS/macOS/Xcode remain open. `Artifacts/verify-production-sprint-15-final-r2.txt` is `UNVERIFIED` because the Unity checkout has no recognized generic manifest. No current-build combat screenshot or 60 FPS claim is made from this pass.

- Production sprint 14 current-source pass: EditMode `44/44` in `Artifacts/TestResults/editmode-production-sprint-14-r5.xml`; PlayMode `47/47` in `Artifacts/TestResults/playmode-production-sprint-14-r5.xml`. Coverage includes bounded enemy/card reuse, active-and-pooled accessibility propagation through projectile and squad feedback, persisted settings, replay counters, one-shot comeback pacing, all-ten-stage terminal paths, card choice, serialized empty-save recovery, and cold-resume pause restoration.
- Production sprint 14 Android evidence: `Logs/android-build-production-sprint-14-r4.log` reports `result=Succeeded errors=0 warnings=1`; the exact ARM64 APK is 142,365,523 bytes with SHA-256 `B0E4ABD3E14C9FD26F2BA2639FACAAD26DD504F6D04B502D6EBF936434DE1591`. `Artifacts/device-production-sprint-14-emulator-install-r4.txt` records exact-APK install success on `emulator-5556`; package evidence confirms `arm64-v8a`, min SDK 26, target SDK 36, version `0.1.0`. Clean startup reached `runtime_ready elapsed_ms=3079`; background save and cold resume loaded `pending_run=True` with no filtered crash markers, and the resumed run opened behind `RUN PAUSED`.
- Production sprint 14 visual/runtime evidence: `Artifacts/device-production-sprint-14-emulator-title-r4.png`, `device-production-sprint-14-emulator-story-r4.png`, `device-production-sprint-14-emulator-combat-r6.png`, `device-production-sprint-14-emulator-terminal-r4.png`, `device-production-sprint-14-emulator-replay-story-r4.png`, and `device-production-sprint-14-emulator-cold-resume-paused-r4.png` show current title, story, live squad combat with paired cards, terminal failure, replay, and the lifecycle pause screen. Current terminal telemetry records `peak_enemies=7/25 peak_cards=2/2 projectile_slots=16 active_feedback=2 worst_frame_ms=116.7 frame_p95_ms=83.3 over_budget_frames=651`, so pooling and instrumentation are verified but 60 FPS is not.
- Production sprint 14 remaining gates: the Galaxy was unavailable for the current build; full ten-stage hardware replay, user replay metrics, outside testing, final art/animation/audio approval, rights/store identity, and iOS/macOS/Xcode remain open. `Artifacts/verify-production-sprint-14-final.txt` is `UNVERIFIED` because the Unity checkout has no recognized generic manifest.

- Production sprint 13 current-source pass: EditMode `41/41` in `Artifacts/TestResults/editmode-production-sprint-13-r7.xml`; PlayMode `44/44` in `Artifacts/TestResults/playmode-production-sprint-13-r7.xml`. Coverage includes semantic primary/backup recovery, local feedback settings, exact title pool trimming, single audio-listener ownership, capped actor/projectile simulation, card collision alignment, card viewport visibility, terminal budget/balance reporting, and existing lifecycle/progression paths.
- Production sprint 13 Android evidence: `Logs/android-build-production-sprint-13-r3.log` reports `result=Succeeded errors=0 warnings=1`; the exact APK is 142,274,131 bytes with SHA-256 `6958AB20071C216D8ED222398F563DAF06896A8F6476E0F94DCF06AF645C2471`. Install returned `Success` on `emulator-5556`. Clean startup reaches `runtime_ready elapsed_ms=3527` with focused `UnityPlayerGameActivity` and no filtered ANR/FATAL markers in `Artifacts/device-production-sprint-13-emulator-startup-logcat-r3.txt`.
- Production sprint 13 visual evidence: `Artifacts/device-production-sprint-13-emulator-title-r3.png` shows the current title/progression state; `Artifacts/device-production-sprint-13-emulator-endless-story-r3.png` shows the two-panel endless briefing; `Artifacts/device-production-sprint-13-emulator-combat-card-positive-r3.png` shows live combat with both `WEAPON` and `+1 CREW` card labels plus the choice hint. The current combat log records `peak_enemies=18/25 peak_cards=2/2 worst_frame_ms=1433.3 frame_p95_ms=150.0 over_budget_frames=190`, so this is evidence of instrumentation and the remaining performance gate, not a 60 FPS signoff.
- Production sprint 13 remaining gates: the Galaxy was unavailable for the current build; full ten-stage hardware replay, user replay metrics, outside playtesting, final art/animation/audio approval, rights/store identity, and iOS/macOS/Xcode remain open. `Artifacts/verify-production-sprint-13.txt` is `UNVERIFIED` because the Unity checkout has no recognized generic manifest.
- Production sprint 12 post-fix native tests: Unity 6000.3.21f1 EditMode `39/39` in `Artifacts/TestResults/editmode-production-sprint-12-postfix.xml`; PlayMode `42/42` in `Artifacts/TestResults/playmode-production-sprint-12-postfix.xml`. Coverage includes sequential ten-stage progression, final extraction terminal state, endless-mode isolation, backup-save recovery, pending-run resume, manual-pause background save, animation hierarchy, and the local runtime budget seam.
- Current exact post-fix Android debug build: `Logs/android-build-production-sprint-12-postfix.log` reports `result=Succeeded errors=0 warnings=1`; `Builds/Android/EscapeFromNodnarb-debug.apk` is 142,254,092 bytes with SHA-256 `C10135976E212E5753BA9A0388903AE56E59FCDFACFB49E559F3CB43BC00136A`. Install returned `Success` on `emulator-5556`; post-fix install evidence is `Artifacts/device-production-sprint-12-postfix-install-emulator.txt`.
- Current emulator path: the exact post-fix APK reaches `UnityPlayerGameActivity`, `runtime_ready`, and a clean title capture in `Artifacts/device-production-sprint-12-postfix-title.png`; startup evidence is `Artifacts/device-production-sprint-12-postfix-logcat-emulator.txt`. Earlier sprint-12 gameplay/lifecycle captures remain valid for unchanged paths. The terminal log previously observed `worst_frame_ms=333.3 ms`, so this remains diagnostic evidence, not a 60 FPS claim.
- Current Galaxy path: the exact APK reaches `UnityPlayerGameActivity` with a live process and no filtered crash/ANR output, but the bounded window remained in IL2CPP extraction. No Galaxy gameplay pass is claimed; the earlier post-install focus-timeout ANR remains open.
- Required generic verifier: `Artifacts/verify-production-sprint-12-postfix.txt` is `UNVERIFIED` because this Unity checkout has no recognized generic manifest. iOS/macOS/Xcode, broader physical-device coverage, outside testing, final art/rights approval, and store identity remain open.
- Unity 6000.3.21f1 EditMode: 36/36 passed in `Artifacts/TestResults/editmode-painpoints-r4.xml`.
- Unity 6000.3.21f1 PlayMode: 39/39 passed in `Artifacts/TestResults/playmode-painpoints-r7.xml`, including all ten campaign runtime entries, movement feedback, input boundaries, pending-run resume, and a sustained 9-second actor/card cap check.
- Android debug build: `Logs/android-build-painpoints-r3.log` reports `result=Succeeded errors=0 warnings=1`; the ARM64 APK is 142,244,636 bytes with SHA-256 `E892B013FA902A86420C48DB1EEE0B4757F7941871EBCF43D70A28C13D07D5FF`.
- Device smoke run: the exact final APK installed with `Success` on `R5CX15CV09L` / `SM-S928U` and `emulator-5556` / Android 15. Final emulator captures cover title, two-panel story, live combat, and resumed combat in `Artifacts/device-painpoints-r3-emulator-clean-late.png`, `Artifacts/device-painpoints-r3-emulator-story.png`, `Artifacts/device-painpoints-r3-emulator-combat.png`, and `Artifacts/device-painpoints-r3-emulator-resumed-combat.png`. The Galaxy foreground launch is recorded in `Artifacts/device-painpoints-r2-galaxy-foreground-launch.txt`; its capture was obscured by a pre-existing floating app.
- Lifecycle evidence: `Artifacts/device-painpoints-r3-emulator-home-save-logcat.txt` records `pending_run_saved`; the cold relaunch records `progress_loaded pending_run=True` and `resumed=True` in `Artifacts/device-painpoints-r3-emulator-cold-resume-logcat-late.txt`.
- Startup evidence: the emulator's first clean launch took longer for IL2CPP extraction, then reached every staged `NODNARB_STARTUP` checkpoint without ANR/FATAL output. The earlier post-install Galaxy focus-timeout ANR remains a release caveat until separately explained or cleared.
- Required generic verifier: `Artifacts/verify-painpoints-r3.txt` is `UNVERIFIED` because the Unity checkout has no recognized generic manifest. Unity-native tests, Android packaging, ADB install, foreground checks, and captures are the applicable local evidence.
- iOS/macOS/Xcode: not verified. Broader physical-device coverage, full ten-stage replay, outside testing, final art approval, rights review, and store identity remain open.

This repository does not include the local APK, screenshots, logs, or generic verifier output. The generic verifier does not recognize Unity-only projects, so Unity-native tests and Android packaging are the applicable checks. Broader device/campaign coverage, final art approval, outside testing, iOS/Xcode, and rights review remain open.

No ads, analytics, purchases, telemetry, accounts, or remote services are enabled.
