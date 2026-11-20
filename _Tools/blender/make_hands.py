# Manos del jugador (abierta y cerrada, derecha e izquierda) a partir del modelo con huesos
# "First Person hands rigged" (davidfischer, CC-BY). Salida: mano_{r,l}_{abierta,cerrada}.glb
# Marco de salida: dedos hacia -Y (=> +Z en Unity), palma hacia -Z (abajo), origen en el centro de la palma, metros.
# La muneca se corta con un plano limpio (bisect) y se tapa.
# Uso: blender -b -P make_hands.py -- <manos_rig.glb> <dir_salida>
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix, Quaternion

SRC, DST = sys.argv[sys.argv.index("--") + 1:][:2]
os.makedirs(DST, exist_ok=True)
FINGERS = ["thumb", "f_index", "f_middle", "f_ring", "f_pinky"]
WRIST_CUT = 0.085      # metros detras del centro de la palma


def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=SRC)
    arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    skin = max((o for o in bpy.context.scene.objects if o.type == 'MESH' and o.vertex_groups), key=lambda o: len(o.data.vertices))
    # Los huesos "_end" (puntas sin articulacion) estan en posiciones raras y arrastran vertices como espinas
    # cuando se doblan los dedos: se les quita toda influencia.
    allv = [v.index for v in skin.data.vertices]
    for g in list(skin.vertex_groups):
        if "_end" in g.name:
            g.remove(allv)
    return arm, skin


def bone(arm, prefix, n, side):
    key = "%s.%02d.%s" % (prefix, n, side)
    return next((b for b in arm.pose.bones if b.name.startswith(key) and "_end" not in b.name), None)


def evaluated(skin):
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    eo = skin.evaluated_get(dg)
    me = eo.to_mesh()
    co = [eo.matrix_world @ v.co for v in me.vertices]
    eo.to_mesh_clear()
    return co


def group_centroid(skin, co, name_prefix):
    idx = [g.index for g in skin.vertex_groups if g.name.startswith(name_prefix) and "_end" not in g.name]
    acc = Vector(); w = 0.0
    for v in skin.data.vertices:
        for g in v.groups:
            if g.group in idx and g.weight > 0.3:
                acc += co[v.index] * g.weight; w += g.weight
    return acc / w if w > 0 else None


BASE = {}


def reset_pose(arm):
    # "reiniciar" = volver a la pose con la que viene el modelo (no a la identidad: eso desarma el brazo)
    if not BASE:
        for pb in arm.pose.bones:
            if pb.rotation_mode != 'QUATERNION':
                q = pb.matrix_basis.to_quaternion(); pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = q
            BASE[pb.name] = pb.rotation_quaternion.copy()
    for pb in arm.pose.bones:
        pb.rotation_quaternion = BASE[pb.name].copy()
    bpy.context.view_layer.update()


def set_pose(arm, skin, side, curl, thumb=0.7):
    """curl: grados por falange. Para cada dedo se elige el eje/sentido que acerca la punta a la palma."""
    reset_pose(arm)
    if curl <= 0:
        return
    palm = group_centroid(skin, evaluated(skin), "hand." + side)
    for f in FINGERS:
        # el pulgar solo dobla sus dos falanges de la punta: girar la base arrastraba piel de la muneca (espinas)
        chain = [b for b in (bone(arm, f, n, side) for n in ((2, 3) if f == "thumb" else (1, 2, 3))) if b]
        if not chain:
            continue
        amount = curl * (thumb if f == "thumb" else 1.0)
        best = None
        for axis in (Vector((1, 0, 0)), Vector((0, 0, 1))):
            for sgn in (1, -1):
                for b in chain:
                    b.rotation_quaternion = BASE[b.name] @ Quaternion(axis, math.radians(amount) * sgn)
                bpy.context.view_layer.update()
                tip = group_centroid(skin, evaluated(skin), "%s.03.%s" % (f, side))
                d = (tip - palm).length if tip is not None else 1e9
                if best is None or d < best[0]:
                    best = (d, axis.copy(), sgn)
        for b in chain:
            b.rotation_quaternion = BASE[b.name] @ Quaternion(best[1], math.radians(amount) * best[2])
        bpy.context.view_layer.update()


def side_mask(skin, side):
    """Vertices de la mano y del tramo de antebrazo pegado a ella (no la otra cadena de huesos del brazo,
    cuyos vertices quedaban como espinas saliendo de la muneca)."""
    pre = ("hand.", "palm_", "f_", "thumb.", "forearm.%s.001" % side)
    mine = {g.index for g in skin.vertex_groups if g.name.startswith(pre) and ("." + side) in g.name and "_end" not in g.name}
    keep = set()
    for v in skin.data.vertices:
        tot = sum(g.weight for g in v.groups)
        a = sum(g.weight for g in v.groups if g.group in mine)
        if tot > 0 and a / tot > 0.6:
            keep.add(v.index)
    return keep


def frame_for(arm, skin, side):
    set_pose(arm, skin, side, 0)
    co = evaluated(skin)
    palm = group_centroid(skin, co, "hand." + side)
    tip_open = group_centroid(skin, co, "f_middle.03." + side)
    knuckle = group_centroid(skin, co, "f_middle.01." + side)
    set_pose(arm, skin, side, 60)
    tip_grip = group_centroid(skin, evaluated(skin), "f_middle.03." + side)
    reset_pose(arm)
    fwd = (tip_open - palm).normalized()
    pd = (tip_grip - tip_open)
    pd = (pd - fwd * pd.dot(fwd)).normalized()
    right = fwd.cross(pd).normalized()
    origin = (palm + knuckle) * 0.5
    length = (tip_open - palm).length
    # rotacion propia (sin espejo): right -> X, fwd -> -Y, palma -> -Z  (det = +1 porque right = fwd x pd)
    src = Matrix((right, fwd, pd)).transposed()
    dstm = Matrix(((1, 0, 0), (0, -1, 0), (0, 0, -1))).transposed()
    # dstm tiene det +1 (dos ejes invertidos); src es ortonormal a derechas => R es rotacion pura
    R = (dstm @ src.inverted()).to_4x4()
    S = Matrix.Scale(0.165 / max(length, 1e-6), 4)
    return S @ R @ Matrix.Translation(-origin)


def build(skin, side, M):
    co = evaluated(skin)
    keep = side_mask(skin, side)
    bm = bmesh.new()
    vmap = {i: bm.verts.new(M @ co[i]) for i in keep}
    for p in skin.data.polygons:
        if all(i in keep for i in p.vertices):
            try:
                bm.faces.new([vmap[i] for i in p.vertices])
            except ValueError:
                pass
    # corte limpio de la muneca: se descarta todo lo que queda detras del plano y = WRIST_CUT
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, WRIST_CUT, 0), plane_no=(0, 1, 0), clear_outer=True, clear_inner=False)
    # tapa: abanico desde el centro del unico borde abierto (el corte)
    bedges = [e for e in bm.edges if e.is_boundary and abs(e.verts[0].co.y - WRIST_CUT) < 0.004 and abs(e.verts[1].co.y - WRIST_CUT) < 0.004]
    if bedges:
        bverts = list({v for e in bedges for v in e.verts})
        c = sum((v.co for v in bverts), Vector()) / len(bverts)
        cv = bm.verts.new(c)
        for e in bedges:
            try:
                bm.faces.new([e.verts[0], e.verts[1], cv])
            except ValueError:
                pass
    # fuera pedazos sueltos lejos de la mano
    # espinas (piel de la muneca mal pesada al pulgar): se recogen hacia sus vecinos sanos
    def is_bad(co):
        return co.y > 0.015 and co.z < -0.04
    for _ in range(12):
        bad = [v for v in bm.verts if is_bad(v.co)]
        if not bad:
            break
        for v in bad:
            ok = [e.other_vert(v).co for e in v.link_edges if not is_bad(e.other_vert(v).co)]
            if ok:
                c = sum(ok, Vector()) / len(ok)
                v.co = Vector((c.x, c.y, max(c.z, -0.038 if c.y > 0.015 else -0.066)))
            else:
                v.co.z = -0.038 if v.co.y > 0.015 else -0.066
    # vertices disparados (pesos de huesos "_end"): se recogen al promedio de sus vecinos sanos
    for _ in range(4):
        far = [v for v in bm.verts if v.co.length > 0.145]
        if not far:
            break
        for v in far:
            near = [e.other_vert(v).co for e in v.link_edges if e.other_vert(v).co.length <= 0.145]
            if near:
                v.co = sum(near, Vector()) / len(near)
    far = [v for v in bm.verts if v.co.length > 0.145]
    if far:
        bmesh.ops.delete(bm, geom=far, context='VERTS')
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("mano"); bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    return me


def export(me, name):
    bpy.ops.object.select_all(action='DESELECT')
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    mat = bpy.data.materials.new("piel_morty"); mat.use_nodes = True
    b = mat.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (0.96, 0.76, 0.6, 1)
    b.inputs["Roughness"].default_value = 0.65
    o.data.materials.append(mat)
    o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.gltf(filepath=os.path.join(DST, name + ".glb"), export_format='GLB', use_selection=True, export_apply=True, export_yup=True)
    mn = [min(v.co[i] for v in me.vertices) for i in range(3)]
    mx = [max(v.co[i] for v in me.vertices) for i in range(3)]
    print("HAND %s: %d tris, x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f" % (name, sum(len(p.vertices) - 2 for p in me.polygons), mn[0], mx[0], mn[1], mx[1], mn[2], mx[2]))
    bpy.data.objects.remove(o, do_unlink=True)


arm, skin = load()
for side, tag in (("R", "r"), ("L", "l")):
    M = frame_for(arm, skin, side)
    for curl, pose in ((12, "abierta"), (58, "cerrada")):
        set_pose(arm, skin, side, curl)
        export(build(skin, side, M), "mano_%s_%s" % (tag, pose))
