import bpy, sys, os
argv = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=argv[0])
os.makedirs(argv[1], exist_ok=True)
for m in bpy.data.materials:
    if not m.use_nodes:
        continue
    for n in m.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image:
            tgt = "_".join(l.to_socket.name.replace(" ", "") for l in n.outputs[0].links)
            img = n.image.copy()
            img.scale(256, 256)
            p = os.path.join(argv[1], f"{m.name}_{n.image.name}_{tgt}.png".replace("/", "_"))
            img.filepath_raw = p; img.file_format = 'PNG'; img.save()
            print("TEX", p, "alpha:", n.image.alpha_mode, "canales", n.image.channels)
