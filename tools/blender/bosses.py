"""
Boss accessories, modelled in the rest-pose space of each boss's base model and attached to its bones in game.
Run:  blender --background --factory-startup --python tools/blender/bosses.py [-- garmr vali hel]
Exports Assets/Wyrdbound/Resources/Models/{GarmrGear,ValiGear,HelGear,HelScepter}.fbx; previews show each piece
on its base model (Builds/blender_preview/<boss>_*.png). Object names are "<part>@<bone>".
"""
import sys, os, math
sys.path.append(os.path.dirname(__file__))
import bpy
from mathutils import Vector
from wyrdlib import *

WOLF = "Assets/ThirdParty/Resources/Quaternius/Wolf.fbx"
BARB = "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/Barbarian.fbx"
MAGE = "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/Mage.fbx"
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["garmr", "vali", "hel"]


def hide_parts(objs, names):
    for o in objs:
        if any(n in o.name for n in names):
            o.hide_render = True


# =============================================================== Garmr: the blood-chested hound of Gnipahellir
def garmr():
    reset()
    base = import_base(WOLF)
    iron_m = material("WyrdAcc_Iron", rough=0.35)
    bone_m = material("WyrdAcc_Bone", rough=0.6)
    blood_m = material("WyrdAcc_Blood", rough=0.3)
    glow_m = material("WyrdAcc_Glow_Eyes", glow=(1.0, 0.55, 0.05))
    out = []

    # spiked iron collar around the neck, tilted with it, and a broken chain hanging from it
    tilt = math.radians(90 - 18)
    collar = [paint(torus("Collar", (0, -1.42, 1.86), 0.42, 0.075, rot=(tilt, 0, 0)), flat_fn(IRON, 0.25, freq=14))]
    for i in range(10):
        a = 2 * math.pi * i / 10
        d = Vector((math.cos(a), 0, math.sin(a)))
        d.rotate(Vector((1, 0, 0)).rotation_difference(Vector((1, 0, 0))))
        rim = Vector((0.42 * math.cos(a), -1.42 + 0.13 * math.sin(a), 1.86 + 0.40 * math.sin(a)))
        if math.sin(a) < -0.6:
            continue
        outv = (rim - Vector((0, -1.42, 1.86))).normalized()
        collar.append(paint(cone("Stud", rim, rim + outv * 0.2, 0.05), flat_fn((0.3, 0.3, 0.32), 0.15)))
    for k in range(6):
        z = 1.42 - k * 0.13
        rot = (0, 0, 0) if k % 2 == 0 else (0, 0, math.radians(90))
        collar.append(paint(torus("Link", (0.0, -1.5 + k * 0.02, z), 0.07, 0.022, rot=(math.radians(90), rot[1], rot[2]), segs=10, minor_segs=6), flat_fn(IRON, 0.3, freq=20)))
    c = join(collar, "Collar@Neck2")
    set_material(c, iron_m)
    out.append(c)

    # bone spikes along the spine, one group per spine bone so they follow its bend
    groups = {"Back": [], "Torso": [], "Torso2": [], "Torso3": []}
    for i in range(9):
        y = 0.62 - i * 0.2
        h = 0.55 - abs(i - 3) * 0.05
        z = 2.06 + 0.02 * math.sin(i)
        sp = paint(cone("Spike", (0, y, z), (0, y + 0.28, z + h), 0.085, bend=-0.25, segments=8, rings=6), bone_fn(i))
        key = "Back" if y > 0.3 else "Torso" if y > -0.26 else "Torso2" if y > -0.85 else "Torso3"
        groups[key].append(sp)
    for k, v in groups.items():
        if v:
            g = join(v, "Spikes@" + k)
            set_material(g, bone_m)
            out.append(g)

    # the blood-matted chest
    chest = paint(blob("Chest", [E((0, -1.5, 1.5), 0.36, (0.95, 0.6, 1.1)), E((0, -1.3, 1.2), 0.3, (0.9, 0.65, 0.9))], voxel=0.03, rough=0.04),
                  lambda p, n: [x * (0.7 + 0.5 * abs(math.sin(p.z * 30 + p.x * 8))) for x in BLOOD])
    decimate(chest, 900)
    chest.name = "Chest@Neck1"
    set_material(chest, blood_m)
    out.append(chest)

    eyes = join([sphere("Eye", (0.19 * sx, -2.18, 2.3), 0.055, seg=12) for sx in (1, -1)], "Eyes@Head")
    set_material(eyes, glow_m)
    out.append(eyes)

    export(out, "GarmrGear.fbx")
    preview("garmr", (0, -0.6, 1.4), [((5.5, -8.5, 7.5), "game"), ((8.5, -1.2, 2.2), "side")], rim=(1.0, 0.4, 0.3), lens=50)


# =============================================================== Váli: the wolf-brother
def vali():
    reset()
    base = import_base(BARB)
    hide_parts(base, ("Hat", "Mug", "Shield", "2H_Axe"))
    fur_m = material("WyrdAcc_Fur", rough=0.8)
    bone_m = material("WyrdAcc_Bone", rough=0.6)
    glow_m = material("WyrdAcc_Glow_Eyes", glow=(1.0, 0.7, 0.1))
    out = []
    wolf_fur = fur_fn((0.20, 0.17, 0.15), (0.46, 0.42, 0.38))

    # a wolf's head worn as a helm: pelt cap, brow, jutting snout and ears
    helm = paint(blob("Helm", [
        E((0, 0.02, 1.98), 0.57, (1.0, 1.04, 0.55)),
        E((0, -0.42, 2.0), 0.3, (1.35, 0.6, 0.45)),
        E((0, -0.8, 1.98), 0.25, (0.72, 1.3, 0.55)),
        E((0, -1.07, 1.96), 0.1, (1.2, 1.0, 0.9)),
        E((0.26, -0.55, 2.08), 0.13, (1.0, 1.5, 0.6)), E((-0.26, -0.55, 2.08), 0.13, (1.0, 1.5, 0.6)),
    ], voxel=0.022, rough=0.025), wolf_fur)
    decimate(helm, 3500)
    parts = [helm]
    for sx in (1, -1):
        parts.append(paint(cone("Ear", (0.3 * sx, -0.02, 2.24), (0.44 * sx, 0.12, 2.75), 0.14, bend=-0.1, segments=8), wolf_fur))
        for i, y in enumerate((-0.98, -0.86, -0.72)):
            parts.append(paint(cone("Fang", (0.1 * sx, y, 1.86), (0.1 * sx, y - 0.02, 1.66 + i * 0.03), 0.032), bone_fn(i)))
    helm = join(parts, "Helm@head")
    set_material(helm, fur_m)
    out.append(helm)
    eyes = join([sphere("Eye", (0.22 * sx, -0.74, 2.12), 0.045, seg=12) for sx in (1, -1)], "Eyes@head")
    set_material(eyes, glow_m)
    out.append(eyes)

    # a heavy fur mantle over the shoulders and the pelt hanging down the back
    mantle = paint(blob("Mantle", [
        E((0.34, 0.0, 1.3), 0.3, (1.2, 1.25, 0.55)), E((-0.34, 0.0, 1.3), 0.3, (1.2, 1.25, 0.55)),
        E((0, 0.26, 1.3), 0.3, (1.3, 0.8, 0.55)), E((0, -0.22, 1.26), 0.24, (1.2, 0.7, 0.5)),
        E((0, 0.38, 0.95), 0.32, (1.15, 0.45, 1.25)),
    ], voxel=0.025, rough=0.03), wolf_fur)
    decimate(mantle, 2500)
    mantle.name = "Mantle@chest"
    set_material(mantle, fur_m)
    out.append(mantle)

    export(out, "ValiGear.fbx")
    preview("vali", (0, 0, 1.3), [((3.2, -5.4, 5.2), "game"), ((4.6, -2.6, 1.6), "front")], rim=(1.0, 0.45, 0.3), lens=55)


# =============================================================== Hel: queen of the dead, half living, half corpse
def hel():
    reset()
    base = import_base(MAGE)
    hide_parts(base, ("Hat", "Spellbook", "Wand", "Staff"))
    bone_m = material("WyrdAcc_Bone", rough=0.6)
    iron_m = material("WyrdAcc_Iron", rough=0.35)
    glow_m = material("WyrdAcc_Glow_Hel", glow=(0.55, 0.95, 1.0))
    out = []

    # a crown of bone spikes on an iron band
    crown = [paint(torus("Band", (0, 0, 2.06), 0.43, 0.05), flat_fn(IRON, 0.3, freq=16))]
    for i in range(11):
        a = -math.pi / 2 + (i - 5) * (2 * math.pi / 11)
        front = math.cos(a + math.pi / 2)
        r = Vector((0.43 * math.cos(a), 0.43 * math.sin(a), 2.08))
        outv = Vector((math.cos(a), math.sin(a), 0)) * 0.14
        h = 0.32 + 0.33 * max(0.0, front) ** 2
        crown.append(paint(cone("CrownSpike", r, r + outv + Vector((0, 0, h)), 0.06, bend=0.15, bend_axis=(math.sin(a), -math.cos(a), 0), segments=8), bone_fn(i)))
    crown = join(crown, "Crown@head")
    set_material(crown, bone_m)
    out.append(crown)

    # the dead half of her face: a skull mask over the left side, with a glowing eye
    mask = paint(blob("Mask", [
        E((0.25, -0.47, 1.68), 0.3, (0.72, 0.32, 1.05)),
        E((0.26, -0.47, 1.44), 0.16, (1.0, 0.45, 0.6)),
        E((0.25, -0.62, 1.75), 0.1, neg=True),
    ], voxel=0.018, rough=0.012), bone_fn(3.0))
    decimate(mask, 1800)
    teeth = [paint(cone("Tooth", (0.12 + i * 0.06, -0.56, 1.44), (0.12 + i * 0.06, -0.58, 1.36), 0.022, segments=6, rings=3), bone_fn(i)) for i in range(5)]
    mask = join([mask] + teeth, "Mask@head")
    set_material(mask, bone_m)
    out.append(mask)
    eye = sphere("DeadEye@head", (0.25, -0.57, 1.75), 0.05, seg=12)
    set_material(eye, glow_m)
    out.append(eye)

    # bone pauldrons with spikes
    for side, sx in (("l", 1), ("r", -1)):
        p = [paint(blob("Pauldron", [E((0.42 * sx, 0.0, 1.27), 0.2, (1.15, 1.05, 0.75)), E((0.52 * sx, 0.0, 1.18), 0.13)], voxel=0.02, rough=0.015), bone_fn(sx))]
        for i, (dy, up) in enumerate(((-0.1, 0.32), (0.05, 0.42), (0.18, 0.3))):
            b = Vector((0.45 * sx, dy, 1.36))
            p.append(paint(cone("Spike", b, b + Vector((0.12 * sx, 0.03, up)), 0.05, segments=8), bone_fn(i + 4)))
        pd = join(p, "Pauldron@upperarm." + side)
        set_material(pd, bone_m)
        out.append(pd)

    export(out, "HelGear.fbx")

    # her scepter (an item held in the right hand slot: handle along Z, grip at the origin)
    sc = [paint(cylinder("Shaft", (0, 0, -0.95), (0, 0, 1.2), 0.045), flat_fn((0.12, 0.11, 0.14), 0.3, freq=20))]
    for z in (0.25, 0.85):
        sc.append(paint(torus("Ring", (0, 0, z), 0.06, 0.022), bone_fn(z)))
    skull = paint(blob("SkullTop", [E((0, 0, 1.36), 0.17, (0.9, 1.0, 1.0)), E((0, -0.05, 1.25), 0.1, (0.8, 1.0, 0.7)),
                                     E((0.06, -0.15, 1.39), 0.045, neg=True), E((-0.06, -0.15, 1.39), 0.045, neg=True)], voxel=0.012, rough=0.006), bone_fn(9))
    sc.append(skull)
    for sx in (1, -1):
        sc.append(paint(cone("Horn", (0.12 * sx, 0.02, 1.42), (0.3 * sx, 0.12, 1.72), 0.04, bend=0.3, bend_axis=(0, 1, 0)), bone_fn(sx)))
    scepter = join(sc, "Scepter")
    set_material(scepter, bone_m)
    orb = sphere("Orb", (0, 0, 1.66), 0.08, seg=14)
    set_material(orb, glow_m)
    export([scepter, orb], "HelScepter.fbx")
    # preview the scepter in her right hand
    for o in (scepter, orb):
        o.rotation_euler = (math.radians(-90), 0, 0)
        o.location = (-0.88, -0.11, 1.05)
    preview("hel", (0, 0, 1.45), [((3.2, -5.4, 5.4), "game"), ((3.0, -3.6, 1.8), "front")], rim=(0.6, 0.9, 1.0), lens=55)


for name in argv:
    {"garmr": garmr, "vali": vali, "hel": hel}[name]()
