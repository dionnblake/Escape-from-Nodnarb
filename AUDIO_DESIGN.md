# Escape from Nodnarb audio design

Audio target for the original mobile prototype. All included sound is generated locally until Blake selects or commissions cleared production assets.

## Sound effects

- Squad weapons: pulse carbine, rail slug, arc repeater, muzzle flash, projectile travel.
- Creature contact: rusher hit, spitter hit, blocker armor, carrier core, defeat burst.
- Player feedback: `+1 CREW`, weapon upgrade, Overdrive, damage, boss telegraph, extraction, defeat.
- Wilderness: wind gusts, crystal resonance, spore pulse, hive heartbeat, snow strain, loose rock.
- UI: title signal, story deploy, pause, result, map movement, loadout change.

## Music layers

- `title`: low alien wind and distress-signal pulse.
- `combat`: restrained horde pulse; intensity follows difficulty.
- `boss`: heavier pressure layer during the boss window.
- `extraction`: rising signal layer during the final eight seconds.

The current runtime uses short generated clips in `ProceduralAudio.cs`. Replace individual clips with original or commercially licensed assets only after recording provenance in `LICENSES.md`. Keep the public API platform-neutral for Android and iOS.

## Mix rules

- Gameplay effects stay louder than music.
- Boss telegraphs and damage must cut through every layer.
- No continuous voice acting in v1; use still panels and radio text first.
- Keep sound optional and safe to disable from a future settings screen.
