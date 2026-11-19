# Imprime dimensiones (Blender, Z arriba), triangulos, armaduras/huesos de cada GLB.
# Uso: blender -b -P inspect.py -- <carpeta> [filtro]
import bpy, sys, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src = argv[0]
flt = argv[1] if len(argv) > 1 else ""
for f in sorted(os.listdir(src)):
    if not f.endswith(".glb") or flt not in f:
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(src, f))
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in meshes:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    d = mx - mn
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    line = f"{f}: dims X{d.x:.3f} Y{d.y:.3f} Z{d.z:.3f} | min {tuple(round(v,2) for v in mn)} | objs {len(meshes)}"
    if arms:
        line += " | bones: " + ",".join(b.name for b in arms[0].data.bones)[:600]
    print("INFO", line)
