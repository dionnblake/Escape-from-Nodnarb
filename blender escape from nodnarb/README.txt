Escape from Nodnarb Captain handoff package

1. Put the Python script and both PNG textures in the same folder.
2. Open Blender 4.x.
3. Open the Scripting workspace.
4. Load escape_from_nodnarb_captain_blender4.py.
5. Run Script.
6. The script builds the blockout, rig, actions, material, and exports GLB/FBX into the same folder.
7. Perform the required artist polish and QA described in CAPTAIN_UNITY_HANDOFF.md before treating it as final.

The world generator also supports the focused Stage 3 landmark export:

blender --background --python nodnarb_world_props_blender.py -- --only-spore-arch

This writes `Assets/EscapeFromNodnarb/Runtime/Resources/World/SporeArch.fbx` for the original N-03 / Unknown March visual slice.
