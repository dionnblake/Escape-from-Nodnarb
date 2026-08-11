"""Generate original low-poly Nodnarb alien prefabs for Unity Resources."""

import bpy
import math
from mathutils import Vector
from pathlib import Path


OUTPUT_DIR = Path(__file__).resolve().parent / "GeneratedAssets" / "Enemies"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for meshes in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        for datablock in list(meshes):
            meshes.remove(datablock)


def material(name, color, metallic=0.0, roughness=0.74, emission=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 3.0
        else:
            bsdf.inputs["Emission"].default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 3.0
    return mat


def apply_bevel(obj, width=0.025):
    if obj.type != "MESH" or width <= 0.0:
        return
    bevel = obj.modifiers.new("ReadableEdge", "BEVEL")
    bevel.width = width
    bevel.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)


def attach(obj, root, mat):
    obj.parent = root
    obj.data.materials.append(mat)
    return obj


def cube(name, root, loc, half_extents, mat, rotation=(0.0, 0.0, 0.0), bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = half_extents
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    apply_bevel(obj, bevel)
    return attach(obj, root, mat)


def ico(name, root, loc, scale, mat):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return attach(obj, root, mat)


def sphere(name, root, loc, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return attach(obj, root, mat)


def cylinder_between(name, root, start, end, radius, mat, vertices=8):
    start_vec = Vector(start)
    end_vec = Vector(end)
    direction = end_vec - start_vec
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=direction.length,
        location=(start_vec + end_vec) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    apply_bevel(obj, radius * 0.22)
    return attach(obj, root, mat)


def cone_between(name, root, start, end, radius, mat):
    start_vec = Vector(start)
    end_vec = Vector(end)
    direction = end_vec - start_vec
    bpy.ops.mesh.primitive_cone_add(
        vertices=6,
        radius1=radius,
        radius2=0.015,
        depth=direction.length,
        location=(start_vec + end_vec) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    return attach(obj, root, mat)


def root_for(name, role, triangle_target):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    root["game"] = "Escape from Nodnarb"
    root["asset"] = name
    root["role"] = role
    root["triangle_budget"] = triangle_target
    root["pivot"] = "feet / world origin"
    return root


def build_rusher(m):
    root = root_for("Rusher", "fast melee swarm alien", "1200-1800")
    ico("Carapace", root, (0.0, 0.56, 0.05), (0.70, 0.43, 0.74), m["violet"])
    ico("JawHead", root, (0.0, 0.48, -0.55), (0.43, 0.32, 0.42), m["violet_light"])
    cube("JawPlate", root, (0.0, 0.34, -0.82), (0.33, 0.09, 0.16), m["dark"], bevel=0.025)
    sphere("Eye", root, (0.0, 0.60, -0.88), (0.11, 0.11, 0.07), m["glow"])
    for index in range(3):
        z = -0.35 + index * 0.39
        for side in (-1.0, 1.0):
            hip = (side * (0.40 + index * 0.08), 0.38, z)
            claw = (side * (0.92 + index * 0.08), 0.18, z - 0.12)
            cylinder_between("Leg", root, hip, claw, 0.10, m["violet_dark"])
            cone_between("Claw", root, claw, (claw[0] + side * 0.22, 0.10, claw[2] - 0.12), 0.13, m["violet"])
    return root


def build_spitter(m):
    root = root_for("Spitter", "ranged biological artillery alien", "1200-1800")
    ico("Core", root, (0.0, 0.93, 0.0), (0.55, 0.52, 0.55), m["ranged"])
    sphere("Eye", root, (0.0, 0.96, -0.51), (0.24, 0.24, 0.11), m["glow"])
    for side in (-1.0, 1.0):
        shoulder = (side * 0.38, 1.02, 0.0)
        elbow = (side * 0.72, 0.75, -0.04)
        hand = (side * 0.96, 0.32, -0.12)
        cylinder_between("UpperArm", root, shoulder, elbow, 0.075, m["ranged_light"])
        cylinder_between("LowerArm", root, elbow, hand, 0.065, m["ranged_light"])
        cone_between("Claw", root, hand, (hand[0] + side * 0.16, 0.10, hand[2] - 0.05), 0.11, m["ranged"])
    for index in range(3):
        x = (index - 1) * 0.12
        cylinder_between("HangingTendril", root, (x, 0.47, 0.08), (x * 1.2, 0.10, 0.02), 0.035, m["dark"] , vertices=6)
    return root


def build_blocker(m):
    root = root_for("Blocker", "defensive armored alien", "1200-1800")
    ico("Body", root, (0.0, 0.66, 0.08), (0.78, 0.62, 0.62), m["armored"])
    cube("FrontShield", root, (0.0, 0.74, -0.56), (0.72, 0.82, 0.14), m["slate"], bevel=0.05)
    cube("ShieldTrim", root, (0.0, 0.75, -0.72), (0.11, 0.68, 0.035), m["trim"], bevel=0.015)
    for side in (-1.0, 1.0):
        cylinder_between("Arm", root, (side * 0.55, 0.54, -0.06), (side * 0.84, 0.25, -0.38), 0.13, m["armored"])
        cone_between("FootClaw", root, (side * 0.82, 0.18, -0.44), (side * 0.98, 0.10, -0.65), 0.15, m["trim"])
    return root


def build_carrier(m):
    root = root_for("Carrier", "major armored boss alien", "6000-9000")
    ico("Body", root, (0.0, 1.12, 0.10), (1.35, 0.92, 1.40), m["violet"])
    ico("DorsalShell", root, (0.0, 1.62, 0.42), (1.12, 0.56, 1.05), m["slate"])
    ico("Head", root, (0.0, 0.88, -1.00), (0.54, 0.42, 0.56), m["violet_dark"])
    sphere("Core", root, (0.0, 1.58, -0.63), (0.40, 0.40, 0.12), m["glow"])
    for side in (-1.0, 1.0):
        shoulder = (side * 0.95, 1.02, -0.18)
        claw = (side * 1.55, 0.34, -0.70)
        cylinder_between("HeavyClawArm", root, shoulder, claw, 0.22, m["violet_dark"])
        cone_between("HeavyClaw", root, claw, (claw[0] + side * 0.28, 0.08, claw[2] - 0.28), 0.25, m["trim"])
    for index in range(3):
        x = (index - 1) * 0.70
        cube("DorsalPlate", root, (x, 2.10, 0.30), (0.27, 0.13, 0.54), m["slate"], rotation=(0.12, 0.0, (index - 1) * 0.12), bevel=0.04)
    return root


def build_soldier(m):
    root = root_for("CrewSoldier", "rescued crew combat unit", "1800-2400")
    cube("Torso", root, (0.0, 0.82, 0.0), (0.25, 0.31, 0.18), m["charcoal"], bevel=0.045)
    cube("ChestArmor", root, (0.0, 0.98, -0.17), (0.25, 0.17, 0.06), m["offwhite"], bevel=0.03)
    cube("ChestLight", root, (0.0, 1.01, -0.235), (0.07, 0.035, 0.015), m["green"], bevel=0.006)
    cube("Pack", root, (0.0, 0.76, 0.20), (0.21, 0.27, 0.08), m["metal"], bevel=0.025)
    sphere("Head", root, (0.0, 1.45, 0.0), (0.16, 0.17, 0.17), m["skin"])
    cube("Helmet", root, (0.0, 1.57, 0.0), (0.17, 0.07, 0.18), m["offwhite"], bevel=0.025)
    for side in (-1.0, 1.0):
        cylinder_between("UpperArm", root, (side * 0.27, 1.02, 0.0), (side * 0.39, 0.72, -0.03), 0.07, m["charcoal"])
        cube("Shoulder", root, (side * 0.29, 1.06, 0.0), (0.10, 0.10, 0.11), m["offwhite"], bevel=0.025)
        cube("Forearm", root, (side * 0.39, 0.70, -0.06), (0.08, 0.14, 0.09), m["offwhite"], bevel=0.02)
        cylinder_between("Thigh", root, (side * 0.13, 0.58, 0.0), (side * 0.14, 0.30, -0.02), 0.09, m["charcoal"])
        cube("Knee", root, (side * 0.14, 0.34, -0.09), (0.09, 0.08, 0.07), m["offwhite"], bevel=0.018)
        cube("Boot", root, (side * 0.14, 0.10, -0.06), (0.11, 0.10, 0.20), m["metal"], bevel=0.025)
    cube("RifleBody", root, (0.25, 0.76, -0.32), (0.10, 0.08, 0.38), m["metal"], bevel=0.018)
    cube("RifleCell", root, (0.25, 0.72, -0.35), (0.06, 0.05, 0.12), m["green"], bevel=0.008)
    cube("RifleGrip", root, (0.25, 0.61, -0.18), (0.06, 0.11, 0.07), m["charcoal"], bevel=0.012)
    cylinder_between("RifleMuzzle", root, (0.25, 0.76, -0.70), (0.25, 0.76, -0.88), 0.045, m["metal"])
    muzzle = bpy.data.objects.new("MuzzlePoint", None)
    bpy.context.collection.objects.link(muzzle)
    muzzle.parent = root
    muzzle.location = (0.25, 0.76, -0.90)
    return root


def export_asset(builder, name):
    clear_scene()
    mats = {
        "violet": material("M_Violet", (0.30, 0.10, 0.42), roughness=0.82),
        "violet_light": material("M_VioletLight", (0.48, 0.18, 0.58), roughness=0.76),
        "violet_dark": material("M_VioletDark", (0.12, 0.06, 0.18), roughness=0.86),
        "ranged": material("M_RangedPink", (0.56, 0.12, 0.30), roughness=0.78),
        "ranged_light": material("M_RangedRose", (0.78, 0.23, 0.47), roughness=0.74),
        "armored": material("M_ArmoredSlate", (0.18, 0.25, 0.29), metallic=0.18, roughness=0.68),
        "slate": material("M_Slate", (0.30, 0.37, 0.40), metallic=0.22, roughness=0.64),
        "trim": material("M_Trim", (0.62, 0.52, 0.38), metallic=0.18, roughness=0.60),
        "dark": material("M_DeepJoint", (0.055, 0.04, 0.075), roughness=0.92),
        "glow": material("M_CoreGlow", (1.0, 0.12, 0.46), roughness=0.32, emission=(1.0, 0.04, 0.25)),
        "offwhite": material("M_ArmorWhite", (0.70, 0.68, 0.60), metallic=0.10, roughness=0.72),
        "charcoal": material("M_Undersuit", (0.10, 0.12, 0.12), roughness=0.88),
        "green": material("M_RescueGreen", (0.35, 0.88, 0.08), roughness=0.34, emission=(0.20, 0.95, 0.05)),
        "metal": material("M_GunMetal", (0.26, 0.30, 0.29), metallic=0.38, roughness=0.54),
        "skin": material("M_Skin", (0.38, 0.22, 0.14), roughness=0.80),
    }
    root = builder(mats)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    filepath = OUTPUT_DIR / f"{name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        object_types={"MESH", "EMPTY"},
    )
    print(f"NODNARB_ENEMY_EXPORT name={name} output={filepath}")


def main():
    export_asset(build_rusher, "Rusher")
    export_asset(build_spitter, "Spitter")
    export_asset(build_blocker, "Blocker")
    export_asset(build_carrier, "Carrier")
    export_asset(build_soldier, "CrewSoldier")


if __name__ == "__main__":
    main()
