# Captain/Crew Movement Feedback: Animation Brief

Confirmed root cause before this pass, read directly from `Assets/EscapeFromNodnarb/Runtime/Gameplay/CaptainSquad.cs`: the Captain and crew imported FBX prefabs, while `TickInput()` only moved the squad root and `Recoil()` only added a fire squash. Dragging the squad moved a rigid prop; nothing about the character reacted to movement itself. Blender inspection of the packaged files now confirms the precise asset boundary: `Captain_Unity.fbx` contains `Captain_Armature` plus separate armor meshes, while `CrewSoldier.fbx` has separate `Thigh`, `Thigh.001`, `UpperArm`, and `UpperArm.001` meshes but no armature. Tier 1 root response remains for the Captain; crew now also gets a restrained per-part stride response using those authored child transforms. A full Captain skeletal walk cycle remains a separate art/rig pass.

This is scoped as movement feedback specifically, not the full production art pass. `PROJECT_SPEC.md` still gates full asset replacement behind the replay-validation tests; this brief doesn't ask to jump that gate. It targets one narrow, currently-missing behavior: characters should visibly react to horizontal movement.

## Tier 1: procedural whole-body motion (implemented)

No new Blender assets, animation clips, or Animator Controller. Pure code in the existing per-frame update in `CaptainSquad.TickInput()`.

- Track horizontal velocity: `float velocityX = (position.x - previousPositionX) / deltaTime` right where `position.x` is already being lerped (line 166).
- Apply to each unit transform (captain and every entry in `soldiers`):
  - **Lean**: small Z-axis tilt proportional to velocity, clamped (e.g. `Quaternion.Euler(0, 0, -velocityX * leanFactor)`), springs back to zero when velocity is near zero. Reads as "leaning into the turn."
  - **Bob**: a sine-wave vertical offset on the unit's local Y position, frequency tied to `Mathf.Abs(velocityX)` so it only bobs while moving, amplitude small (a few centimeters). Reads as footsteps without needing actual feet to move.
- **Alternating micro-squash**: reuse the squash pattern already in `Recoil()` (it already does `localScale.z *= 0.93f` type edits), but drive a subtle alternating left/right weight-shift squash timed to the bob cycle instead of only firing on shot.

Implemented result: `CaptainSquad` stores each unit's visual baseline, derives horizontal velocity from the settled root position, then applies non-accumulating lean, movement bob, and alternating weight shift. The PlayMode regression reports Captain lean `2.346063` degrees, crew lean `2.346063` degrees, and bob offsets `0.009315248` / `0.01007825` on the Unity test path.
- This is genuinely a few hours of code, touches only `CaptainSquad.cs`, and directly answers "why does nothing move when I drag them." It won't look like a real walk cycle, but it stops reading as inert.

## Tier 2: real skeletal walk cycle (the actual production-quality fix)

Bigger scope, needs both a Blender step and a Unity step. Only start this once Tier 1 is in and the team wants to spend real time on it, ideally paired with the Phase 3 art pass rather than squeezed in ahead of it.

1. **Check the FBX hierarchy first.** Open `Captain_Unity.fbx` and `CrewSoldier.fbx` and confirm whether legs/arms exist as separate child transforms under the mesh (common for Blender-generated low-poly rigs) or are baked into one static mesh. This determines the actual scope: separate limb transforms can get a cheap 2 to 3 pose walk cycle without a full skeleton; a single fused mesh needs a real armature added in Blender first.
2. **Add or confirm an armature** in the two generator scripts (`blender escape from nodnarb/escape_from_nodnarb_captain_blender4.py`, `blender escape from nodnarb/generate_enemy_assets.py`) with a minimal bone set: hips, two upper legs, two lower legs is enough for a readable low-poly walk, no arms/spine needed for a mobile game at this camera distance.
3. **Author two animation clips**: `Idle` (subtle breathing/sway, loops when velocity is near zero) and `Walk` (loops when moving), exported with the FBX.
4. **Wire a Unity `Animator Controller`** with a single float parameter (e.g. `Speed`) driving a blend between `Idle` and `Walk`, set from the same `velocityX` calculation described in Tier 1. `CaptainSquad.BuildImportedCaptain()` / `BuildImportedSoldier()` (lines 361 to 413) are where the `Animator` component would get added and the controller assigned after instantiation.
5. Apply the same rig/clips to both Captain and CrewSoldier prefabs so movement feedback stays visually consistent across the whole squad, matching the existing "Captain nearest the incoming horde, crew trailing toward the camera" rule already documented in `Assets/EscapeFromNodnarb/AGENTS.md`.

## Suggested priority

The current split is deliberate. Captain root lean/bob and crew root lean/bob are the safe cross-platform Tier 1 response. Crew limb stride is a bounded Tier 1.5 response because the generated asset exposes separate child meshes. A full Captain skeletal walk cycle needs rig-pose review and belongs with the broader art pass; do not claim that the current code replaces authored animation.

## Formation Z-order: Captain should lead from the front

Separate, smaller fix, same file. Blake wants the Captain positioned closest to the incoming enemies, with the squad trailing behind him toward the camera, the classic "leader charges ahead" read.

**Before this pass**: `GameTheme.CaptainZ = -2.4`, `GameTheme.FrontLineZ = -0.35` (the breach threshold enemies approach from `SpawnZ = 21`). The Captain was at `-1.95`, while the first crew row was at `-1.05`, so the crew sat closer to the incoming horde than the Captain.

**Implemented**: `CaptainSquad.Build()` now places the Captain at `-1.05`; `FormationPosition()` places the first crew row at `-1.85` and subtracts each later row's spacing toward the camera. The PlayMode regression confirms the Captain leads the crew and all three focal units remain inside the portrait action band.

**Checked, not a concern**: breach detection (`CombatActors.cs` line 306, `position.z <= GameTheme.FrontLineZ`) is a fixed world-space threshold check, not tied to squad or Captain position, and ranged enemies already always target `CaptainPosition` specifically (`CombatActors.cs` line 302), not "nearest visible unit." So this is confirmed to be a pure formation/visual change; it doesn't alter breach timing, ranged targeting, or difficulty. Worth a playtest anyway since it does change the read of danger, the Captain will now visually be the thing melee rushers reach first, which may feel more tense/heroic or may feel like the Captain is unfairly exposed; that's a feel judgment call, not a bug to fix.
