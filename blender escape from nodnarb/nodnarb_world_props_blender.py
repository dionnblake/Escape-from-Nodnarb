# Escape from Nodnarb - original low-poly environment landmark generator.
# Blender 5.2 CLI safe. Exports small, mobile-readable FBX props for Unity Resources.

import bpy
import math
import sys
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parent.parent
OUTPUT_DIR = PROJECT_ROOT / "Assets" / "EscapeFromNodnarb" / "Runtime" / "Resources" / "World"
ASSET_VERSION = "world-landmarks-v3"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        for item in list(collection):
            collection.remove(item)


def material(name, color, metallic=0.0, roughness=0.82):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    nodes = value.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return value


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


def bevel(obj, width=0.035):
    modifier = obj.modifiers.new("SingleEdgeBevel", "BEVEL")
    modifier.width = width
    modifier.segments = 1
    modifier.affect = "EDGES"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def cube(name, location, dimensions, mat, rotation=(0.0, 0.0, 0.0), edge=0.035):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, edge)
    return assign(obj, mat)


def cylinder(name, location, radius, depth, mat, rotation=(0.0, 0.0, 0.0), vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return assign(obj, mat)


def cone(name, location, radius, depth, mat, rotation=(0.0, 0.0, 0.0), vertices=6):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius * 0.18, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return assign(obj, mat)


def torus(name, location, major_radius, minor_radius, mat, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=10,
        minor_segments=4,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    return assign(obj, mat)


def parent_to_root(root):
    for obj in list(bpy.context.scene.objects):
        if obj != root:
            obj.parent = root


def make_root(asset_name):
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.0, 0.0))
    root = bpy.context.object
    root.name = asset_name + "_ROOT"
    root["game"] = "Escape from Nodnarb"
    root["asset"] = asset_name
    root["source"] = "nodnarb_world_props_blender.py"
    root["version"] = ASSET_VERSION
    root["pivot"] = "ground center / Y-up"
    return root


def build_crashed_engine(root, mats):
    metal, dark, trim, signal = mats
    cylinder("EngineHousing", (0.0, 1.0, 0.0), 1.05, 2.55, metal, rotation=(0.0, math.pi * 0.5, 0.0), vertices=10)
    torus("EngineRim", (1.30, 1.0, 0.0), 0.82, 0.12, trim, rotation=(0.0, math.pi * 0.5, 0.0))
    cylinder("EngineCore", (1.43, 1.0, 0.0), 0.54, 0.12, dark, rotation=(0.0, math.pi * 0.5, 0.0), vertices=8)
    cylinder("EngineGlow", (1.51, 1.0, 0.0), 0.18, 0.08, signal, rotation=(0.0, math.pi * 0.5, 0.0), vertices=8)
    for index in range(4):
        angle = index * math.pi * 0.5
        x = math.cos(angle) * 0.82
        z = math.sin(angle) * 0.82
        cube("EngineBrace", (0.0, 1.0 + math.sin(angle) * 0.55, z), (1.55, 0.12, 0.12), dark, rotation=(0.0, angle, 0.0), edge=0.02)
    for side in (-1.0, 1.0):
        cube("EngineFoot", (-0.42, 0.12, side * 0.70), (0.82, 0.18, 0.22), dark, rotation=(0.0, 0.0, side * 0.16), edge=0.02)
    parent_to_root(root)


def build_snow_arch(root, mats):
    rock, snow, dark, signal = mats
    cube("SnowPillarLeft", (-2.55, 1.45, 0.0), (0.78, 2.9, 1.0), rock, rotation=(0.0, -0.08, -0.04))
    cube("SnowPillarRight", (2.55, 1.45, 0.0), (0.78, 2.9, 1.0), rock, rotation=(0.0, 0.06, 0.04))
    for index in range(7):
        x = -1.95 + index * 0.65
        height = 3.35 + 0.42 * math.sin(index / 6.0 * math.pi)
        angle = (index - 3) * -0.13
        cube("SnowArchBlock", (x, height, 0.0), (0.42, 0.44, 0.96), snow, rotation=(0.0, angle, angle), edge=0.045)
    cube("SnowSignal", (0.0, 0.62, -0.82), (0.12, 0.92, 0.08), signal, edge=0.02)
    cube("SnowBase", (0.0, 0.12, -0.82), (0.82, 0.16, 0.25), dark, edge=0.025)
    parent_to_root(root)


def build_relay_beacon(root, mats):
    rock, metal, dark, signal = mats
    cylinder("RelayBase", (0.0, 0.24, 0.0), 1.18, 0.48, dark, vertices=10)
    cylinder("RelayPlate", (0.0, 0.48, 0.0), 0.86, 0.20, metal, vertices=8)
    cylinder("RelayMast", (0.0, 1.70, 0.0), 0.16, 2.35, rock, vertices=8)
    cylinder("RelayCore", (0.0, 1.70, 0.0), 0.26, 0.42, signal, vertices=8)
    torus("RelayRing", (0.0, 2.56, 0.0), 0.42, 0.055, signal, rotation=(math.pi * 0.5, 0.0, 0.0))
    for side in (-1.0, 1.0):
        cube("RelayArm", (side * 0.76, 0.72, 0.0), (0.72, 0.12, 0.15), metal, rotation=(0.0, 0.0, side * 0.12), edge=0.02)
        cylinder("RelayLight", (side * 0.96, 0.88, 0.0), 0.10, 0.10, signal, rotation=(0.0, math.pi * 0.5, 0.0), vertices=8)
    parent_to_root(root)


def build_canyon_debris(root, mats):
    metal, dark, trim, signal = mats
    cube("DebrisPlate", (0.0, 0.22, 0.0), (3.5, 0.34, 1.55), metal, rotation=(0.05, -0.18, 0.08), edge=0.045)
    cube("DebrisPlateBroken", (-0.58, 0.52, 0.22), (1.45, 0.18, 0.62), trim, rotation=(0.10, 0.24, -0.16), edge=0.025)
    cube("DebrisBeam", (0.78, 0.64, -0.42), (1.85, 0.22, 0.24), dark, rotation=(0.16, -0.34, 0.12), edge=0.025)
    cylinder("DebrisEngine", (-1.14, 0.64, -0.06), 0.56, 0.92, dark, rotation=(0.0, math.pi * 0.5, 0.0), vertices=8)
    torus("DebrisEngineRim", (-1.60, 0.64, -0.06), 0.42, 0.07, trim, rotation=(0.0, math.pi * 0.5, 0.0))
    cylinder("DebrisCore", (-1.68, 0.64, -0.06), 0.20, 0.10, signal, rotation=(0.0, math.pi * 0.5, 0.0), vertices=8)
    for index in range(3):
        angle = (-0.5 + index * 0.5)
        cube("DebrisStrut", (0.25 + index * 0.58, 0.18, 0.42), (0.72, 0.14, 0.18), dark,
             rotation=(0.0, angle, 0.22 - index * 0.12), edge=0.02)
    parent_to_root(root)


def build_crystal_cluster(root, mats):
    rock, crystal, glow, dark = mats
    cylinder("CrystalBase", (0.0, 0.14, 0.0), 1.35, 0.28, dark, vertices=8)
    pieces = [
        (-0.72, 1.15, 0.16, 0.34, 2.30, -0.16),
        (-0.26, 1.55, -0.08, 0.42, 3.10, 0.04),
        (0.22, 1.18, 0.12, 0.36, 2.38, 0.16),
        (0.72, 0.82, -0.08, 0.30, 1.65, 0.28),
        (0.05, 0.72, 0.42, 0.28, 1.45, -0.32),
    ]
    for index, (x, y, z, radius, height, lean) in enumerate(pieces):
        cone("CrystalShard", (x, y, z), radius, height, crystal, rotation=(0.0, lean, 0.0), vertices=6)
    cylinder("CrystalCore", (0.0, 0.58, 0.0), 0.18, 0.42, glow, vertices=8)
    for side in (-1.0, 1.0):
        cube("CrystalBrace", (side * 0.88, 0.30, -0.16), (0.52, 0.12, 0.18), rock,
             rotation=(0.0, side * 0.32, side * 0.08), edge=0.02)
    parent_to_root(root)


def build_hive_growth(root, mats):
    organic, flesh, core, dark = mats
    cube("HiveMat", (0.0, 0.12, 0.0), (2.7, 0.22, 1.30), dark, rotation=(0.0, -0.10, 0.0), edge=0.04)
    pods = [
        (-0.92, 0.66, 0.10, 0.55),
        (-0.24, 0.88, -0.12, 0.72),
        (0.52, 0.62, 0.08, 0.56),
        (1.02, 0.42, -0.25, 0.38),
    ]
    for index, (x, y, z, size) in enumerate(pods):
        pod = bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=size, location=(x, y, z))
        obj = bpy.context.object
        obj.name = "HivePod"
        obj.scale = (1.0, 0.78, 0.88)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        assign(obj, organic)
        cylinder("HiveStem", (x * 0.9, 0.27, z + 0.10), 0.12, max(0.18, y * 0.68), flesh,
                 rotation=(0.0, 0.0, (index - 1.5) * 0.10), vertices=7)
    cylinder("HiveCore", (-0.24, 0.90, -0.16), 0.22, 0.16, core, rotation=(math.pi * 0.5, 0.0, 0.0), vertices=8)
    for side in (-1.0, 1.0):
        cube("HiveTendril", (side * 1.02, 0.34, 0.34), (1.05, 0.10, 0.10), flesh,
             rotation=(0.0, side * 0.35, side * 0.20), edge=0.02)
    parent_to_root(root)


def build_spore_arch(root, mats):
    organic, flesh, core, dark = mats
    cube("SporeBed", (0.0, 0.12, 0.0), (4.8, 0.24, 1.35), dark,
         rotation=(0.0, -0.08, 0.0), edge=0.04)

    for side in (-1.0, 1.0):
        base_x = side * 2.05
        cone("SporePillar", (base_x, 1.22, 0.0), 0.72, 2.55, organic,
             rotation=(0.0, side * 0.16, side * 0.10), vertices=7)
        cone("SporeFleshRidge", (side * 1.58, 1.14, -0.06), 0.34, 2.12, flesh,
             rotation=(0.0, side * 0.22, side * 0.14), vertices=6)
        cylinder("SporeRoot", (side * 1.72, 0.30, 0.18), 0.18, 1.35, flesh,
                 rotation=(0.0, side * 0.24, 0.0), vertices=7)

    canopy_lobes = [
        (-1.72, 2.36, 0.02, 0.64, 0.42, 0.60, -0.18),
        (-1.05, 2.56, 0.04, 0.67, 0.46, 0.68, -0.10),
        (-0.35, 2.70, 0.06, 0.66, 0.50, 0.74, -0.04),
        (0.35, 2.70, 0.06, 0.66, 0.50, 0.74, 0.04),
        (1.05, 2.56, 0.04, 0.67, 0.46, 0.68, 0.10),
        (1.72, 2.36, 0.02, 0.64, 0.42, 0.60, 0.18),
    ]
    for index, (x, y, z, sx, sy, sz, tilt) in enumerate(canopy_lobes):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0, location=(x, y, z))
        canopy = bpy.context.object
        canopy.name = "SporeCanopyLobe"
        canopy.scale = (sx, sy, sz)
        canopy.rotation_euler[2] = tilt
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        assign(canopy, organic if index % 2 == 0 else flesh)

    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.34, location=(0.0, 2.38, -0.78))
    signal = bpy.context.object
    signal.name = "SporeCore"
    signal.scale = (1.0, 0.62, 0.46)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(signal, core)

    for index, (x, y, z, size) in enumerate([
        (-1.10, 0.82, -0.22, 0.42),
        (-0.42, 1.05, 0.18, 0.54),
        (0.46, 0.96, 0.10, 0.48),
        (1.18, 0.76, -0.18, 0.36),
    ]):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=size, location=(x, y, z))
        pod = bpy.context.object
        pod.name = "SporePod"
        pod.scale = (1.0, 0.72, 0.86)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        assign(pod, flesh if index % 2 == 0 else organic)

    for side in (-1.0, 1.0):
        cube("SporeRootPlate", (side * 1.20, 0.28, 0.34), (1.20, 0.12, 0.16), flesh,
             rotation=(0.0, side * 0.26, side * 0.18), edge=0.02)
    parent_to_root(root)


def build_ruin_gate(root, mats):
    metal, dark, signal, trim = mats
    cube("GateBase", (0.0, 0.12, 0.0), (5.4, 0.24, 1.35), dark, edge=0.04)
    for side in (-1.0, 1.0):
        cube("GatePylon", (side * 2.15, 1.72, 0.0), (0.58, 3.24, 0.86), metal,
             rotation=(0.0, 0.0, side * 0.14), edge=0.045)
        cube("GatePylonInset", (side * 2.15, 1.72, -0.48), (0.18, 1.85, 0.08), signal,
             rotation=(0.0, 0.0, side * 0.14), edge=0.018)
        cube("GateBrace", (side * 1.58, 3.02, 0.0), (1.45, 0.18, 0.30), trim,
             rotation=(0.0, 0.0, side * 0.12), edge=0.02)
    cube("GateBrokenBeam", (0.0, 3.34, 0.0), (2.55, 0.34, 0.68), trim,
         rotation=(0.0, 0.0, 0.08), edge=0.04)
    torus("GateSignalRing", (0.0, 2.22, -0.48), 0.64, 0.08, signal, rotation=(math.pi * 0.5, 0.0, 0.0))
    cube("GateCore", (0.0, 2.22, -0.56), (0.28, 0.72, 0.08), signal, edge=0.018)
    parent_to_root(root)


def build_hive_obelisk(root, mats):
    organic, flesh, core, dark = mats
    cylinder("ObeliskBase", (0.0, 0.16, 0.0), 1.30, 0.32, dark, vertices=8)
    cone("ObeliskBody", (0.0, 2.05, 0.0), 0.94, 3.78, organic, vertices=7)
    cone("ObeliskSpineLeft", (-0.82, 1.36, 0.16), 0.26, 2.25, flesh,
         rotation=(0.0, -0.22, -0.10), vertices=6)
    cone("ObeliskSpineRight", (0.82, 1.36, 0.16), 0.26, 2.25, flesh,
         rotation=(0.0, 0.22, 0.10), vertices=6)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.30, location=(0.0, 2.10, -0.78))
    eye = bpy.context.object
    eye.name = "ObeliskCore"
    assign(eye, core)
    for side in (-1.0, 1.0):
        cube("ObeliskTendril", (side * 1.02, 0.52, 0.32), (1.15, 0.12, 0.12), flesh,
             rotation=(0.0, side * 0.36, side * 0.22), edge=0.02)
        cube("ObeliskShard", (side * 0.48, 0.70, 0.40), (0.16, 0.82, 0.22), organic,
             rotation=(0.0, side * 0.20, side * 0.18), edge=0.018)
    parent_to_root(root)


def build_extraction_beacon(root, mats):
    metal, dark, signal, trim = mats
    cylinder("ExtractionBase", (0.0, 0.24, 0.0), 1.46, 0.48, dark, vertices=10)
    cylinder("ExtractionPlate", (0.0, 0.54, 0.0), 1.08, 0.18, metal, vertices=8)
    cylinder("ExtractionMast", (0.0, 2.10, 0.0), 0.20, 3.05, trim, vertices=8)
    cylinder("ExtractionCore", (0.0, 2.10, 0.0), 0.34, 0.50, signal, vertices=8)
    torus("ExtractionRing", (0.0, 3.62, 0.0), 0.62, 0.08, signal)
    cone("ExtractionCap", (0.0, 3.92, 0.0), 0.30, 0.54, signal, vertices=6)
    for index in range(4):
        angle = index * math.pi * 0.5
        side_x = math.cos(angle) * 0.88
        side_z = math.sin(angle) * 0.88
        cube("ExtractionFin", (side_x, 0.82, side_z), (0.72, 0.14, 0.18), metal,
             rotation=(0.0, -angle, 0.0), edge=0.02)
    parent_to_root(root)


def build_asset(asset_name, builder, material_colors):
    clear_scene()
    mats = [material(name, color, metallic=metallic) for name, color, metallic in material_colors]
    root = make_root(asset_name)
    builder(root, mats)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    output = OUTPUT_DIR / (asset_name + ".fbx")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(output),
        use_selection=False,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    print("NODNARB_WORLD_PROP", asset_name, output)


def main():
    set_units = bpy.context.scene.unit_settings
    set_units.system = "METRIC"
    set_units.scale_length = 1.0

    if "--only-spore-arch" in sys.argv:
        build_asset(
            "SporeArch",
            build_spore_arch,
            [
                ("WorldOrganic", (0.22, 0.10, 0.25), 0.05),
                ("WorldFlesh", (0.54, 0.16, 0.36), 0.05),
                ("WorldCore", (0.82, 0.90, 0.28), 0.05),
                ("WorldDark", (0.06, 0.05, 0.09), 0.20),
            ],
        )
        return

    build_asset(
        "CrashedEngine",
        build_crashed_engine,
        [
            ("WorldMetal", (0.31, 0.34, 0.34), 0.55),
            ("WorldDark", (0.08, 0.10, 0.11), 0.30),
            ("WorldTrim", (0.56, 0.36, 0.20), 0.30),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
        ],
    )
    build_asset(
        "SnowArch",
        build_snow_arch,
        [
            ("WorldRock", (0.25, 0.31, 0.36), 0.05),
            ("WorldSnow", (0.78, 0.88, 0.94), 0.02),
            ("WorldDark", (0.08, 0.12, 0.16), 0.15),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
        ],
    )
    build_asset(
        "RelayBeacon",
        build_relay_beacon,
        [
            ("WorldRock", (0.25, 0.31, 0.32), 0.05),
            ("WorldMetal", (0.42, 0.46, 0.44), 0.55),
            ("WorldDark", (0.07, 0.10, 0.10), 0.30),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
        ],
    )
    build_asset(
        "CanyonDebris",
        build_canyon_debris,
        [
            ("WorldMetal", (0.34, 0.38, 0.39), 0.55),
            ("WorldDark", (0.08, 0.10, 0.11), 0.30),
            ("WorldTrim", (0.56, 0.36, 0.20), 0.30),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
        ],
    )
    build_asset(
        "CrystalCluster",
        build_crystal_cluster,
        [
            ("WorldRock", (0.18, 0.15, 0.22), 0.05),
            ("WorldCrystal", (0.48, 0.18, 0.72), 0.12),
            ("WorldSignal", (0.90, 0.24, 0.70), 0.05),
            ("WorldDark", (0.07, 0.06, 0.10), 0.20),
        ],
    )
    build_asset(
        "HiveGrowth",
        build_hive_growth,
        [
            ("WorldOrganic", (0.24, 0.08, 0.22), 0.05),
            ("WorldFlesh", (0.58, 0.12, 0.32), 0.05),
            ("WorldCore", (0.95, 0.18, 0.42), 0.05),
            ("WorldDark", (0.07, 0.05, 0.08), 0.20),
        ],
    )
    build_asset(
        "SporeArch",
        build_spore_arch,
        [
            ("WorldOrganic", (0.22, 0.10, 0.25), 0.05),
            ("WorldFlesh", (0.54, 0.16, 0.36), 0.05),
            ("WorldCore", (0.82, 0.90, 0.28), 0.05),
            ("WorldDark", (0.06, 0.05, 0.09), 0.20),
        ],
    )
    build_asset(
        "RuinGate",
        build_ruin_gate,
        [
            ("WorldMetal", (0.36, 0.42, 0.42), 0.55),
            ("WorldDark", (0.07, 0.11, 0.13), 0.30),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
            ("WorldTrim", (0.62, 0.45, 0.24), 0.30),
        ],
    )
    build_asset(
        "HiveObelisk",
        build_hive_obelisk,
        [
            ("WorldOrganic", (0.25, 0.08, 0.22), 0.05),
            ("WorldFlesh", (0.64, 0.12, 0.34), 0.05),
            ("WorldCore", (0.96, 0.20, 0.45), 0.05),
            ("WorldDark", (0.06, 0.04, 0.08), 0.20),
        ],
    )
    build_asset(
        "ExtractionBeacon",
        build_extraction_beacon,
        [
            ("WorldMetal", (0.42, 0.48, 0.46), 0.55),
            ("WorldDark", (0.06, 0.10, 0.10), 0.30),
            ("WorldSignal", (0.32, 0.95, 0.10), 0.05),
            ("WorldTrim", (0.75, 0.60, 0.28), 0.30),
        ],
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIR / "Nodnarb_World_Props.blend"))
    print("NODNARB_WORLD_PROPS_OK", ASSET_VERSION, OUTPUT_DIR)


main()
