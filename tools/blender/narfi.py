"""
Narfi, son of Loki: a human character built on an MPFB2 (MakeHuman) body, entirely by script.
Run:  blender --background --python tools/blender/narfi.py [-- body|dressed]
  body    = sculpted body + face + Unity-ready (Mixamo-named) skeleton only
  export  = dressed, then T-posed, baked and exported to Assets/Wyrdbound/Art/Narfi/Narfi.fbx (+ textures) for Unity
  dressed = + skin, eyes, brows, lashes, hair, and Norse gear cut from the body itself (so it fits and is weighted)
Renders front / side / back / face / game-angle previews to Builds/blender_preview/narfi_*.png.

Look: a young Norse warrior (early twenties), shirtless and hard-muscled, sharp cheekbones and jaw (his father's
face), auburn hair tied back, a teal scarf, leather harness and bracers, fur trim, and teal war-paint runes that
glow faintly: he is dead, and walking out of Hel.
"""
import sys, os, math
sys.path.append(os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, noise
from wyrdlib import *
from bl_ext.user_default.mpfb.services.humanservice import HumanService
from bl_ext.user_default.mpfb.services.targetservice import TargetService
from bl_ext.user_default.mpfb.services.locationservice import LocationService

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["dressed"]
STAGE = argv[0] if argv else "dressed"
DATA = LocationService.get_user_data()

reset()
macros = TargetService.get_default_macro_info_dict()
macros.update({"gender": 1.0, "age": 0.44, "muscle": 0.95, "weight": 0.5, "proportions": 0.85, "height": 0.62})
macros["race"] = {"asian": 0.0, "caucasian": 1.0, "african": 0.0}
human = HumanService.create_human(macro_detail_dict=macros)
human.name = "Narfi"

shape = {
    "chin-prominent-incr": 0.35, "chin-width-incr": 0.25, "chin-height-incr": 0.15,
    "l-cheek-bones-incr": 0.5, "r-cheek-bones-incr": 0.5, "l-cheek-volume-decr": 0.35, "r-cheek-volume-decr": 0.35,
    "eyebrows-angle-down": 0.45, "eyebrows-trans-down": 0.2, "forehead-trans-forward": 0.2,
    "nose-hump-incr": 0.25, "nose-scale-depth-incr": 0.15, "nose-point-width-decr": 0.2,
    "head-scale-horiz-decr": 0.1, "head-square": 0.3,
    "measure-shoulder-dist-incr": 0.35, "measure-waist-circ-decr": 0.35, "torso-muscle-pectoral-incr": 0.35,
    "torso-muscle-dorsi-incr": 0.45, "measure-neck-circ-incr": 0.3,
}
for name, w in shape.items():
    TargetService.load_target(human, TargetService.target_full_path(name), weight=w, name=name)

rig = HumanService.add_builtin_rig(human, "mixamo_unity")
h = human.dimensions.z


def bone_pos(name, tail=False):
    b = rig.data.bones["mixamorig:" + name]
    return rig.matrix_world @ (b.tail_local if tail else b.head_local)


if STAGE == "body":
    skin = material("SkinPreview")
    human.data.materials.clear()
    human.data.materials.append(skin)
else:
    HumanService.set_character_skin(os.path.join(DATA, "skins", "young_caucasian_male", "young_caucasian_male.mhmat"), human, skin_type="GAMEENGINE")
    for kind, path in (("Eyes", "eyes/high-poly/high-poly.mhclo"), ("Eyebrows", "eyebrows/eyebrow002/eyebrow002.mhclo"),
                       ("Eyelashes", "eyelashes/eyelashes02/eyelashes02.mhclo"), ("Hair", "hair/ponytail01/ponytail01.mhclo"),
                       ):
        full = os.path.join(DATA, path)
        if os.path.isfile(full):
            HumanService.add_mhclo_asset(full, human, asset_type=kind, subdiv_levels=0, material_type="GAMEENGINE")
        else:
            print("MISSING", full)

# ------------------------------------------------------------------ gear cut from the body

def body_copy():
    """A copy of the body with its shape (targets) baked in and no modifiers: the cloth for garments."""
    dg = bpy.context.evaluated_depsgraph_get()
    tmp = human.copy()
    tmp.data = human.data.copy()
    link(tmp)
    if tmp.data.shape_keys:
        bpy.context.view_layer.objects.active = tmp
        tmp.shape_key_add(name="mix", from_mix=True)
        keys = tmp.data.shape_keys.key_blocks
        mix = keys["mix"]
        for v, kp in zip(tmp.data.vertices, mix.data):
            v.co = kp.co
        tmp.shape_key_clear()
    tmp.modifiers.clear()
    return tmp


BASE = None


def garment(name, keep, offset, color, thickness=0.006, smooth=1, glow=None, rough=0.6):
    """Cut the faces whose vertices pass keep(world_co, normal, weights) out of the body, push them outward and
    give them thickness. They keep the body's skin weights, so they bend with the skeleton."""
    global BASE
    if BASE is None:
        BASE = body_copy()
    ob = BASE.copy()
    ob.data = BASE.data.copy()
    ob.name = name
    link(ob)
    me = ob.data
    names = {g.index: g.name.replace("mixamorig:", "") for g in ob.vertex_groups}
    body_idx = ob.vertex_groups["body"].index
    keepv = set()
    for v in me.vertices:
        w = {names[g.group]: g.weight for g in v.groups}
        if w.get("body", 0) <= 0:
            continue
        if keep(v.co, v.normal, w):
            keepv.add(v.index)
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    drop = [f for f in bm.faces if not all(v.index in keepv for v in f.verts)]
    bmesh.ops.delete(bm, geom=drop, context="FACES_ONLY")
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bm.normal_update()
    for v in bm.verts:
        v.co += v.normal * offset
    for _ in range(smooth):
        bmesh.ops.smooth_vert(bm, verts=bm.verts[:], factor=0.4, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bm.to_mesh(me)
    bm.free()
    if thickness > 0:
        so = ob.modifiers.new("solid", "SOLIDIFY")
        so.thickness = thickness
        so.offset = 1.0
        apply_mods(ob)
        me = ob.data
    for p in me.polygons:
        p.use_smooth = True
    paint(ob, color)
    set_material(ob, material("WyrdAcc_" + name, glow=glow, rough=rough) if glow else material("WyrdAcc_" + name, rough=rough))
    if glow:
        set_material(ob, material("WyrdAcc_Glow_" + name, glow=glow))
    print("GARMENT", name, len(me.vertices), "verts")
    return ob


def bind(ob, weights_from=None):
    """Skin a separately-built piece (fur tufts, buckles) to the rig: copy weights from the body by proximity."""
    dt = ob.modifiers.new("weights", "DATA_TRANSFER")
    dt.object = BASE
    dt.use_vert_data = True
    dt.data_types_verts = {"VGROUP_WEIGHTS"}
    dt.vert_mapping = "NEAREST"
    bpy.context.view_layer.objects.active = ob
    for g in BASE.vertex_groups:
        if g.name not in ob.vertex_groups:
            ob.vertex_groups.new(name=g.name)
    apply_mods(ob)
    return ob


def wrap_on_body(ob, offset, thickness, color, mat, project=None):
    """Snap a strip/ring onto the body's surface (clean edges, unlike a cut), thicken, colour and skin it."""
    global BASE
    if BASE is None:
        BASE = body_copy()
    sw = ob.modifiers.new("snap", "SHRINKWRAP")
    sw.target = BASE
    if project:
        sw.wrap_method = "PROJECT"
        sw.use_project_x, sw.use_project_y, sw.use_project_z = project
        sw.use_negative_direction = sw.use_positive_direction = True
    else:
        sw.wrap_method = "NEAREST_SURFACEPOINT"
    sw.offset = offset
    sm = ob.modifiers.new("smooth", "SMOOTH")
    sm.factor = 0.5
    sm.iterations = 2
    if thickness > 0:
        so = ob.modifiers.new("solid", "SOLIDIFY")
        so.thickness = thickness
        so.offset = 1.0
    apply_mods(ob)
    paint(ob, color)
    set_material(ob, mat)
    bind(ob)
    print("WRAPPED", ob.name, len(ob.data.vertices), "verts")
    return ob


def strip(name, pts, width, steps=10):
    """A flat ribbon following a path (to be wrapped onto the body). Width runs across the body surface."""
    dense = []
    for i in range(len(pts) - 1):
        for k in range(steps):
            dense.append(pts[i].lerp(pts[i + 1], k / steps))
    dense.append(pts[-1])
    verts, faces = [], []
    for i, p in enumerate(dense):
        t = (dense[min(i + 1, len(dense) - 1)] - dense[max(i - 1, 0)]).normalized()
        out = Vector((p.x, p.y, 0)) - Vector((0, 0.02, 0))
        out = out.normalized() if out.length > 1e-4 else Vector((0, -1, 0))
        s = t.cross(out).normalized() * (width * 0.5)
        verts += [p - s, p + s]
        if i > 0:
            a = 2 * (i - 1)
            faces.append((a, a + 1, a + 3, a + 2))
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    sub = link(bpy.data.objects.new(name, me))
    m = sub.modifiers.new("sub", "SUBSURF")
    m.levels = 1
    apply_mods(sub)
    return sub


def ring(name, center, axis, radius, height, segs=40, rows=4):
    """An open tube around an axis (belts, arm bands), to be wrapped onto the body."""
    axis = axis.normalized()
    u = axis.orthogonal().normalized()
    v = axis.cross(u)
    verts, faces = [], []
    for r in range(rows + 1):
        off = axis * (height * (r / rows - 0.5))
        for s in range(segs):
            a = 2 * math.pi * s / segs
            verts.append(center + off + (u * math.cos(a) + v * math.sin(a)) * radius)
    for r in range(rows):
        for s in range(segs):
            a, b = r * segs + s, r * segs + (s + 1) % segs
            faces.append((a, b, b + segs, a + segs))
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    return link(bpy.data.objects.new(name, me))


def tuft(p0, d, length, width, droop=0.5, flat=None):
    """A fur tuft: a curved, flattened, tapering leaf."""
    t = cone("Fur", p0, p0 + d.normalized() * length, width, bend=-droop, segments=6, rings=5)
    if flat is not None:
        fa = flat.normalized()
        for v in t.data.vertices:
            rel = v.co - p0
            v.co -= fa * rel.dot(fa) * 0.6
    return t


LEATHER = (0.24, 0.13, 0.07)
DARK_LEATHER = (0.12, 0.07, 0.045)
WOOL = (0.13, 0.12, 0.11)
TEAL = (0.06, 0.32, 0.28)
FUR = (0.42, 0.36, 0.28)
RUNE = (0.25, 1.0, 0.85)


def leather_fn(base, seed=0.0):
    def f(p, n):
        k = 0.8 + 0.3 * noise.noise(p * 25 + Vector((seed, 0, 0))) + 0.15 * noise.noise(p * 6)
        return [c * k for c in base]
    return f


if STAGE in ("dressed", "export"):
    hips, spine, chest = bone_pos("Hips"), bone_pos("Spine1"), bone_pos("Spine2")
    neck, head = bone_pos("Neck"), bone_pos("Head")
    knee_l, ankle_l = bone_pos("LeftLeg"), bone_pos("LeftFoot")
    sh_l, sh_r = bone_pos("LeftArm"), bone_pos("RightArm")
    elbow_l, wrist_l = bone_pos("LeftForeArm"), bone_pos("LeftHand")
    elbow_r, wrist_r = bone_pos("RightForeArm"), bone_pos("RightHand")
    waist_z = hips.z + (spine.z - hips.z) * 0.55
    print("LANDMARKS hips %.2f waist %.2f knee %.2f ankle %.2f shoulderL %s" % (hips.z, waist_z, knee_l.z, ankle_l.z, tuple(round(c, 2) for c in sh_l)))
    gear = []

    def legs(w):
        return w.get("LeftUpLeg", 0) + w.get("RightUpLeg", 0) + w.get("LeftLeg", 0) + w.get("RightLeg", 0) + w.get("Hips", 0) + w.get("LeftButtock", 0) + w.get("RightButtock", 0)

    gear.append(garment("Trousers", lambda c, n, w: legs(w) > 0.5 and ankle_l.z + 0.18 < c.z < waist_z - 0.01, 0.009,
                        leather_fn(WOOL, 1.0), thickness=0.004))
    # soft leather boots (turnshoes) from the toes to below the knee, with a darker sole band
    def boot_fn(p, n):
        k = 0.8 + 0.3 * noise.noise(p * 25 + Vector((2.0, 0, 0))) + 0.15 * noise.noise(p * 6)
        base = DARK_LEATHER if p.z < 0.035 else LEATHER
        return [c * k for c in base]
    gear.append(garment("Boots", lambda c, n, w: c.z < knee_l.z - 0.06, 0.018, boot_fn, thickness=0.007, smooth=6))
    # leather thongs criss-crossing the shins
    for side in ():   # shin thongs: disabled (read as loose wires from the game camera)
        for i in range(4):
            z = ankle_l.z + 0.1 + i * 0.075
            ax = Vector((0.18 * (1 if i % 2 else -1), 0.0, 1.0))
            gear.append(wrap_on_body(ring("Thong%d%d" % (side + 1, i), Vector((knee_l.x * side, knee_l.y, z)), ax, 0.09, 0.022, segs=30, rows=3),
                                     0.027, 0.004, leather_fn(DARK_LEATHER, 9.0 + i), material("WyrdAcc_Leather", rough=0.55)))
    belt_z = waist_z - 0.025
    leather_m = material("WyrdAcc_Leather", rough=0.55)
    gear.append(wrap_on_body(ring("Belt", Vector((0, hips.y, belt_z)), Vector((0, 0, 1)), 0.26, 0.07), 0.016, 0.012,
                             leather_fn(DARK_LEATHER, 3.0), leather_m))

    # a strap across the chest, over the left shoulder down to the right hip, front and back
    top = Vector((sh_l.x * 0.62, sh_l.y + 0.01, sh_l.z + 0.1))
    for name, y in (("StrapFront", -0.4), ("StrapBack", 0.4)):
        # a straight diagonal (seen from the front) projected onto the body; it ends tucked under the belt
        end = Vector((-0.1, y, belt_z + 0.02))
        start = Vector((sh_l.x * 0.5, y, chest.z + 0.12))
        path = [top] + [start.lerp(end, k / 6) for k in range(7)]
        gear.append(wrap_on_body(strip(name, path, 0.055), 0.012, 0.007, leather_fn(DARK_LEATHER, 4.0), leather_m, project=(False, True, False)))

    # leather plate on the left shoulder
    gear.append(garment("Pauldron", lambda c, n, w: (w.get("LeftShoulder", 0) + w.get("LeftArm", 0)) > 0.4 and c.z > sh_l.z - 0.1 and c.x > sh_l.x - 0.12,
                        0.03, leather_fn(LEATHER, 5.0), thickness=0.014, smooth=2))

    # bracers on both forearms
    for side, el, wr in (("L", elbow_l, wrist_l), ("R", elbow_r, wrist_r)):
        ax = (wr - el)
        ln = ax.length
        axn = ax.normalized()
        gear.append(garment("Bracer" + side, lambda c, n, w, el=el, axn=axn, ln=ln: 0.3 < (c - el).dot(axn) / ln < 0.95 and (c - el - axn * (c - el).dot(axn)).length < 0.09,
                            0.016, leather_fn(LEATHER, 6.0), thickness=0.008, smooth=2))

    # teal scarf wrapped around the neck and over the collarbones
    gear.append(garment("Scarf", lambda c, n, w: neck.z - 0.11 < c.z < neck.z + 0.05 and (w.get("Neck", 0) + w.get("Spine2", 0) + w.get("LeftShoulder", 0) + w.get("RightShoulder", 0)) > 0.3 and abs(c.x) < 0.17,
                        0.03, leather_fn(TEAL, 7.0), thickness=0.012, smooth=3, rough=0.85))

    # glowing war paint: a stripe over the left eye and rune bands on the right upper arm
    eye_l = bone_pos("LeftEye")
    paint_m = material("WyrdAcc_Glow_Runes", glow=(0.1, 0.75, 0.62), glow_strength=1.2)
    face_y = min(v.co.y for v in human.data.vertices) + 0.0
    gear.append(wrap_on_body(strip("PaintFace", [Vector((eye_l.x + 0.004, eye_l.y - 0.05, eye_l.z + 0.075)),
                                                  Vector((eye_l.x, eye_l.y - 0.05, eye_l.z)),
                                                  Vector((eye_l.x + 0.01, eye_l.y - 0.04, eye_l.z - 0.085))], 0.017, steps=6),
                             0.0018, 0.0, flat_fn((0.2, 0.9, 0.75), 0.15), paint_m))
    ua = bone_pos("RightArm")
    uax = (elbow_r - ua)
    for i, t in enumerate((0.42, 0.56, 0.7)):
        gear.append(wrap_on_body(ring("PaintArm%d" % i, ua + uax * t, uax, 0.07, 0.014 if i != 1 else 0.026, segs=28, rows=2),
                                 0.0018, 0.0, flat_fn((0.2, 0.9, 0.75), 0.15), paint_m))

    # stubble: a short beard along the jaw and chin, below the mouth
    jaw = bone_pos("Jaw")
    lips = [v.co for v in BASE.data.vertices if any(BASE.vertex_groups[g.group].name == "lips" and g.weight > 0.5 for g in v.groups)] if BASE else []
    mouth_z = min(c.z for c in lips) if lips else jaw.z + 0.02
    if False: gear.append(garment("Stubble", lambda c, n, w: w.get("Head", 0) + w.get("Jaw", 0) > 0.6 and jaw.z - 0.1 < c.z < mouth_z - 0.006 and c.y < jaw.y - 0.005 and n.z < 0.6,
                        0.0022, lambda p, n: [0.13 + 0.03 * noise.noise(p * 400), 0.065, 0.035], thickness=0, smooth=1, rough=0.95))

    # fur: tufts along the top of the leg wraps, the bracers and over the pauldron
    import random as _r
    _r.seed(11)
    tufts = []
    down = Vector((0, 0, -1))
    # a ruff of fur over the top of each leg wrap, hanging down and outward
    for cx in (knee_l.x, -knee_l.x):
        for layer in range(3):
            for i in range(30):
                a2 = 2 * math.pi * (i + 0.5 * layer) / 30 + _r.uniform(-0.08, 0.08)
                out = Vector((math.cos(a2), math.sin(a2), 0))
                p0 = Vector((cx, knee_l.y, knee_l.z - 0.05 - layer * 0.02)) + out * (0.07 + 0.01 * layer)
                tufts.append(tuft(p0, out * 0.5 + down, _r.uniform(0.05, 0.1), 0.017, droop=-0.7, flat=out))
    # a shaggy wolf pelt thrown over the left shoulder: a lumpy fur mass with a fringe of short tufts
    sx0, sy0, sz0 = sh_l.x + 0.02, sh_l.y, sh_l.z + 0.07
    pelt_el = [E((sx0 + dx, sy0 + dy, sz0 + dz), r, sz) for dx, dy, dz, r, sz in (
        (0.0, 0.0, 0.0, 0.12, (1.3, 1.2, 0.5)), (0.08, -0.04, -0.02, 0.09, (1.0, 1.0, 0.55)), (0.08, 0.05, -0.02, 0.09, (1.0, 1.0, 0.55)),
        (-0.07, -0.06, 0.0, 0.08, (1.0, 1.0, 0.5)), (-0.07, 0.07, 0.0, 0.08, (1.0, 1.0, 0.5)), (0.14, 0.0, -0.05, 0.07, (0.8, 1.2, 0.6)))]
    pelt = paint(blob("Pelt", pelt_el, voxel=0.008, rough=0.018, rough_scale=0.025), fur_fn((0.2, 0.16, 0.12), FUR, streak=90))
    decimate(pelt, 2500)
    set_material(pelt, material("WyrdAcc_Fur", rough=0.95))
    bind(pelt)
    gear.append(pelt)
    for i in range(34):
        a2 = 2 * math.pi * i / 34 + _r.uniform(-0.05, 0.05)
        out = Vector((math.cos(a2) * 1.25, math.sin(a2), 0)).normalized()
        p0 = Vector((sx0 + 0.03, sy0, sz0 - 0.02)) + Vector((math.cos(a2) * 0.15, math.sin(a2) * 0.12, 0))
        tufts.append(tuft(p0, out * 0.5 + down, _r.uniform(0.04, 0.08), 0.016, droop=-0.6, flat=out))
    # cuffs of fur where the bracers start
    for el2, wr2 in ((elbow_l, wrist_l), (elbow_r, wrist_r)):
        axn2 = (wr2 - el2).normalized()
        c0 = el2 + (wr2 - el2) * 0.3
        u = axn2.orthogonal().normalized()
        v2 = axn2.cross(u)
        for i in range(28):
            a2 = 2 * math.pi * i / 28
            d = u * math.cos(a2) + v2 * math.sin(a2)
            tufts.append(tuft(c0 + d * 0.062, d * 0.6 - axn2, _r.uniform(0.035, 0.07), 0.013, droop=-0.5, flat=d))
    for t in tufts:
        paint(t, fur_fn((0.22, 0.18, 0.14), FUR, streak=60))
    fur = join(tufts, "Fur")
    set_material(fur, material("WyrdAcc_Fur", rough=0.9))
    bind(fur)
    gear.append(fur)

    # a leather pouch hanging from the belt on the right hip
    hip_r = Vector((-0.17, hips.y - 0.02, belt_z - 0.07))
    pouch = paint(blob("Pouch", [E((hip_r.x, hip_r.y, hip_r.z), 0.06, (0.7, 0.55, 1.0)), E((hip_r.x, hip_r.y - 0.005, hip_r.z + 0.05), 0.045, (0.8, 0.6, 0.35))],
                       voxel=0.006, rough=0.004, rough_scale=0.04), leather_fn(LEATHER, 12.0))
    decimate(pouch, 900)
    set_material(pouch, material("WyrdAcc_Leather", rough=0.55))
    bind(pouch)
    gear.append(pouch)

    # buckle on the belt, front
    belt_ob = next(g for g in gear if g.name == "Belt")
    front = min((belt_ob.matrix_world @ v.co for v in belt_ob.data.vertices if abs(v.co.x + 0.05) < 0.02), key=lambda c: c.y)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(front.x, front.y - 0.008, belt_z))
    buck = bpy.context.active_object
    buck.name = "Buckle"
    buck.scale = (0.075, 0.016, 0.062)
    bpy.ops.object.transform_apply(scale=True)
    bev = buck.modifiers.new("bev", "BEVEL")
    bev.width = 0.008
    bev.segments = 2
    apply_mods(buck)
    paint(buck, flat_fn((0.42, 0.42, 0.45), 0.15))
    set_material(buck, material("WyrdAcc_Iron", rough=0.3))
    bind(buck)
    gear.append(buck)

    for g in gear:
        mod = g.modifiers.new("rig", "ARMATURE")
        mod.object = rig
        g.parent = rig

    # the hair: Loki's line runs red
    for ob in bpy.context.scene.objects:
        tint = (0.75, 0.32, 0.16, 1) if "hair" in ob.name.lower() else (0.55, 0.34, 0.2, 1) if "shoe" in ob.name.lower() else None
        if ob.type == "MESH" and tint:
            for m in ob.data.materials:
                if m and m.use_nodes:
                    for nd in m.node_tree.nodes:
                        if nd.type == "BSDF_PRINCIPLED":
                            src = nd.inputs["Base Color"].links[0].from_node if nd.inputs["Base Color"].links else None
                            mix = m.node_tree.nodes.new("ShaderNodeMix")
                            mix.data_type = "RGBA"
                            mix.blend_type = "MULTIPLY"
                            mix.inputs[0].default_value = 1.0
                            mix.inputs[7].default_value = tint
                            if src:
                                m.node_tree.links.new(src.outputs[0], mix.inputs[6])
                            m.node_tree.links.new(mix.outputs[2], nd.inputs["Base Color"])

    if BASE is not None:
        bpy.data.objects.remove(BASE)
        BASE = None

# ------------------------------------------------------------------ export for Unity

def bake_body(ob):
    """Freeze the targets (shape keys) and apply every modifier but the armature (drops the helper geometry)."""
    bpy.context.view_layer.objects.active = ob
    if ob.data.shape_keys:
        ob.shape_key_add(name="mix", from_mix=True)
        mix = ob.data.shape_keys.key_blocks["mix"]
        for v, kp in zip(ob.data.vertices, mix.data):
            v.co = kp.co
        ob.shape_key_clear()
    for m in list(ob.modifiers):
        if m.type != "ARMATURE":
            with bpy.context.temp_override(object=ob, active_object=ob):
                bpy.ops.object.modifier_move_to_index(modifier=m.name, index=0)
                bpy.ops.object.modifier_apply(modifier=m.name)


def t_pose():
    """Raise the arms from MakeHuman's A-pose to a T-pose and make that the rest pose (Unity's Humanoid expects it)."""
    from mathutils import Matrix
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="POSE")
    for side, sx in (("Left", 1), ("Right", -1)):
        for b in ("Arm", "ForeArm", "Hand"):
            bpy.context.view_layer.update()
            pb = rig.pose.bones["mixamorig:" + side + b]
            m = pb.matrix.copy()
            d = (pb.tail - pb.head).normalized()
            q = d.rotation_difference(Vector((sx, 0, 0)))
            p = m.to_translation()
            pb.matrix = Matrix.Translation(p) @ q.to_matrix().to_4x4() @ Matrix.Translation(-p) @ m
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")
    dg = bpy.context.evaluated_depsgraph_get()
    posed = {}
    for ob in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        posed[ob] = [v.co.copy() for v in me.vertices]
        ev.to_mesh_clear()
    for ob, cos in posed.items():
        for v, c in zip(ob.data.vertices, cos):
            v.co = c
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")


if STAGE == "export":
    import shutil
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for ob in meshes:
        if ob is human:
            bake_body(ob)
            continue
        if ob.data.shape_keys:
            bpy.context.view_layer.objects.active = ob
            ob.shape_key_clear()
        for m in list(ob.modifiers):
            if m.type != "ARMATURE":
                with bpy.context.temp_override(object=ob, active_object=ob):
                    bpy.ops.object.modifier_move_to_index(modifier=m.name, index=0)
                    bpy.ops.object.modifier_apply(modifier=m.name)
    t_pose()
    # FBX only carries a texture wired straight into Base Color: undo the preview hair tint (the game tints it)
    for m in bpy.data.materials:
        if not m.use_nodes:
            continue
        for nd in m.node_tree.nodes:
            if nd.type == "BSDF_PRINCIPLED" and nd.inputs["Base Color"].links:
                mix = nd.inputs["Base Color"].links[0].from_node
                if mix.type == "MIX" and mix.inputs[6].links:
                    m.node_tree.links.new(mix.inputs[6].links[0].from_socket, nd.inputs["Base Color"])
    out = os.path.join(ROOT, "Assets", "Wyrdbound", "Art", "Narfi")
    os.makedirs(out, exist_ok=True)
    # copy every image the materials use next to the FBX, so Unity finds them by name
    for img in bpy.data.images:
        src = bpy.path.abspath(img.filepath) if img.filepath else ""
        if src and os.path.isfile(src):
            shutil.copy2(src, os.path.join(out, os.path.basename(src)))
            print("TEX", os.path.basename(src))
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for ob in meshes:
        ob.select_set(True)
    path = os.path.join(out, "Narfi.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
                             colors_type="SRGB", add_leaf_bones=False, object_types={"ARMATURE", "MESH"},
                             use_armature_deform_only=True, bake_anim=False, path_mode="STRIP")
    print("EXPORTED Narfi.fbx", len(meshes), "meshes", sum(len(o.data.polygons) for o in meshes), "faces", os.path.getsize(path) // 1024, "KB")
    for ob in meshes:
        print("MESH", ob.name, len(ob.data.vertices), [m.name for m in ob.data.materials if m], [md.type for md in ob.modifiers])


t = (0, 0, h * 0.55)
preview("narfi_" + STAGE, t, [((0.6, -4.2, h * 0.62), "front"), ((4.2, -0.4, h * 0.6), "side"), ((-0.8, 4.2, h * 0.62), "back"),
                              ((0.35, -1.05, h * 0.94), "face", (0, 0, h * 0.91)), ((1.6, -3.0, h * 1.8), "game")], rim=(0.6, 0.9, 1.0), lens=50)
for o in bpy.context.scene.objects:
    if o.type == "MESH" and ("shoes" in o.name or "ponytail" in o.name):
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        print("DBG", o.name, "zmin %.3f zmax %.3f" % (min(v.z for v in bb), max(v.z for v in bb)), [m.name + ":" + str(getattr(m, "surface_render_method", "?")) for m in o.data.materials], [md.type for md in o.modifiers], "visible", o.visible_get())
