
# Escape from Nodnarb - Captain procedural blockout, rig, animation and export scaffold
# Blender 4.x
#
# IMPORTANT:
# This generates a production-oriented LOW-POLY BLOCKOUT and Unity rig/animation scaffold.
# It is not a substitute for a final artist pass, retopology review, deformation testing,
# facial polish, or final weapon polish.
#
# Run from Blender 4.x Scripting workspace. Keep captain_color_atlas.png in the same folder.
# The script will build the scene and, if AUTO_EXPORT is True, export GLB and FBX beside it.

import bpy
import math
from mathutils import Vector
from pathlib import Path

# -----------------------------
# CONFIG
# -----------------------------
CAPTAIN_HEIGHT_M = 1.80
FPS = 30
AUTO_EXPORT = True
EXPORT_GLB = True
EXPORT_FBX = True
APPLY_BEVELS = True

# One shared material, one base-color atlas.
ATLAS_FILENAME = "captain_color_atlas.png"

# Atlas cells in a 4 x 4 grid, bottom-left UV convention.
ATLAS = {
    "orange":    (0, 3),
    "charcoal":  (1, 3),
    "offwhite":  (2, 3),
    "green":     (3, 3),
    "skin":      (0, 2),
    "hair":      (1, 2),
    "metal":     (2, 2),
    "strap":     (3, 2),
    "darkmetal": (0, 1),
    "black":     (1, 1),
    "sole":      (2, 1),
    "neutral":   (3, 1),
}

# -----------------------------
# BASIC SCENE
# -----------------------------
def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        pass

def set_units():
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.unit_settings.system = 'METRIC'
    sc.unit_settings.scale_length = 1.0

def script_dir():
    # When launched from Blender's Text Editor, use the opened text file path.
    text = getattr(getattr(bpy.context, "space_data", None), "text", None)
    text_path = getattr(text, "filepath", "") if text else ""
    if text_path:
        return Path(bpy.path.abspath(text_path)).resolve().parent

    # Background/command-line execution provides __file__ instead.
    script_path = globals().get("__file__")
    if script_path:
        return Path(script_path).resolve().parent

    blend_path = Path(bpy.data.filepath) if bpy.data.filepath else None
    if blend_path:
        return blend_path.parent

    # Never fall back to the drive root. Use a writable, predictable folder.
    fallback = Path.home() / "Documents" / "EscapeFromNodnarb" / "CaptainHandoff"
    fallback.mkdir(parents=True, exist_ok=True)
    return fallback

# -----------------------------
# SHARED ATLAS MATERIAL
# -----------------------------
def make_material():
    mat = bpy.data.materials.new("M_Captain_Atlas")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexImage")

    atlas_path = script_dir() / ATLAS_FILENAME
    if atlas_path.exists():
        tex.image = bpy.data.images.load(str(atlas_path), check_existing=True)
        tex.interpolation = 'Closest'
    else:
        print("WARNING: texture atlas not found:", atlas_path)

    bsdf.inputs["Roughness"].default_value = 0.72
    bsdf.inputs["Metallic"].default_value = 0.03

    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat

def atlas_uv(obj, key):
    if obj.type != 'MESH':
        return
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    cx, cy = ATLAS[key]
    cols = rows = 4
    # center of cell, with bottom-left UV origin
    u = (cx + 0.5) / cols
    v = (cy + 0.5) / rows
    for loop in uv_layer:
        loop.uv = (u, v)

def add_mat(obj, mat, key):
    if obj.type == 'MESH':
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        atlas_uv(obj, key)

# -----------------------------
# LOW-POLY PRIMITIVES
# -----------------------------
def apply_bevel(obj, width=0.015):
    if not APPLY_BEVELS or obj.type != 'MESH':
        return
    mod = obj.modifiers.new("Bevel", 'BEVEL')
    mod.width = width
    mod.segments = 1
    mod.affect = 'EDGES'
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)

def cube(name, loc, scale, mat, key, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    apply_bevel(o, bevel)
    add_mat(o, mat, key)
    return o

def cyl(name, loc, radius, depth, mat, key, rot=(0,0,0), vertices=8, bevel=0.008):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    apply_bevel(o, bevel)
    add_mat(o, mat, key)
    return o

def sphere(name, loc, scale, mat, key, segments=8, rings=4):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    add_mat(o, mat, key)
    return o

# -----------------------------
# ARMATURE
# -----------------------------
def add_bone(arm, name, head, tail, parent=None):
    b = arm.edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if parent:
        b.parent = arm.edit_bones[parent]
        b.use_connect = False
    return b

def make_armature():
    arm_data = bpy.data.armatures.new("Captain_Armature")
    arm_obj = bpy.data.objects.new("Captain_Armature", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    arm_obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')

    add_bone(arm_data, "Root", (0,0,0.00), (0,0,0.10))
    add_bone(arm_data, "Hips", (0,0,0.88), (0,0,1.02), "Root")
    add_bone(arm_data, "Spine", (0,0,1.02), (0,0,1.20), "Hips")
    add_bone(arm_data, "Chest", (0,0,1.20), (0,0,1.42), "Spine")
    add_bone(arm_data, "Neck", (0,0,1.42), (0,0,1.55), "Chest")
    add_bone(arm_data, "Head", (0,0,1.55), (0,0,1.80), "Neck")

    # Arms, mild A-pose.
    add_bone(arm_data, "LeftUpperArm",  (0.18,0,1.39), (0.47,0,1.31), "Chest")
    add_bone(arm_data, "LeftLowerArm",  (0.47,0,1.31), (0.69,0,1.17), "LeftUpperArm")
    add_bone(arm_data, "LeftHand",      (0.69,0,1.17), (0.80,0,1.13), "LeftLowerArm")
    add_bone(arm_data, "RightUpperArm", (-0.18,0,1.39), (-0.47,0,1.31), "Chest")
    add_bone(arm_data, "RightLowerArm", (-0.47,0,1.31), (-0.69,0,1.17), "RightUpperArm")
    add_bone(arm_data, "RightHand",     (-0.69,0,1.17), (-0.80,0,1.13), "RightLowerArm")

    # Legs.
    add_bone(arm_data, "LeftUpperLeg",  (0.12,0,0.93), (0.13,0,0.56), "Hips")
    add_bone(arm_data, "LeftLowerLeg",  (0.13,0,0.56), (0.13,0,0.15), "LeftUpperLeg")
    add_bone(arm_data, "LeftFoot",      (0.13,0,0.15), (0.13,-0.18,0.06), "LeftLowerLeg")
    add_bone(arm_data, "RightUpperLeg", (-0.12,0,0.93), (-0.13,0,0.56), "Hips")
    add_bone(arm_data, "RightLowerLeg", (-0.13,0,0.56), (-0.13,0,0.15), "RightUpperLeg")
    add_bone(arm_data, "RightFoot",     (-0.13,0,0.15), (-0.13,-0.18,0.06), "RightLowerLeg")

    bpy.ops.object.mode_set(mode='OBJECT')
    arm_obj.show_in_front = True
    return arm_obj

def bone_parent(obj, arm_obj, bone_name):
    world_matrix = obj.matrix_world.copy()
    obj.parent = arm_obj
    obj.parent_type = 'BONE'
    obj.parent_bone = bone_name
    # Bone parenting adds the target bone's transform. Restore the original
    # world matrix after parenting so rigid armor does not explode outward.
    obj.matrix_world = world_matrix

# -----------------------------
# CHARACTER PARTS
# -----------------------------
def build_character(mat, arm):
    parts = []

    # Undersuit core.
    parts += [
        cube("Undersuit_Pelvis", (0,0,0.92), (0.18,0.12,0.13), mat, "charcoal", 0.02),
        cube("Undersuit_Abdomen", (0,0,1.10), (0.19,0.115,0.13), mat, "charcoal", 0.02),
        cube("Undersuit_Chest", (0,0,1.30), (0.23,0.13,0.16), mat, "charcoal", 0.025),
    ]
    bone_parent(parts[0], arm, "Hips")
    bone_parent(parts[1], arm, "Spine")
    bone_parent(parts[2], arm, "Chest")

    # Head and hair.
    head = sphere("Head_Mesh", (0,-0.005,1.66), (0.105,0.09,0.13), mat, "skin", 8, 5)
    hair = cube("Hair_Block", (0,0.005,1.765), (0.11,0.085,0.045), mat, "hair", 0.01)
    bone_parent(head, arm, "Head")
    bone_parent(hair, arm, "Head")
    parts += [head,hair]

    # Neck seal.
    neck = cyl("Neck_Seal", (0,0,1.51), 0.095, 0.08, mat, "charcoal", vertices=8)
    bone_parent(neck, arm, "Neck")
    parts.append(neck)

    # Chest armor, practical crash-survivor profile.
    chest_plate = cube("Armor_Chest", (0,-0.095,1.325), (0.22,0.035,0.13), mat, "orange", 0.018)
    abdomen_plate = cube("Armor_Abdomen", (0,-0.09,1.105), (0.17,0.03,0.09), mat, "orange", 0.012)
    back_plate = cube("Armor_Back", (0,0.115,1.30), (0.205,0.025,0.12), mat, "orange", 0.012)
    bone_parent(chest_plate, arm, "Chest")
    bone_parent(abdomen_plate, arm, "Spine")
    bone_parent(back_plate, arm, "Chest")
    parts += [chest_plate, abdomen_plate, back_plate]

    # Chest module and rescue light.
    module = cube("Chest_Module", (0,-0.145,1.26), (0.085,0.025,0.055), mat, "darkmetal", 0.008)
    light = cube("Chest_Light", (0,-0.172,1.27), (0.045,0.006,0.017), mat, "green", 0.003)
    bone_parent(module, arm, "Chest")
    bone_parent(light, arm, "Chest")
    parts += [module, light]

    # Utility belt.
    belt = cube("Utility_Belt", (0,-0.005,0.995), (0.205,0.14,0.035), mat, "strap", 0.008)
    bone_parent(belt, arm, "Hips")
    parts.append(belt)
    for i,x in enumerate((-0.17,-0.085,0.085,0.17)):
        p = cube(f"Pouch_{i}", (x,-0.15,0.99), (0.035,0.025,0.05), mat, "strap", 0.005)
        bone_parent(p, arm, "Hips")
        parts.append(p)

    # Arms.
    for side, sx in (("Left",1),("Right",-1)):
        ua = cyl(f"{side}_UpperArm_Undersuit", (sx*0.34,0,1.35), 0.065, 0.30, mat, "charcoal",
                 rot=(0, math.radians(76),0), vertices=8)
        la = cyl(f"{side}_LowerArm_Undersuit", (sx*0.58,0,1.23), 0.055, 0.27, mat, "charcoal",
                 rot=(0, math.radians(58),0), vertices=8)
        sh = cube(f"{side}_Shoulder_Armor", (sx*0.25,-0.03,1.40), (0.075,0.09,0.075), mat, "orange", 0.015)
        fa = cube(f"{side}_Forearm_Armor", (sx*0.60,-0.02,1.22), (0.075,0.065,0.11), mat, "orange", 0.012)
        hand = cube(f"{side}_Hand_Glove", (sx*0.755,0,1.14), (0.055,0.045,0.05), mat, "charcoal", 0.008)
        bone_parent(ua, arm, f"{side}UpperArm")
        bone_parent(la, arm, f"{side}LowerArm")
        bone_parent(sh, arm, f"{side}UpperArm")
        bone_parent(fa, arm, f"{side}LowerArm")
        bone_parent(hand, arm, f"{side}Hand")
        parts += [ua,la,sh,fa,hand]

    # Legs and boots.
    for side, sx in (("Left",1),("Right",-1)):
        ut = cyl(f"{side}_UpperLeg_Undersuit", (sx*0.13,0,0.74), 0.075, 0.36, mat, "charcoal", vertices=8)
        lt = cyl(f"{side}_LowerLeg_Undersuit", (sx*0.13,0,0.36), 0.065, 0.38, mat, "charcoal", vertices=8)
        thigh = cube(f"{side}_Thigh_Armor", (sx*0.13,-0.055,0.73), (0.08,0.07,0.13), mat, "orange", 0.012)
        shin = cube(f"{side}_Shin_Armor", (sx*0.13,-0.06,0.35), (0.075,0.065,0.14), mat, "orange", 0.012)
        knee = cube(f"{side}_Knee_Armor", (sx*0.13,-0.10,0.54), (0.07,0.035,0.06), mat, "offwhite", 0.01)
        boot = cube(f"{side}_Boot", (sx*0.13,-0.055,0.105), (0.09,0.14,0.075), mat, "sole", 0.015)
        bone_parent(ut, arm, f"{side}UpperLeg")
        bone_parent(lt, arm, f"{side}LowerLeg")
        bone_parent(thigh, arm, f"{side}UpperLeg")
        bone_parent(shin, arm, f"{side}LowerLeg")
        bone_parent(knee, arm, f"{side}LowerLeg")
        bone_parent(boot, arm, f"{side}Foot")
        parts += [ut,lt,thigh,shin,knee,boot]

    # Original compact automatic sidearm, mounted to right hand.
    gun_body = cube("CaptainWeapon_Body", (-0.82,-0.02,1.10), (0.10,0.035,0.045), mat, "darkmetal", 0.008)
    gun_grip = cube("CaptainWeapon_Grip", (-0.79,0.00,1.05), (0.025,0.03,0.07), mat, "charcoal", 0.006)
    gun_muzzle = cyl("CaptainWeapon_Muzzle", (-0.94,-0.02,1.10), 0.028, 0.07, mat, "metal",
                     rot=(0, math.radians(90),0), vertices=8)
    gun_light = cube("CaptainWeapon_Light", (-0.86,-0.055,1.10), (0.025,0.005,0.01), mat, "green", 0.002)
    for o in (gun_body,gun_grip,gun_muzzle,gun_light):
        bone_parent(o, arm, "RightHand")
    parts += [gun_body,gun_grip,gun_muzzle,gun_light]

    # Muzzle transform for Unity VFX placement.
    empty = bpy.data.objects.new("MuzzlePoint", None)
    empty.empty_display_type = 'PLAIN_AXES'
    empty.empty_display_size = 0.04
    empty.location = (-0.98,-0.02,1.10)
    bpy.context.collection.objects.link(empty)
    bone_parent(empty, arm, "RightHand")

    return parts

# -----------------------------
# ANIMATION
# -----------------------------
def ensure_action(arm, name):
    action = bpy.data.actions.new(name)
    arm.animation_data_create()
    arm.animation_data.action = action
    return action

def set_euler(pb, xyz):
    pb.rotation_mode = 'XYZ'
    pb.rotation_euler = xyz

def krot(arm, bone, frame, xyz):
    pb = arm.pose.bones[bone]
    set_euler(pb, xyz)
    pb.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone)

def kloc(arm, bone, frame, xyz):
    pb = arm.pose.bones[bone]
    pb.location = xyz
    pb.keyframe_insert(data_path="location", frame=frame, group=bone)

def action_to_nla(arm, action):
    arm.animation_data_create()
    track = arm.animation_data.nla_tracks.new()
    track.name = action.name
    # Blender 4.4+ no longer exposes the legacy Action curve collection directly.
    # frame_range works across the Blender 4.x action APIs.
    start = int(action.frame_range[0]) or 1
    strip = track.strips.new(action.name, int(start), action)
    strip.action_frame_start = action.frame_range[0]
    strip.action_frame_end = action.frame_range[1]
    strip.mute = False

def anim_idle(arm):
    act = ensure_action(arm, "Idle")
    for f, chest, arms in [(1,0.0,0.03),(30,0.015,-0.015),(60,0.0,0.03)]:
        krot(arm,"Chest",f,(chest,0,0))
        krot(arm,"LeftUpperArm",f,(0,0,arms))
        krot(arm,"RightUpperArm",f,(0,0,-arms))
    act.frame_start, act.frame_end = 1, 60
    action_to_nla(arm, act)

def anim_walk(arm):
    act = ensure_action(arm, "Walk")
    for f, s in [(1,1),(8,0),(15,-1),(23,0),(30,1)]:
        krot(arm,"LeftUpperLeg",f,(math.radians(24)*s,0,0))
        krot(arm,"RightUpperLeg",f,(-math.radians(24)*s,0,0))
        krot(arm,"LeftLowerLeg",f,(math.radians(12)*(1-s),0,0))
        krot(arm,"RightLowerLeg",f,(math.radians(12)*(1+s),0,0))
        krot(arm,"LeftUpperArm",f,(-math.radians(14)*s,0,0))
        krot(arm,"RightUpperArm",f,(math.radians(14)*s,0,0))
        kloc(arm,"Hips",f,(0,0,0.012 if f in (8,23) else 0))
    act.frame_start, act.frame_end = 1, 30
    action_to_nla(arm, act)

def anim_attack(arm):
    act = ensure_action(arm, "AutoFire")
    # Raise weapon arm and brace with off hand. Three short recoil pulses.
    for f, lean in [(1,0),(6,-0.10),(10,-0.18),(14,-0.20),(16,-0.16),(18,-0.20),(20,-0.16),(24,0)]:
        krot(arm,"Chest",f,(0,0,math.radians(lean*8)))
        krot(arm,"RightUpperArm",f,(math.radians(-28),math.radians(8),math.radians(-18)))
        krot(arm,"RightLowerArm",f,(math.radians(-35),0,0))
        krot(arm,"LeftUpperArm",f,(math.radians(-18),math.radians(-4),math.radians(12)))
        krot(arm,"LeftLowerArm",f,(math.radians(-52),0,0))
        recoil = -0.012 if f in (14,18) else 0
        kloc(arm,"RightHand",f,(0,recoil,0))
    act.frame_start, act.frame_end = 1, 24
    action_to_nla(arm, act)

def anim_hit(arm):
    act = ensure_action(arm, "HitReaction")
    for f, chest, hip in [(1,0,0),(5,math.radians(-14),math.radians(5)),(10,math.radians(7),math.radians(-3)),(18,0,0)]:
        krot(arm,"Chest",f,(chest,0,math.radians(8 if f==5 else 0)))
        krot(arm,"Hips",f,(hip,0,0))
        krot(arm,"LeftUpperArm",f,(0,0,math.radians(15 if f==5 else 0)))
        krot(arm,"RightUpperArm",f,(0,0,math.radians(-12 if f==5 else 0)))
    act.frame_start, act.frame_end = 1, 18
    action_to_nla(arm, act)

def anim_defeat(arm):
    act = ensure_action(arm, "Defeat")
    frames = [1,12,24,36,45]
    hips_z = [0, -0.03, -0.16, -0.34, -0.46]
    chest_rx = [0, math.radians(8), math.radians(28), math.radians(55), math.radians(72)]
    chest_rz = [0, math.radians(4), math.radians(12), math.radians(22), math.radians(28)]
    for f,z,rx,rz in zip(frames,hips_z,chest_rx,chest_rz):
        kloc(arm,"Hips",f,(0,0,z))
        krot(arm,"Chest",f,(rx,0,rz))
        krot(arm,"LeftUpperLeg",f,(math.radians(20 if f>=24 else 0),0,0))
        krot(arm,"RightUpperLeg",f,(math.radians(-12 if f>=24 else 0),0,0))
        krot(arm,"LeftLowerLeg",f,(math.radians(35 if f>=24 else 0),0,0))
        krot(arm,"RightLowerLeg",f,(math.radians(25 if f>=24 else 0),0,0))
        krot(arm,"LeftUpperArm",f,(0,0,math.radians(30 if f>=24 else 0)))
        krot(arm,"RightUpperArm",f,(0,0,math.radians(-35 if f>=24 else 0)))
    act.frame_start, act.frame_end = 1, 45
    action_to_nla(arm, act)

def make_animations(arm):
    anim_idle(arm)
    anim_walk(arm)
    anim_attack(arm)
    anim_hit(arm)
    anim_defeat(arm)
    # Clear active action so NLA is authoritative for export.
    arm.animation_data.action = None

# -----------------------------
# EXPORT AND QA
# -----------------------------
def mesh_tri_count():
    depsgraph = bpy.context.evaluated_depsgraph_get()
    total = 0
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            eo = o.evaluated_get(depsgraph)
            mesh = eo.to_mesh()
            mesh.calc_loop_triangles()
            total += len(mesh.loop_triangles)
            eo.to_mesh_clear()
    return total

def add_scene_metadata(arm):
    arm["game"] = "Escape from Nodnarb"
    arm["asset"] = "Captain"
    arm["height_m"] = CAPTAIN_HEIGHT_M
    arm["target_triangles"] = "2500-3500"
    arm["unity_rig"] = "Humanoid"
    arm["material_count_target"] = 1
    arm["texture_max"] = "1024x1024"
    arm["pivot"] = "feet / Root at world Z=0"

def export_files():
    base = script_dir()
    if EXPORT_GLB:
        glb = base / "Captain_Unity.glb"
        bpy.ops.export_scene.gltf(
            filepath=str(glb),
            export_format='GLB',
            export_apply=True,
            export_animations=True,
            export_nla_strips=True,
            export_materials='EXPORT',
        )
        print("Exported", glb)
    if EXPORT_FBX:
        fbx = base / "Captain_Unity.fbx"
        bpy.ops.export_scene.fbx(
            filepath=str(fbx),
            use_selection=False,
            apply_unit_scale=True,
            bake_space_transform=False,
            add_leaf_bones=False,
            use_armature_deform_only=True,
            bake_anim=True,
            bake_anim_use_all_actions=True,
            bake_anim_simplify_factor=0.0,
            path_mode='AUTO',
        )
        print("Exported", fbx)

def main():
    clean_scene()
    set_units()
    mat = make_material()
    arm = make_armature()
    add_scene_metadata(arm)
    build_character(mat, arm)
    make_animations(arm)

    # Ground marker, confirms pivot at feet.
    bpy.context.scene.cursor.location = (0,0,0)

    tri_count = mesh_tri_count()
    print("Captain generated triangle count:", tri_count)
    if tri_count < 2500 or tri_count > 3500:
        print("NOTE: This is a procedural blockout. Final art pass should land at 2500-3500 tris.")

    # Save a .blend if the file has a writable path.
    base = script_dir()
    try:
        bpy.ops.wm.save_as_mainfile(filepath=str(base / "Captain_Procedural_Blockout.blend"))
        print("Saved Blender file")
    except Exception as exc:
        print("Could not auto-save .blend:", exc)

    if AUTO_EXPORT:
        export_files()

main()
