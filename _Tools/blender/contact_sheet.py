import bpy, sys, os, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out = argv[0], argv[1]
cols = int(argv[2]) if len(argv) > 2 else 8
side = len(argv) > 3 and argv[3] == "side"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
files = []
for dp, dn, fn in os.walk(src):
    files += [os.path.relpath(os.path.join(dp, f), src) for f in fn if f.lower().endswith(".glb")]
files.sort()
cell = 1.6

for i, f in enumerate(files):
    before = set(bpy.data.objects)
    try:
        bpy.ops.import_scene.gltf(filepath=os.path.join(src, f))
    except Exception as e:
        print("FAIL", f, e)
        continue
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == 'MESH']
    if not meshes:
        continue
    bpy.context.view_layer.update()
    mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
    for o in meshes:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    size = max((mx - mn).length, 1e-6)
    center = (mn + mx) / 2
    root = bpy.data.objects.new("root_" + f, None)
    scene.collection.objects.link(root)
    for o in new:
        if o.parent is None:
            o.parent = root
    s = 1.2 / size
    cx, cy = (i % cols) * cell, -(i // cols) * cell
    root.scale = (s, s, s)
    root.location = (Vector((0, -cx, cy)) if side else Vector((cx, 0, cy))) - center * s
    bpy.ops.object.text_add(location=((-0.8, -cx + 0.75, cy - 0.78) if side else (cx - 0.75, -0.8, cy - 0.78)))
    t = bpy.context.object
    t.data.body = f"{i}:{os.path.basename(f)[:22]}"
    t.data.size = 0.11
    t.rotation_euler = (math.radians(90), 0, math.radians(-90)) if side else (math.radians(90), 0, 0)

rows = math.ceil(len(files) / cols)
cam_data = bpy.data.cameras.new("cam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = max(cols, rows * 1.33) * cell * 1.02
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
if side:
    cam.location = (-20, -(cols - 1) * cell / 2, -(rows - 1) * cell / 2)
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
else:
    cam.location = ((cols - 1) * cell / 2, -20, -(rows - 1) * cell / 2)
    cam.rotation_euler = (math.radians(90), 0, 0)
scene.camera = cam
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.studio_light = 'paint.sl' if False else scene.display.shading.studio_light
scene.world = bpy.data.worlds.new('w'); scene.world.color = (0.35, 0.35, 0.38)
scene.display.shading.color_type = (argv[4] if len(argv) > 4 else 'TEXTURE')
scene.render.resolution_x = 2000
scene.render.resolution_y = int(2000 * rows * 1.0 / cols * 1.0) + 60
scene.render.film_transparent = False
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("DONE", len(files))
