"""Print the rest-pose bones and mesh bounds of the boss base models (to fit accessories to them)."""
import bpy, sys, os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODELS = {
    "Barbarian": "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/Barbarian.fbx",
    "Mage": "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/Mage.fbx",
    "Wolf": "Assets/ThirdParty/Resources/Quaternius/Wolf.fbx",
}
WANT = ("head", "neck", "chest", "spine", "upperarm", "shoulder", "hand", "torso", "body", "hips", "back", "tail1")
for name, rel in MODELS.items():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, rel))
    print("==== %s" % name)
    for ob in bpy.context.scene.objects:
        if ob.type == "ARMATURE":
            mw = ob.matrix_world
            print("ARM %s scale=%s rot=%s" % (ob.name, tuple(round(v, 3) for v in ob.scale), tuple(round(v, 3) for v in ob.rotation_euler)))
            for b in ob.data.bones:
                if any(w in b.name.lower() for w in WANT):
                    h, t = mw @ b.head_local, mw @ b.tail_local
                    print("  BONE %-22s head=(%.3f, %.3f, %.3f) tail=(%.3f, %.3f, %.3f)" % (b.name, h.x, h.y, h.z, t.x, t.y, t.z))
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for ob in bpy.context.scene.objects:
        if ob.type == "MESH" and ob.visible_get():
            for c in ob.bound_box:
                w = ob.matrix_world @ Vector(c)
                lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
            if name != "Wolf" and not any(k in ob.name for k in ("Head", "Body", "Hat", "Cape")):
                continue
            bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
            mn = Vector(map(min, *bb)); mx = Vector(map(max, *bb))
            print("  MESH %-24s min=(%.2f, %.2f, %.2f) max=(%.2f, %.2f, %.2f)" % (ob.name, mn.x, mn.y, mn.z, mx.x, mx.y, mx.z))
    print("  ALL min=%s max=%s" % (tuple(round(v, 2) for v in lo), tuple(round(v, 2) for v in hi)))
