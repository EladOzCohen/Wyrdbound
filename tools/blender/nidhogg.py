"""
Níðhöggr, the dragon who gnaws the root of the World-Tree: head (skull + hinged jaw + eyes) and body coil.
Run:  blender --background --factory-startup --python tools/blender/nidhogg.py
Exports Assets/Wyrdbound/Resources/Models/Nidhogg.fbx and NidhoggCoil.fbx; previews in Builds/blender_preview.
The head faces -Y with Z up (the game turns it to face its target); the neck reaches down into the floor.
"""
import sys, os, math
sys.path.append(os.path.dirname(__file__))
import bpy
from mathutils import Vector
from wyrdlib import *

reset()
BACK, SIDE, BELLY = (0.055, 0.10, 0.045), (0.10, 0.17, 0.07), (0.46, 0.42, 0.26)
hide = scales_fn(BACK, SIDE, BELLY)

head = [
    E((0, 1.55, -0.85), 0.5), E((0, 1.25, -0.45), 0.5), E((0, 0.95, -0.12), 0.48), E((0, 0.62, 0.12), 0.48),
    E((0, 0.15, 0.3), 0.6, (1.05, 1.15, 0.78)), E((0, 0.45, 0.42), 0.38, (1.1, 0.9, 0.7)),
    E((0, -0.55, 0.2), 0.48, (0.68, 1.35, 0.5)), E((0, -1.02, 0.14), 0.3, (0.75, 1.25, 0.55)), E((0, -1.3, 0.12), 0.17, (0.85, 1.0, 0.65)),
    E((0.27, -0.36, 0.45), 0.2, (0.95, 1.7, 0.5), rot=((0, 0, 1), math.radians(-14))),
    E((-0.27, -0.36, 0.45), 0.2, (0.95, 1.7, 0.5), rot=((0, 0, 1), math.radians(14))),
    E((0.43, 0.0, 0.08), 0.26, (0.75, 1.2, 0.85)), E((-0.43, 0.0, 0.08), 0.26, (0.75, 1.2, 0.85)),
    E((0.12, -1.25, 0.24), 0.075), E((-0.12, -1.25, 0.24), 0.075),
    E((0.32, -0.5, 0.33), 0.13, neg=True), E((-0.32, -0.5, 0.33), 0.13, neg=True),
]
skull = paint(blob("Skull", head), hide)
decimate(skull, 6500)

spikes = []
for sx in (1, -1):
    spikes.append(cone("Horn", (0.3 * sx, 0.2, 0.58), (0.6 * sx, 1.3, 1.05), 0.15, bend=-0.35, bend_axis=(sx, 0, 0), segments=12, rings=10))
    spikes.append(cone("HornS", (0.45 * sx, 0.02, 0.38), (0.95 * sx, 0.6, 0.45), 0.075, bend=-0.2))
    for i in range(3):
        y = -0.56 + i * 0.13
        spikes.append(cone("Brow", (0.3 * sx, y, 0.52), (0.38 * sx, y + 0.2, 0.66), 0.035))
        spikes.append(cone("Jowl", (0.5 * sx, 0.05 + i * 0.12, 0.02), (0.75 * sx, 0.25 + i * 0.12, -0.08), 0.04))
    for i in range(5):
        y, x = -1.24 + i * 0.16, (0.15 - 0.012 * i) * sx
        spikes.append(cone("Fang", (x, y, 0.0), (x, y + 0.01, -0.2 + i * 0.025), 0.036 - i * 0.003))
for i in range(7):
    y, z = 0.4 + i * 0.2, 0.68 - i * 0.18
    spikes.append(cone("Crest", (0, y, z), (0, y + 0.22, z + 0.4 - i * 0.03), 0.1 - i * 0.008, bend=-0.3))
for s in spikes:
    paint(s, bone_fn())
skull = join([skull] + spikes, "Skull")

eyes = join([sphere("Eye", (0.34 * sx, -0.5, 0.33), 0.072) for sx in (1, -1)], "Eyes")

jaw = paint(blob("Jaw", [
    E((0, 0.05, -0.06), 0.36, (0.95, 0.85, 0.42)), E((0, -0.5, -0.1), 0.38, (0.72, 1.55, 0.3)),
    E((0, -1.08, -0.07), 0.2, (0.8, 1.1, 0.45)),
    E((0.36, 0.08, -0.02), 0.18, (0.7, 1.0, 0.8)), E((-0.36, 0.08, -0.02), 0.18, (0.7, 1.0, 0.8)),
]), scales_fn(BACK, SIDE, BELLY, seed=4.0))
decimate(jaw, 2500)
teeth = []
for sx in (1, -1):
    for i in range(4):
        y = -1.12 + i * 0.17
        teeth.append(paint(cone("JawFang", (0.12 * sx, y, 0.0), (0.12 * sx, y, 0.16 - i * 0.015), 0.03), bone_fn(5.0)))
jaw = join([jaw] + teeth, "Jaw")
bpy.context.scene.cursor.location = (0, 0.15, -0.06)
bpy.ops.object.select_all(action="DESELECT")
jaw.select_set(True)
bpy.context.view_layer.objects.active = jaw
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")

hide_m = material("WyrdAcc_NidhoggScales", rough=0.45)
for o in (skull, jaw):
    set_material(o, hide_m)
set_material(eyes, material("WyrdAcc_Glow_Eyes", glow=(1.0, 0.45, 0.02)))
export([skull, jaw, eyes], "Nidhogg.fbx")

# ---------------------------------------------------------------- body coil (one segment, origin at its centre)

coil_body = paint(blob("Coil", [
    E((0, 0, 0), 0.55, (1.0, 1.2, 0.85)), E((0, 0.35, 0.05), 0.38, (0.9, 0.8, 0.8)), E((0, -0.35, -0.02), 0.4, (0.9, 0.8, 0.8)),
], voxel=0.025), scales_fn(BACK, SIDE, BELLY, scale=7.0, seed=9.0))
decimate(coil_body, 2200)
cs = []
for i, y in enumerate((-0.25, 0.1, 0.42)):
    cs.append(cone("Dorsal", (0, y, 0.38), (0, y + 0.25, 0.95 - i * 0.12), 0.12 - i * 0.015, bend=-0.3))
for sx in (1, -1):
    for y in (-0.2, 0.25):
        cs.append(cone("SideSpike", (0.48 * sx, y, 0.12), (0.8 * sx, y + 0.2, 0.3), 0.05))
for s in cs:
    paint(s, bone_fn(7.0))
coil = join([coil_body] + cs, "Coil")
set_material(coil, hide_m)
coil.location = (0, 0, 0)
export([coil], "NidhoggCoil.fbx")

# ---------------------------------------------------------------- preview: head over three coils
jaw.rotation_euler = (math.radians(18), 0, 0)
for i, (x, y) in enumerate([(0.0, 2.6), (0.9, 3.8), (1.9, 4.6)]):
    c = coil.copy()
    c.data = coil.data
    link(c)
    c.location = (x, y, -0.35)
    c.scale = (0.9 - i * 0.1,) * 3
preview("nidhogg", (0.4, 0.8, 0.2), [((3.2, -6.0, 6.4), "game"), ((7.5, -1.0, 1.3), "side")], rim=(0.55, 1.0, 0.35), ground_z=-0.85)
