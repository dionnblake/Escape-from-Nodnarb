# Escape from Nodnarb - Captain 3D Modeling Handoff

## What this package is

This package is a Blender 4.x procedural blockout, rig, animation, material, and export scaffold for the Captain. It is not a finished production FBX or GLB because this environment does not have Blender or a 3D DCC/export runtime installed.

The visual target comes from the supplied reference direction: stylized low-poly 3D, hard flat-shaded geometry, strong silhouettes, matte materials, minimal surface detail, and shapes practical for optimized real-time mobile assets. The Captain is an adult crash survivor in a fitted survival suit with a compact chest rig, utility belt, reinforced boots, forearm protection, and practical equipment.

## Final production target

- Height: 1.80 m, sole to crown
- Root/pivot: world origin at feet, Z = 0
- Rig: Unity Humanoid-compatible
- Triangle budget: 2,500 to 3,500 triangles
- Material count: 1 shared material
- Base texture: 1024 x 1024 maximum
- Emission mask: 1024 x 1024 maximum
- Armor orange: #C66A32
- Charcoal undersuit: #2B2F34
- Utility off-white: #F2F5ED
- Rescue green: #84CC16
- Surface response: matte, low specular, no micro-detail dependence

## Blender package contents

- escape_from_nodnarb_captain_blender4.py
  - Builds a low-poly Captain blockout
  - Creates one shared atlas material
  - Builds a Unity-style humanoid bone hierarchy
  - Adds Idle, Walk, AutoFire, HitReaction, and Defeat actions
  - Creates a MuzzlePoint transform
  - Exports Captain_Unity.glb and Captain_Unity.fbx when run in Blender
- captain_color_atlas.png
  - 1024 x 1024 base-color atlas
- captain_emission_mask.png
  - 1024 x 1024 grayscale mask, rescue-green cell is emissive

## Required artist pass after running the script

The procedural mesh is a blockout. Before calling it final:

1. Refine the face into the approved original Captain likeness without copying any existing game character.
2. Reshape armor to match the approved turnaround, especially chest plate, shoulder blocks, forearm armor, belt, thigh armor, shin armor, and boots.
3. Preserve the mobile silhouette. Do not add tiny greebles that disappear in portrait gameplay.
4. Retopologize or simplify until the final evaluated mesh lands between 2,500 and 3,500 triangles.
5. Check elbow, shoulder, hip, knee, and ankle deformation.
6. Keep the chest module, belt, armor plates, and weapon readable from the game camera.
7. Keep one material and atlas all color zones.
8. Keep the weapon original. Use the right-hand mount and MuzzlePoint as the gameplay attachment reference.

## Unity import settings

### FBX

- Scale Factor: 1
- Convert Units: enabled
- Bake Axis Conversion: disabled unless your pipeline requires it
- Rig Animation Type: Humanoid
- Avatar Definition: Create From This Model
- Optimize Game Objects: enabled after attachment bones are verified
- Preserve Hierarchy: enabled if your weapon/VFX transform discovery depends on names
- Import BlendShapes: disabled unless a later facial pass adds them
- Read/Write: disabled
- Mesh Compression: Medium
- Generate Colliders: disabled

### GLB

Use GLB for quick validation or pipelines with a glTF importer. For a conventional Unity Humanoid workflow, FBX is usually the cleaner handoff.

## Humanoid bone map

- Root
- Hips
- Spine
- Chest
- Neck
- Head
- LeftUpperArm
- LeftLowerArm
- LeftHand
- RightUpperArm
- RightLowerArm
- RightHand
- LeftUpperLeg
- LeftLowerLeg
- LeftFoot
- RightUpperLeg
- RightLowerLeg
- RightFoot

In Unity Avatar Configuration, verify all required bones map correctly. Save the mapping only after the T-pose passes.

## Animation clips

Recommended clip setup at 30 fps:

| Clip | Frames | Loop |
|---|---:|---|
| Idle | 1-60 | Yes |
| Walk | 1-30 | Yes |
| AutoFire | 1-24 | Optional, usually loop while firing |
| HitReaction | 1-18 | No |
| Defeat | 1-45 | No |

The script creates rough blocking motions. Final animation should remove foot sliding, improve shoulder aiming, and match the final weapon grip.

## Material setup in Unity URP

Create one material named `M_Captain_Atlas`.

Recommended URP Lit settings:

- Base Map: captain_color_atlas.png
- Metallic: 0.02 to 0.05
- Smoothness: 0.12 to 0.20
- Normal Map: none unless later art review proves it is necessary
- Emission: enabled
- Emission Mask: captain_emission_mask.png
- Emission Color: #84CC16, HDR intensity about 1.5 to 2.5
- Alpha Clipping: off
- Double Sided: off
- Cast Shadows: on
- Receive Shadows: on

For a lower-end device tier, use a single custom mobile shader that samples the same atlas and emission mask.

## Exact prefab setup

Suggested prefab hierarchy:

```text
Captain_Prefab
  VisualRoot
    Captain_Armature
      Root
        Hips
        ...
      MuzzlePoint
    Captain mesh parts
  GameplayRoot
    CharacterController
    Hurtbox
    GroundCheck
    WeaponSocket
```

Transform rules:

- Captain_Prefab position: 0, 0, 0
- Rotation: 0, 0, 0
- Scale: 1, 1, 1
- Feet rest on Y = 0 in Unity
- Total standing height: approximately 1.80 m

## Collision guidance

For a mobile third-person or lane character, do not use mesh collision.

Recommended CharacterController:

- Height: 1.72 m
- Radius: 0.28 m
- Center: X 0, Y 0.86, Z 0
- Step Offset: 0.20 m
- Skin Width: 0.03 to 0.05 m
- Min Move Distance: 0

If using a Rigidbody character:

- One capsule collider for body
- Optional small sphere or capsule hurtbox for head only if gameplay needs head-specific hits
- No per-limb colliders unless gameplay requires them

## Optimization guidance

- Keep 2,500 to 3,500 final triangles at LOD0
- LOD1: 55 to 65 percent of LOD0
- LOD2: 25 to 35 percent of LOD0
- Shadow-caster LOD can be simpler than visible LOD
- One shared material
- One 1024 atlas
- Avoid alpha transparency on the character
- Avoid micro decals and small bevels that do not read on mobile
- Keep rescue-green emissive areas small to limit bloom and overdraw
- Prefer rigid armor chunks over high-cost deformation where visually acceptable

## QA checklist

Before approval:

- Height is 1.80 m in Blender and Unity
- Root is at feet
- No negative scale
- All transforms applied
- Humanoid Avatar validates
- No missing atlas texture
- One material only
- Triangle count is within 2,500 to 3,500
- Idle and Walk loop cleanly
- AutoFire does not detach the weapon
- HitReaction returns to neutral
- Defeat does not intersect the ground excessively
- MuzzlePoint faces weapon-forward axis
- Silhouette reads at the target portrait-game camera distance
- No copied armor, face, weapon, logos, or franchise-specific design language
