# Renderiza pares "actual vs nuevo" de modelos con la misma camara/luz (vista 3/4 desde el frente).
# Uso: blender -b -P render_compare.py -- <dir_actual_Models> <dir_nuevo_Models> <salida_png_dir> id1,id2,...
# Busca <dir>/Items/<id>.glb o <dir>/Env/<id>.glb. Frente del modelo = -Y (convencion del proyecto).
import bpy, sys, os, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
CUR, NEW, OUT, IDS = argv[0], argv[1], argv[2], argv[3].split(",")
os.makedirs(OUT, exist_ok=True)


def find(base, i):
    for d in ("Items", "Env"):
        p = os.path.join(base, d, i + ".glb")
        if os.path.exists(p):
            return p
    return None


def render(path, out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    bpy.ops.import_scene.gltf(filepath=path)
    bpy.context.view_layer.update()
    # Workbench muestra el nodo de imagen ACTIVO: que sea el del color base (no emision/normal)
    for m in bpy.data.materials:
        if m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == 'TEX_IMAGE' and any(l.to_socket.name == 'Base Color' for l in n.outputs[0].links):
                    m.node_tree.nodes.active = n
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in meshes:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    ctr = (mn + mx) / 2
    rad = max((mx - mn).length / 2, 1e-4)
    cam_d = bpy.data.cameras.new("c"); cam_d.lens = 50
    cam = bpy.data.objects.new("c", cam_d); sc.collection.objects.link(cam)
    dirv = Vector((0.75, -1.0, 0.55)).normalized()          # frente-derecha-arriba
    dist = rad / math.tan(math.radians(36) / 2) * 1.05
    cam.location = ctr + dirv * dist
    cam.rotation_euler = (-dirv).to_track_quat('-Z', 'Y').to_euler()
    cam_d.clip_start = dist * 0.01; cam_d.clip_end = dist * 10
    sc.camera = cam
    sc.render.engine = 'BLENDER_WORKBENCH'
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'
    sh.show_cavity = True; sh.cavity_type = 'WORLD'; sh.show_shadows = False
    sh.show_specular_highlight = True
    sc.world = bpy.data.worlds.new('w'); sc.world.color = (0.32, 0.33, 0.36)
    sc.render.resolution_x = sc.render.resolution_y = 520
    sc.render.film_transparent = False
    sc.render.filepath = out
    bpy.ops.render.render(write_still=True)
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    return tris


for i in IDS:
    for tag, base in (("actual", CUR), ("nuevo", NEW)):
        p = find(base, i)
        if not p:
            print("FALTA", tag, i); continue
        t = render(p, os.path.join(OUT, f"{i}_{tag}.png"))
        print(f"REN {i} {tag} tris={t} mb={os.path.getsize(p)/1048576:.2f}")
