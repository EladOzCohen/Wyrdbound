"""
Shared toolkit for Wyrdbound's script-built models (run inside Blender: `blender --background --python ...`).

Shapes: metaball blobs rebuilt as clean meshes, tapered/curved cones (horns, spikes, fangs), tori and
cylinders. Colour: vertex colours (scales, fur, bone, iron), which the game's Wyrd/Lit shader multiplies in.
Accessories for existing rigs are modelled in the base model's own rest-pose space; each object is named
"<part>@<bone>" and the game re-parents it to that bone, so it follows the animation.
Materials: names containing "Glow" become emissive in the game; "WyrdAcc" ones are never re-tinted.
"""
import bpy, bmesh, math, os, random
from mathutils import Vector, Quaternion, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_MODELS = os.path.join(ROOT, "Assets", "Wyrdbound", "Resources", "Models")
OUT_PREVIEW = os.path.join(ROOT, "Builds", "blender_preview")

BONE = (0.80, 0.72, 0.55)
IRON = (0.20, 0.20, 0.22)
BLOOD = (0.32, 0.03, 0.03)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(7)
    return bpy.context.scene


def link(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


def E(co, r, size=(1, 1, 1), **kw):
    d = dict(co=co, r=r, size=size)
    d.update(kw)
    return d


def apply_mods(ob):
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    if old.users == 0:
        bpy.data.meshes.remove(old)
    for p in me.polygons:
        p.use_smooth = True
    return ob


def blob(name, elements, voxel=0.02, rough=0.02, grow=1.12, threshold=0.32, resolution=0.03, rough_scale=0.18):
    """Melt metaball ellipsoids into one smooth surface (negative ones carve) and rebuild it as a clean mesh."""
    mb = bpy.data.metaballs.new(name)
    mb.resolution = resolution
    mb.render_resolution = resolution
    mb.threshold = threshold
    ob = link(bpy.data.objects.new(name + "_mb", mb))
    for e in elements:
        el = mb.elements.new(type="ELLIPSOID")
        el.co = e["co"]
        el.radius = e["r"] * grow
        el.size_x, el.size_y, el.size_z = e.get("size", (1, 1, 1))
        if "rot" in e:
            el.rotation = Quaternion(e["rot"][0], e["rot"][1])
        el.stiffness = e.get("stiff", 2.0)
        el.use_negative = e.get("neg", False)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    out = link(bpy.data.objects.new(name, me))
    rm = out.modifiers.new("remesh", "REMESH")
    rm.mode = "VOXEL"
    rm.voxel_size = voxel
    if rough > 0:
        tex = bpy.data.textures.new(name + "_noise", "CLOUDS")
        tex.noise_scale = rough_scale
        tex.noise_depth = 2
        d = out.modifiers.new("rough", "DISPLACE")
        d.texture = tex
        d.strength = rough
        d.mid_level = 0.5
    return apply_mods(out)


def decimate(ob, faces):
    n = len(ob.data.polygons)
    if n > faces:
        dm = ob.modifiers.new("dec", "DECIMATE")
        dm.ratio = faces / n
        apply_mods(ob)
    return ob


def cone(name, base, tip, r0, bend=0.0, bend_axis=Vector((1, 0, 0)), segments=10, rings=8, r1=0.002):
    """A tapered, optionally curved spike from base to tip (horns, spikes, fangs, handles when r1 = r0)."""
    base, tip = Vector(base), Vector(tip)
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    axis = tip - base
    length = axis.length
    d = axis.normalized()
    ba = Vector(bend_axis)
    side = d.cross(ba).normalized() if abs(d.dot(ba.normalized())) < 0.99 else d.orthogonal().normalized()
    up = side.cross(d).normalized()
    ringsv = []
    for k in range(rings + 1):
        t = k / rings
        r = r0 * (1 - t) ** 0.9 + r1 * t if r1 > 0.01 else r0 * (1 - t) ** 0.9 + 0.002
        c = base + d * (length * t) + up * (bend * length * t * t)
        ringsv.append([bm.verts.new(c + side * math.cos(2 * math.pi * s / segments) * r + up * math.sin(2 * math.pi * s / segments) * r) for s in range(segments)])
    for k in range(rings):
        for s in range(segments):
            bm.faces.new((ringsv[k][s], ringsv[k][(s + 1) % segments], ringsv[k + 1][(s + 1) % segments], ringsv[k + 1][s]))
    bm.faces.new(list(reversed(ringsv[0])))
    if r1 > 0.01:
        bm.faces.new(ringsv[-1])
    bm.to_mesh(me)
    bm.free()
    ob = link(bpy.data.objects.new(name, me))
    for p in me.polygons:
        p.use_smooth = True
    return ob


def cylinder(name, a, b, r, segments=12):
    return cone(name, a, b, r, segments=segments, rings=2, r1=r)


def torus(name, loc, major, minor, rot=(0, 0, 0), segs=24, minor_segs=8):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=segs, minor_segments=minor_segs,
                                     location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for p in ob.data.polygons:
        p.use_smooth = True
    return ob


def sphere(name, loc, r, seg=20):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=seg, ring_count=max(8, seg // 2))
    ob = bpy.context.active_object
    ob.name = name
    for p in ob.data.polygons:
        p.use_smooth = True
    return ob


def colors(ob):
    me = ob.data
    if not me.color_attributes:
        me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    return me.color_attributes[0]


def paint(ob, fn):
    """fn(world_pos, world_normal) -> (r, g, b)."""
    me = ob.data
    col = colors(ob)
    mw = ob.matrix_world
    nm = mw.to_3x3()
    for poly in me.polygons:
        for li in poly.loop_indices:
            v = me.vertices[me.loops[li].vertex_index]
            c = fn(mw @ v.co, (nm @ v.normal).normalized())
            col.data[li].color = (c[0], c[1], c[2], 1.0)
    return ob


def scales_fn(back, side_c, belly, scale=8.5, seed=0.0):
    def f(p, n):
        under = max(0.0, min(1.0, (-n.z + 0.05) / 0.55))
        side = max(0.0, min(1.0, 1.0 - abs(n.z) * 1.6))
        dist, pts = noise.voronoi(p * scale + Vector((seed, seed, 0)))
        seam = max(0.0, min(1.0, (dist[1] - dist[0]) / 0.12))
        tone = 0.82 + 0.36 * noise.noise(pts[0] * 1.7)
        big = 1.0 + 0.25 * noise.noise(p * 2.2)
        base = [back[i] * (1 - side) + side_c[i] * side for i in range(3)]
        c = [base[i] * (1 - under) + belly[i] * under for i in range(3)]
        k = tone * big * (0.45 + 0.55 * seam) if under < 0.7 else big * (0.85 + 0.15 * seam)
        return [x * k for x in c]
    return f


def fur_fn(base, tip, streak=22.0, seed=0.0):
    """Fur: long streaks along Z with lighter tips on top."""
    def f(p, n):
        s = noise.noise(Vector((p.x * streak, p.y * streak, p.z * 3.0 + seed)))
        top = max(0.0, min(1.0, (n.z + 0.2) / 1.0))
        k = 0.7 + 0.45 * s
        return [(base[i] * (1 - top) + tip[i] * top) * k for i in range(3)]
    return f


def flat_fn(c, mottle=0.12, seed=1.0, freq=9.0):
    def f(p, n):
        k = 1.0 + mottle * noise.noise(p * freq + Vector((seed, 0, 0)))
        return [x * k for x in c]
    return f


def bone_fn(seed=2.0):
    """Old bone: pale with darker cracks and dirt in the hollows (facing down)."""
    def f(p, n):
        crack = abs(noise.noise(p * 14 + Vector((seed, 0, 0))))
        dirt = max(0.0, -n.z) * 0.35
        k = (0.78 + 0.22 * min(1.0, crack * 6)) * (1 - dirt)
        return [x * k for x in BONE]
    return f


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    return ob


def material(name, glow=None, rough=0.5, glow_strength=4.0):
    """Vertex-colour material for previews; exported names drive the game's material choice."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (1, 1, 1, 1)
    bsdf.inputs["Roughness"].default_value = rough
    if glow:
        bsdf.inputs["Emission Color"].default_value = (glow[0], glow[1], glow[2], 1)
        bsdf.inputs["Emission Strength"].default_value = glow_strength
        bsdf.inputs["Base Color"].default_value = (glow[0], glow[1], glow[2], 1)
    else:
        attr = nt.nodes.new("ShaderNodeVertexColor")
        nt.links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def set_material(ob, m):
    ob.data.materials.clear()
    ob.data.materials.append(m)
    return ob


def import_base(rel_path):
    """Import an existing game model (for fitting accessories and for previews). Returns its objects."""
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, rel_path))
    objs = [o for o in bpy.context.scene.objects if o not in before]
    # show the undeformed rest pose (some rigs' imported animation squashes the mesh in Blender)
    for o in objs:
        for m in getattr(o, "modifiers", []):
            if m.type == "ARMATURE":
                m.show_render = m.show_viewport = False
        if o.type == "ARMATURE" and o.animation_data:
            o.animation_data.action = None
        # some FBX materials import as transparent; previews need them solid
        if o.type == "MESH":
            for m in o.data.materials:
                if m is None:
                    continue
                if hasattr(m, "blend_method"):
                    m.blend_method = "OPAQUE"
                if hasattr(m, "surface_render_method"):
                    m.surface_render_method = "DITHERED"
                if m.use_nodes:
                    for n in m.node_tree.nodes:
                        if n.type == "BSDF_PRINCIPLED":
                            for l in list(n.inputs["Alpha"].links):
                                m.node_tree.links.remove(l)
                            n.inputs["Alpha"].default_value = 1.0
    return objs


def export(objs, filename):
    os.makedirs(OUT_MODELS, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    path = os.path.join(OUT_MODELS, filename)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             colors_type="SRGB", add_leaf_bones=False, object_types={"MESH"})
    faces = sum(len(o.data.polygons) for o in objs)
    print("EXPORTED %s (%d objects, %d faces, %d KB)" % (filename, len(objs), faces, os.path.getsize(path) // 1024))
    return path


def aim(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat("-Z", "Y").to_euler()


def preview(name, target, views, rim=(0.55, 0.9, 1.0), lens=60, ground_z=0.0, size=(900, 720)):
    """Render previews: views = [(camera_location, suffix), ...]."""
    scene = bpy.context.scene
    os.makedirs(OUT_PREVIEW, exist_ok=True)
    world = bpy.data.worlds.new("w")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.01, 0.013, 0.016, 1)

    def light(n, loc, energy, color, sz):
        ld = bpy.data.lights.new(n, "AREA")
        ld.energy, ld.color, ld.size = energy, color, sz
        ob = link(bpy.data.objects.new(n, ld))
        ob.location = loc
        aim(ob, target)
        return ob

    t = Vector(target)
    light("Key", t + Vector((3, -3, 4)), 900, (1.0, 0.9, 0.8), 3)
    light("Rim", t + Vector((-2.5, 3.2, 2.5)), 1100, rim, 2.5)
    light("Fill", t + Vector((-3.5, -3, 0.5)), 120, (0.4, 0.5, 0.85), 3.5)
    g = link(bpy.data.objects.new("Ground", bpy.data.meshes.new("g")))
    g.data.from_pydata([(-12, -12, ground_z), (12, -12, ground_z), (12, 12, ground_z), (-12, 12, ground_z)], [], [(0, 1, 2, 3)])
    gm = bpy.data.materials.new("GroundMat")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.04, 0.045, 0.05, 1)
    g.data.materials.append(gm)
    cam = link(bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")))
    cam.data.lens = lens
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = size
    try:
        scene.eevee.taa_render_samples = 48
    except Exception:
        pass
    shots = []
    for view in views:
        loc, suffix = view[0], view[1]
        tgt = view[2] if len(view) > 2 else target
        cam.location = loc
        aim(cam, tgt)
        p = os.path.join(OUT_PREVIEW, "%s_%s.png" % (name, suffix))
        scene.render.filepath = p
        bpy.ops.render.render(write_still=True)
        shots.append(p)
    print("RENDERED", shots)
    return shots
