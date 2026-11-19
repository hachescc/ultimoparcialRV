# Lista materiales de un GLB: color base, metal/rugosidad, texturas conectadas y extensiones.
# Uso: blender -b -P inspect_mats.py -- <archivo.glb>
import bpy, sys
argv = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=argv[0])
for o in bpy.data.objects:
    if o.type == 'MESH':
        print("OBJ", o.name, [s.material.name if s.material else None for s in o.material_slots], "colattr:", [a.name for a in o.data.color_attributes])
for m in bpy.data.materials:
    if not m.use_nodes:
        print("MAT", m.name, "sin nodos"); continue
    b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    links = []
    for n in m.node_tree.nodes:
        if n.type == 'TEX_IMAGE':
            tgt = [f"{l.to_node.name}.{l.to_socket.name}" for l in n.outputs[0].links]
            links.append(f"{n.image.name if n.image else None}({n.image.size[0] if n.image else 0})->{tgt}")
    if b:
        bc = tuple(round(v, 2) for v in b.inputs['Base Color'].default_value)
        print("MAT", m.name, "base", bc, "metal", round(b.inputs['Metallic'].default_value, 2), "rough", round(b.inputs['Roughness'].default_value, 2), "tex", links)
    else:
        print("MAT", m.name, "nodos:", [n.type for n in m.node_tree.nodes], "tex", links)
