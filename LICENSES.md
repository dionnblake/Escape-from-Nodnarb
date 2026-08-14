# Asset and license inventory

## Included

| Item | Source | Status |
|---|---|---|
| Game code and campaign text | Original project work | Included |
| Procedural terrain, cards, projectiles, effects, and fallback meshes | Generated at runtime from Unity primitives and original code | Included |
| Card-choice silhouettes and platform-neutral haptic bridge | Original project code; platform calls are limited to built-in Android/iOS handheld feedback when compiled for those targets | Included; no third-party SDK or asset |
| Captain FBX, color atlas, and emission mask | Project-generated Blender handoff supplied for Escape from Nodnarb | Prototype inclusion; confirm commercial rights before release |
| Rusher, Spitter, Blocker, Carrier, and CrewSoldier FBX assets | Generated locally by `blender escape from nodnarb/generate_enemy_assets.py` with Blender CLI | Project-generated; retain generator/source with the build |
| CrashedEngine, SnowArch, RelayBeacon, CanyonDebris, CrystalCluster, HiveGrowth, SporeArch, RuinGate, HiveObelisk, and ExtractionBeacon FBX assets | Generated locally by `blender escape from nodnarb/nodnarb_world_props_blender.py` with Blender 5.2 CLI; current generator version `world-landmarks-v3` | Project-generated original geometry; retain generator/source and approve for commercial release |
| Crash Basin authored low-poly FBX kit: 3 rocks, 3 cliffs, 2 spires, 2 ground chunks, 2 wreck pieces, 1 rock arch, and 2 alien flora accents | Generated locally by `blender escape from nodnarb/nodnarb_world_props_blender.py -- --only-crash-basin-kit`; custom faceted meshes, Blender 5.2 CLI | Project-generated original geometry; retain generator/source and approve for commercial release |
| Imported enemy materials | Recolored at runtime through `GameTheme` and the project shader | Included; no external textures |
| `Runtime/Resources/Art/title-screen.png` | User-supplied original Escape from Nodnarb title artwork | Prototype inclusion; confirm generation source and commercial rights before store release |
| UI layout and visual tokens | Original project work using the project's token sheet | Included |
| Sound effects | Original procedural tones generated at runtime | Included |
| Runtime UI fallback font | Unity `LegacyRuntime.ttf` built-in resource | Placeholder; distributed under Unity runtime terms |
| Escape from Nodnarb visual bible | User-provided reference image kept outside the public repository | Reference-only; not loaded by the runtime or included as production art |

## Explicitly excluded

- The supplied Hero Wars advertisement/reference video
- Hero Wars names, logos, characters, environments, UI, audio, code, or extracted assets
- Third-party game art, music, fonts, ad SDKs, analytics SDKs, or purchase SDKs

Before a store release, review Unity redistribution terms for the selected editor version and replace the fallback font with an original or commercially licensed display/body pair if the visual pass requires it.

## Audit note (2026-08-11)

Cross-checked this table against every file under `Assets/EscapeFromNodnarb/Runtime/Resources/` (Art, Captain, Enemies, World). No undocumented asset found: every FBX, texture, material, prefab, and shader currently in that folder traces back to a row above. `CaptainVisual.prefab` and `M_Captain_Atlas.mat` aren't named individually, but both are Unity project files built entirely from the already-logged Captain FBX and textures, not new sourced material, so they don't need a separate row.

Open items confirmed still open, unchanged by this audit: Captain FBX commercial rights, `title-screen.png` generation source and commercial rights, and the fallback font replacement. All three remain store-release gates per `PROJECT_SPEC.md` and `HANDOFF.md`.
