# Genera por codigo los modelos que no se descargaron (objetos, easter eggs, exterior).
# Convencion: Blender +Z arriba, FRENTE hacia -Y (=> +Z en Unity). Medidas en metros.
# Uso: blender -b -P generate_models.py -- <dst_dir> [ids separados por coma]
import bpy, bmesh, sys, os, math, random
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
DST = argv[0]
ONLY = argv[1].split(",") if len(argv) > 1 else None
R = math.radians

# ------------------------------------------------------------------ helpers
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(7)
    _mats.clear()

_mats = {}
def M(name, color, metal=0.0, rough=0.5, emit=0.0, alpha=1.0):
    key = (name, color, metal, rough, emit, alpha)
    if key in _mats and _mats[key].name in bpy.data.materials:
        return _mats[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rough
    if emit > 0:
        b.inputs["Emission Color"].default_value = (*color, 1)
        b.inputs["Emission Strength"].default_value = emit
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        m.surface_render_method = 'BLENDED'
    m.diffuse_color = (*color, 1)
    _mats[key] = m
    return m

def P(kind, loc=(0, 0, 0), dims=(1, 1, 1), rot=(0, 0, 0), mat=None, bevel=0.0, sub=0, smooth=None, seg=40):
    if kind == 'cube':
        bpy.ops.mesh.primitive_cube_add(size=1)
    elif kind == 'sphere':
        bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=max(8, seg // 2), radius=0.5)
    elif kind == 'ico':
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=4, radius=0.5)
    elif kind == 'cyl':
        bpy.ops.mesh.primitive_cylinder_add(vertices=seg, radius=0.5, depth=1)
    elif kind == 'cone':
        bpy.ops.mesh.primitive_cone_add(vertices=seg, radius1=0.5, radius2=0, depth=1)
    elif kind == 'torus':
        bpy.ops.mesh.primitive_torus_add(major_radius=0.5, minor_radius=0.12, major_segments=seg, minor_segments=10)
    o = bpy.context.object
    o.scale = dims
    o.rotation_euler = tuple(R(a) for a in rot)
    o.location = loc
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        o.data.materials.append(mat)
    if bevel > 0:
        b = o.modifiers.new("bev", 'BEVEL'); b.width = bevel; b.segments = 3; b.limit_method = 'ANGLE'
    if sub > 0:
        s = o.modifiers.new("sub", 'SUBSURF'); s.levels = sub; s.render_levels = sub
    if smooth is None:
        smooth = kind in ('sphere', 'ico', 'torus') or sub > 0
    if smooth:
        bpy.ops.object.shade_smooth()
    return o

def text(body, loc, size, mat, rot=(90, 0, 0), extrude=0.002, align='CENTER'):
    bpy.ops.object.text_add(location=loc)
    t = bpy.context.object
    t.data.body = body
    t.data.size = size
    t.data.extrude = extrude
    t.data.align_x = align
    t.data.align_y = 'CENTER'
    t.rotation_euler = tuple(R(a) for a in rot)
    t.data.materials.append(mat)
    return t

def displace(o, strength, scale=0.05, kind='CLOUDS'):
    tex = bpy.data.textures.new("n", kind)
    if hasattr(tex, "noise_scale"):
        tex.noise_scale = scale
    d = o.modifiers.new("disp", 'DISPLACE'); d.texture = tex; d.strength = strength; d.mid_level = 0.5

def boolean(o, cutter, op='DIFFERENCE'):
    b = o.modifiers.new("bool", 'BOOLEAN'); b.object = cutter; b.operation = op; b.solver = 'EXACT'
    cutter.hide_render = True
    cutter['cutter'] = True

def bend(o, angle, axis='Z'):
    m = o.modifiers.new("bend", 'SIMPLE_DEFORM'); m.deform_method = 'BEND'; m.angle = R(angle); m.deform_axis = axis

def eye(loc, size, look=(0, -1, 0), iris=(0.05, 0.05, 0.05), closed=False, lid=None):
    parts = []
    if closed:
        parts.append(P('sphere', loc, (size, size * 0.5, size * 0.25), mat=lid or M("lid", (0.3, 0.2, 0.15))))
        parts.append(P('cube', (loc[0], loc[1] - size * 0.12, loc[2] - size * 0.02), (size * 0.8, size * 0.05, size * 0.04), mat=M("lash", (0.05, 0.05, 0.05))))
        return parts
    parts.append(P('sphere', loc, (size, size, size), mat=M("eyewhite", (0.96, 0.96, 0.94), rough=0.15)))
    d = Vector(look).normalized() * size * 0.42
    parts.append(P('sphere', (loc[0] + d.x, loc[1] + d.y, loc[2] + d.z), (size * 0.45, size * 0.45, size * 0.45), mat=M("iris", iris, rough=0.1)))
    d2 = Vector(look).normalized() * size * 0.55
    parts.append(P('sphere', (loc[0] + d2.x, loc[1] + d2.y, loc[2] + d2.z), (size * 0.22,) * 3, mat=M("pupil", (0.01, 0.01, 0.01), rough=0.05)))
    return parts

def star_mesh(r_out, r_in, depth, mat, loc, rot):
    bm = bmesh.new()
    verts = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        r = r_out if i % 2 == 0 else r_in
        verts.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0)))
    f = bm.faces.new(verts)
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in [e for e in ext['geom'] if isinstance(e, bmesh.types.BMVert)]:
        v.co.z += depth
    me = bpy.data.meshes.new("star"); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("star", me); bpy.context.scene.collection.objects.link(o)
    o.location = loc; o.rotation_euler = tuple(R(a) for a in rot)
    o.data.materials.append(mat)
    return o

def pixel_image(name, rows):
    """rows: lista de filas (arriba->abajo) de colores RGB."""
    h, w = len(rows), len(rows[0])
    img = bpy.data.images.new(name, w, h)
    px = []
    for y in range(h - 1, -1, -1):
        for c in rows[y]:
            px += [c[0], c[1], c[2], 1.0]
    img.pixels = px
    img.pack()
    return img

def tex_mat(name, img, rough=0.9):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes.get("Principled BSDF")
    t = nt.nodes.new("ShaderNodeTexImage"); t.image = img; t.interpolation = 'Closest'
    nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = rough
    return m

def finish(name, folder, pivot='keep', max_tris=14000):
    objs = [o for o in bpy.context.scene.objects if o.type in ('MESH', 'FONT', 'CURVE')]
    cutters = [o for o in objs if o.get('cutter')]
    keep = [o for o in objs if not o.get('cutter')]
    bpy.ops.object.select_all(action='DESELECT')
    for o in keep:
        o.select_set(True)
    bpy.context.view_layer.objects.active = keep[0]
    bpy.ops.object.convert(target='MESH')
    for c in cutters:
        bpy.data.objects.remove(c, do_unlink=True)
    keep = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in keep:
        o.select_set(True)
    bpy.context.view_layer.objects.active = keep[0]
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if pivot in ('bottom', 'center'):
        mn = Vector([min(v.co[i] for v in o.data.vertices) for i in range(3)])
        mx = Vector([max(v.co[i] for v in o.data.vertices) for i in range(3)])
        c = (mn + mx) / 2
        if pivot == 'bottom':
            c.z = mn.z
        o.data.transform(__import__('mathutils').Matrix.Translation(-c))
    # tope de triangulos para Quest: lo que pase de 14k se simplifica (conserva la forma suavizada)
    tris0 = sum(len(p.vertices) - 2 for p in o.data.polygons)
    if tris0 > max_tris:
        d = o.modifiers.new("tope", 'DECIMATE'); d.ratio = max_tris / tris0
        with bpy.context.temp_override(object=o, active_object=o):
            bpy.ops.object.modifier_apply(modifier="tope")
    out = os.path.join(DST, folder)
    os.makedirs(out, exist_ok=True)
    path = os.path.join(out, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', export_apply=True, export_yup=True, export_image_format='AUTO')
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    d = o.dimensions
    print(f"GEN {name}: {tris} tris, {d.x:.2f}x{d.y:.2f}x{d.z:.2f} m, {os.path.getsize(path)/1024:.0f} KB")


# ------------------------------------------------------------------ objetos (Items)
def papa_rellena():
    o = P('ico', dims=(0.12, 0.11, 0.1), mat=M("papa", (0.78, 0.48, 0.16), rough=0.65), sub=1)
    displace(o, 0.006, 0.02)
    finish("papa_rellena", "Items", 'center')

def sr_pantalones():
    yel = M("popo", (1.0, 0.82, 0.25), rough=0.55)
    P('sphere', (0, 0, 0.055), (0.085, 0.075, 0.095), mat=yel)
    for s in (-1, 1):
        eye((s * 0.018, -0.03, 0.07), 0.03, look=(0, -1, -0.1), iris=(0.1, 0.1, 0.1))
        P('cyl', (s * 0.05, -0.005, 0.05), (0.014, 0.014, 0.05), rot=(0, s * 35, 0), mat=yel)       # brazos
        P('sphere', (s * 0.068, -0.005, 0.032), (0.018, 0.018, 0.018), mat=yel)
        P('cyl', (s * 0.016, 0, 0.008), (0.016, 0.016, 0.025), mat=yel)                              # piernas
    P('cube', (0, -0.038, 0.042), (0.025, 0.004, 0.004), mat=M("boca", (0.25, 0.08, 0.05)))
    blue = M("sombrero", (0.15, 0.3, 0.8), rough=0.4)
    P('cyl', (0, 0, 0.104), (0.07, 0.07, 0.006), mat=blue)                                            # ala
    P('cyl', (0, 0, 0.13), (0.045, 0.045, 0.05), mat=blue)                                             # copa
    P('cyl', (0, 0, 0.113), (0.047, 0.047, 0.01), mat=M("cinta", (0.95, 0.95, 0.95)))
    finish("sr_pantalones", "Items", 'center')

def tio_steve():
    pink = M("steve", (1.0, 0.55, 0.75), rough=0.15)
    body = P('sphere', (0, 0.04, 0.08), (0.22, 0.34, 0.16), mat=pink, sub=1)
    displace(body, 0.006, 0.08)
    P('sphere', (0, -0.11, 0.16), (0.2, 0.18, 0.17), mat=pink, sub=1)                                # cabeza
    P('sphere', (0, -0.18, 0.13), (0.09, 0.06, 0.06), mat=M("hocico", (1.0, 0.7, 0.82), rough=0.2))
    for s in (-1, 1):
        P('sphere', (s * 0.052, -0.185, 0.19), (0.07, 0.05, 0.08), mat=M("ojosteve", (0.01, 0.01, 0.02), rough=0.02))
        P('sphere', (s * 0.04, -0.208, 0.205), (0.02, 0.01, 0.02), mat=M("brillo", (1, 1, 1), emit=1.0))
        P('cone', (s * 0.05, -0.1, 0.26), (0.03, 0.03, 0.08), rot=(15, s * 20, 0), mat=pink, sub=1)  # antenitas
        P('sphere', (s * 0.07, -0.09, 0.03), (0.06, 0.08, 0.04), mat=pink)                            # patitas
        P('sphere', (s * 0.07, 0.15, 0.03), (0.06, 0.08, 0.04), mat=pink)
    P('torus', (0, -0.195, 0.115), (0.04, 0.03, 0.03), rot=(90, 0, 0), mat=M("sonrisa", (0.5, 0.1, 0.2)))
    finish("tio_steve", "Items", 'center')

def mini_cronenberg():
    flesh = M("carne", (0.85, 0.45, 0.48), rough=0.35)
    b = P('ico', (0, 0, 0), (0.2, 0.18, 0.16), mat=flesh, sub=2)
    displace(b, 0.03, 0.04)
    gut = M("tripa", (0.65, 0.2, 0.25), rough=0.25)
    for i in range(5):
        a = random.uniform(0, 6.28)
        P('torus', (math.cos(a) * 0.07, math.sin(a) * 0.06 + 0.02, random.uniform(-0.02, 0.05)), (0.06, 0.06, 0.06), rot=(random.uniform(0, 90), random.uniform(0, 90), 0), mat=gut)
    eye((0, -0.07, 0.03), 0.09, look=(0.1, -1, 0.05), iris=(0.35, 0.55, 0.2))
    for i, a in enumerate((200, 260, 320)):
        t = P('cyl', (0, 0, 0), (0.025, 0.025, 0.18), mat=flesh, seg=12)
        t.data.transform(__import__('mathutils').Matrix.Translation((0, 0, -0.09)))
        bend(t, 70, 'X')
        t.location = (math.cos(R(a)) * 0.06, math.sin(R(a)) * 0.05 + 0.04, -0.04)
        t.rotation_euler = (R(30), 0, R(a + 90))
        P('sphere', (math.cos(R(a)) * 0.1, math.sin(R(a)) * 0.08 + 0.05, -0.12), (0.02, 0.02, 0.03), mat=M("baba", (0.02, 0.02, 0.02), rough=0.05))
    finish("mini_cronenberg", "Items", 'center')

def casco_snowball():
    metal = M("cascometal", (0.6, 0.62, 0.66), metal=0.8, rough=0.3)
    shell = P('sphere', (0, 0, 0), (0.24, 0.27, 0.34), mat=metal)
    cut = P('cube', (0, 0, -0.1), (0.4, 0.4, 0.2))
    boolean(shell, cut)
    P('torus', (0, 0, 0.0), (0.245, 0.265, 0.12), mat=M("borde", (0.2, 0.2, 0.22), metal=0.5, rough=0.4))
    P('cube', (0, -0.11, 0.045), (0.16, 0.05, 0.05), mat=M("visor", (0.1, 0.8, 1.0), rough=0.05, emit=1.5), bevel=0.01)
    for s in (-1, 1):
        P('cyl', (s * 0.125, 0, 0.05), (0.07, 0.07, 0.03), rot=(0, 90, 0), mat=M("oreja", (0.3, 0.3, 0.32), metal=0.6))
    P('cyl', (0, 0.02, 0.17), (0.008, 0.008, 0.12), mat=metal)
    P('sphere', (0, 0.02, 0.235), (0.025, 0.025, 0.025), mat=M("antena", (1, 0.15, 0.1), emit=2.0))
    P('cube', (0, 0.1, 0.08), (0.06, 0.03, 0.04), mat=M("caja", (0.25, 0.25, 0.27)), bevel=0.005)
    finish("casco_snowball", "Items", 'center')

def cereal_eyeholes():
    box = P('cube', (0, 0, 0.14), (0.2, 0.07, 0.28), mat=M("caja_cereal", (1.0, 0.62, 0.1), rough=0.6), bevel=0.003)
    purple = M("morado", (0.45, 0.2, 0.65), rough=0.5)
    P('cyl', (0, -0.0355, 0.17), (0.12, 0.12, 0.004), rot=(90, 0, 0), mat=purple)
    for s in (-1, 1):
        P('cyl', (s * 0.025, -0.038, 0.18), (0.03, 0.03, 0.004), rot=(90, 0, 0), mat=M("negro", (0.02, 0.02, 0.02)))
    text("EYEHOLES", (0, -0.037, 0.255), 0.032, M("titulo", (0.6, 0.05, 0.1)), extrude=0.001)
    for i in range(4):
        P('torus', (-0.07 + i * 0.045, -0.037, 0.06), (0.03, 0.03, 0.03), rot=(90, 0, 0), mat=M("cereal", (0.85, 0.65, 0.3), rough=0.8))
    text("¡No los comas!", (0, -0.037, 0.025), 0.014, M("blanco", (1, 1, 1)), extrude=0.0005)
    finish("cereal_eyeholes", "Items", 'center')

def vhs_balistica():
    black = M("vhs", (0.05, 0.05, 0.06), rough=0.4)
    P('cube', (0, 0, 0), (0.19, 0.1, 0.025), mat=black, bevel=0.003)
    for s in (-1, 1):
        P('cyl', (s * 0.045, 0.0, 0.0126), (0.035, 0.035, 0.002), mat=M("ventana", (0.25, 0.25, 0.28), rough=0.1))
        P('cyl', (s * 0.045, 0.0, 0.0135), (0.012, 0.012, 0.002), mat=M("blanco2", (0.9, 0.9, 0.9)))
    P('cube', (0, -0.0505, 0), (0.15, 0.002, 0.018), mat=M("etiqueta", (0.95, 0.93, 0.85)))
    text("BALISTICA INTERDIM. 2", (0, -0.0525, 0), 0.008, M("tinta", (0.1, 0.1, 0.4)), extrude=0.0003)
    finish("vhs_balistica", "Items", 'center')

def bateria_microverso():
    P('cube', (0, 0, 0.07), (0.17, 0.12, 0.14), mat=M("bateria", (0.15, 0.15, 0.17), rough=0.5), bevel=0.006)
    P('cube', (0, 0, 0.145), (0.172, 0.122, 0.012), mat=M("tapa", (0.25, 0.25, 0.28)), bevel=0.003)
    P('cyl', (-0.05, 0.03, 0.16), (0.022, 0.022, 0.025), mat=M("pos", (0.85, 0.1, 0.1), metal=0.4))
    P('cyl', (0.05, 0.03, 0.16), (0.022, 0.022, 0.025), mat=M("neg", (0.05, 0.05, 0.05), metal=0.4))
    P('cyl', (0, -0.061, 0.075), (0.07, 0.07, 0.004), rot=(90, 0, 0), mat=M("vidrio", (0.6, 0.8, 0.9), rough=0.05))
    P('sphere', (0, -0.062, 0.075), (0.04, 0.03, 0.04), mat=M("planeta", (0.35, 0.33, 0.3), rough=0.9))
    text("MICROVERSO", (0, -0.0615, 0.125), 0.015, M("amarillo", (1, 0.85, 0.1)), extrude=0.0005)
    text("SIN CARGA", (0, -0.0615, 0.02), 0.012, M("rojo", (1, 0.2, 0.15), emit=1.0), extrude=0.0005)
    finish("bateria_microverso", "Items", 'center')

def semilla_sillas():
    P('sphere', (0, 0, -0.008), (0.055, 0.055, 0.07), mat=M("semilla", (0.55, 0.36, 0.18), rough=0.4))
    cap = P('sphere', (0, 0, 0.022), (0.062, 0.062, 0.04), mat=M("capuchon", (0.35, 0.22, 0.1), rough=0.9))
    displace(cap, 0.003, 0.005)
    P('cyl', (0, 0, 0.048), (0.008, 0.008, 0.02), mat=M("tallito", (0.3, 0.6, 0.2)))
    leg = M("silla", (0.75, 0.55, 0.3))   # mini silla brotando
    P('cube', (0, 0, 0.068), (0.022, 0.02, 0.003), mat=leg)
    P('cube', (0, 0.009, 0.08), (0.022, 0.003, 0.025), mat=leg)
    finish("semilla_sillas", "Items", 'center')

def bloque_minecraft():
    rnd = random.Random(3)
    def dirt():
        return [[(0.45 + rnd.uniform(-0.06, 0.06), 0.3 + rnd.uniform(-0.05, 0.05), 0.18 + rnd.uniform(-0.04, 0.04)) for _ in range(16)] for _ in range(16)]
    def grass():
        return [[(0.3 + rnd.uniform(-0.05, 0.05), 0.62 + rnd.uniform(-0.08, 0.08), 0.18 + rnd.uniform(-0.04, 0.04)) for _ in range(16)] for _ in range(16)]
    side = dirt()
    g = grass()
    for x in range(16):
        depth = 3 + rnd.randint(0, 2)
        for y in range(depth):
            side[y][x] = g[y][x]
    mats = [tex_mat("mc_top", pixel_image("mc_top", grass())), tex_mat("mc_side", pixel_image("mc_side", side)), tex_mat("mc_bottom", pixel_image("mc_bottom", dirt()))]
    bpy.ops.mesh.primitive_cube_add(size=0.3)
    o = bpy.context.object
    for m in mats:
        o.data.materials.append(m)
    bm = bmesh.new(); bm.from_mesh(o.data)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        f.material_index = 0 if n.z > 0.5 else (2 if n.z < -0.5 else 1)
        # UV completa por cara, con el pasto arriba en las caras laterales
        cs = [l.vert.co for l in f.loops]
        if abs(n.z) > 0.5:
            for l in f.loops:
                l[uv].uv = (l.vert.co.x / 0.3 + 0.5, l.vert.co.y / 0.3 + 0.5)
        else:
            hx = Vector((-n.y, n.x, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(hx) / 0.3 + 0.5, l.vert.co.z / 0.3 + 0.5)
    bm.to_mesh(o.data); bm.free()
    finish("bloque_minecraft", "Items", 'center')

# ------------------------------------------------------------------ easter eggs y utileria (Env)
def casco_ironman():
    red = M("ironrojo", (0.62, 0.04, 0.04), metal=0.75, rough=0.28)
    gold = M("irondorado", (0.86, 0.66, 0.22), metal=0.95, rough=0.22)
    dark = M("ironoscuro", (0.08, 0.06, 0.05), metal=0.4, rough=0.6)
    # casco: forma de huevo achatada atras + mandibula
    head = P('sphere', (0, 0.01, 0.15), (0.21, 0.255, 0.27), mat=red, sub=1, seg=48)
    jaw = P('cube', (0, -0.035, 0.045), (0.17, 0.17, 0.08), mat=red, bevel=0.03, sub=2)
    # mascara dorada: solo el frente, con forma (frente angosta y pomulos)
    face = P('sphere', (0, -0.008, 0.135), (0.217, 0.255, 0.275), mat=gold, sub=1, seg=48)
    boolean(face, P('cube', (0, -0.11, 0.105), (0.125, 0.12, 0.2)), 'INTERSECT')
    chin = P('cube', (0, -0.103, 0.035), (0.085, 0.04, 0.06), mat=gold, bevel=0.015, sub=2)
    P('cube', (0, -0.075, 0.245), (0.05, 0.13, 0.02), mat=gold, bevel=0.008, sub=1)   # franja de la frente
    # ojos: uno brilla, al otro le falta la luz
    P('cube', (0.042, -0.122, 0.168), (0.05, 0.02, 0.013), rot=(0, -10, 0), mat=M("ojoluz", (0.65, 0.95, 1.0), emit=5.0), bevel=0.004)
    P('cube', (-0.042, -0.122, 0.168), (0.05, 0.02, 0.013), rot=(0, 10, 0), mat=M("ojoroto", (0.01, 0.01, 0.01)), bevel=0.004)
    for i in range(3):   # rendijas de la boca
        P('cube', (0, -0.126, 0.072 - i * 0.011), (0.058 - i * 0.012, 0.012, 0.0035), mat=dark)
    for s in (-1, 1):    # orejeras y lineas de paneles
        P('cyl', (s * 0.104, 0.01, 0.16), (0.06, 0.06, 0.012), rot=(0, 90, 0), mat=M("orejera", (0.35, 0.35, 0.37), metal=0.9, rough=0.3), seg=40)
    for i in range(7):   # marcas de soldadura
        P('sphere', (random.uniform(-0.07, 0.07), random.uniform(-0.03, 0.08), 0.255 + random.uniform(-0.03, 0.02)), (0.016, 0.016, 0.007), mat=M("soldadura", (0.22, 0.16, 0.1), metal=0.4, rough=0.9), seg=16)
    P('cyl', (0, 0.01, 0.012), (0.16, 0.19, 0.025), mat=M("cuello", (0.15, 0.15, 0.16), metal=0.7), seg=48)
    finish("casco_ironman", "Env", 'bottom')

def esfera_dragon():
    P('sphere', (0, 0, 0), (0.07, 0.07, 0.07), mat=M("dragonball", (1.0, 0.52, 0.04), rough=0.04, emit=0.5), seg=64)
    P('sphere', (0.012, -0.022, 0.018), (0.016, 0.008, 0.012), mat=M("brillo_db", (1, 1, 0.95), emit=2.0), seg=16)   # reflejo
    redm = M("estrella", (0.85, 0.04, 0.04), emit=0.4)
    for (x, z) in ((-0.011, 0.011), (0.011, 0.011), (-0.011, -0.011), (0.011, -0.011)):
        p = Vector((x, -0.0345, z)).normalized() * 0.0352
        o = star_mesh(0.0085, 0.0036, 0.0012, redm, p, (0, 0, 0))
        o.rotation_euler = p.normalized().to_track_quat('Z', 'Y').to_euler()
    finish("esfera_dragon", "Env", 'center')

def figura_miku():
    teal = M("mikupelo", (0.22, 0.8, 0.8), rough=0.35)
    skin = M("piel", (1.0, 0.87, 0.8), rough=0.55)
    gray = M("traje", (0.22, 0.23, 0.26), rough=0.5)
    P('cyl', (0, 0, 0.006), (0.1, 0.1, 0.012), mat=M("base", (0.08, 0.08, 0.1), rough=0.3), seg=48)
    for s in (-1, 1):
        P('cyl', (s * 0.013, 0, 0.032), (0.016, 0.016, 0.05), mat=M("medias", (0.06, 0.06, 0.08)), sub=1)
        P('sphere', (s * 0.013, -0.004, 0.012), (0.02, 0.028, 0.016), mat=M("botas", (0.1, 0.1, 0.12)))
    skirt = P('cone', (0, 0, 0.075), (0.085, 0.08, 0.04), mat=gray, seg=40)
    boolean(skirt, P('cube', (0, 0, 0.1), (0.2, 0.2, 0.03)))          # falda acampanada (cono truncado)
    P('cyl', (0, 0, 0.105), (0.042, 0.034, 0.05), mat=M("blusa", (0.78, 0.8, 0.83)), sub=1)
    P('cube', (0, -0.017, 0.11), (0.009, 0.004, 0.032), mat=teal)    # corbata
    for s in (-1, 1):
        P('cyl', (s * 0.027, 0, 0.1), (0.011, 0.011, 0.045), rot=(0, s * 18, 0), mat=skin, sub=1)   # brazos
    P('sphere', (0, 0, 0.165), (0.085, 0.08, 0.08), mat=skin, sub=1)              # cabeza grande
    P('sphere', (0, 0.012, 0.178), (0.093, 0.083, 0.078), mat=teal, sub=1)        # pelo
    P('cube', (0, -0.033, 0.197), (0.075, 0.02, 0.026), mat=teal, bevel=0.009, sub=1)   # flequillo
    for s in (-1, 1):
        P('sphere', (s * 0.048, 0.01, 0.203), (0.02, 0.02, 0.02), mat=M("mono", (0.85, 0.15, 0.3)))
        tail = P('cyl', (0, 0, 0), (0.028, 0.022, 0.17), mat=teal, sub=1)
        tail.data.transform(__import__('mathutils').Matrix.Translation((0, 0, -0.085)))
        bend(tail, 12 * s, 'X')
        tail.location = (s * 0.05, 0.016, 0.205); tail.rotation_euler = (0, R(s * -6), 0)
        P('cyl', (s * 0.062, 0.02, 0.07), (0.009, 0.009, 0.04), mat=M("tubo", (0.5, 1.0, 0.6), rough=0.05, emit=0.8))
        eye((s * 0.021, -0.037, 0.16), 0.02, iris=(0.1, 0.62, 0.62))
    P('cube', (0.021, -0.046, 0.162), (0.026, 0.004, 0.02), mat=M("parche", (0.85, 0.05, 0.05), emit=1.5), bevel=0.003)
    P('cube', (0, -0.04, 0.138), (0.012, 0.003, 0.003), mat=M("boca", (0.6, 0.2, 0.25)))
    finish("figura_miku", "Env", 'bottom')

def llama():
    purple = M("llamamorado", (0.62, 0.42, 0.88), rough=0.75)
    light = M("llamaclaro", (0.88, 0.8, 0.98), rough=0.75)
    stripes = [M("ll_azul", (0.3, 0.55, 0.95)), M("ll_rosa", (1, 0.5, 0.75)), M("ll_amarillo", (1, 0.86, 0.3))]
    P('cube', (0, 0.01, 0.1), (0.09, 0.09, 0.2), mat=purple, bevel=0.025, sub=2)                     # cuello
    P('cube', (0, -0.025, 0.215), (0.112, 0.155, 0.095), mat=purple, bevel=0.035, sub=2)          # cabeza
    P('cube', (0, -0.1, 0.2), (0.085, 0.05, 0.065), mat=light, bevel=0.02, sub=2)                  # hocico
    for i, m in enumerate(stripes):                                                                 # franjas de pinata
        P('cube', (0, 0.01, 0.03 + i * 0.05), (0.095, 0.095, 0.02), mat=m, bevel=0.006, sub=1)
    for s in (-1, 1):
        ear = P('cube', (s * 0.036, 0.02, 0.29), (0.026, 0.022, 0.085), rot=(0, s * -8, 0), mat=stripes[0], bevel=0.009, sub=2)
        P('cube', (s * 0.036, 0.012, 0.3), (0.012, 0.004, 0.05), rot=(0, s * -8, 0), mat=stripes[1])
        eye((s * 0.04, -0.094, 0.235), 0.03, iris=(0.05, 0.05, 0.08))
        P('sphere', (s * 0.016, -0.126, 0.205), (0.01, 0.006, 0.008), mat=M("nariz", (0.25, 0.15, 0.3)))
    P('cube', (0, -0.126, 0.182), (0.03, 0.004, 0.004), mat=M("boca", (0.3, 0.15, 0.35)))
    for i in range(5):   # flequillo de papel
        P('cone', (-0.03 + i * 0.015, -0.03, 0.27), (0.016, 0.016, 0.035), rot=(-15, 0, 0), mat=stripes[i % 3], seg=12)
    finish("llama", "Env", 'bottom')

def pokebola():
    red = M("pkrojo", (0.85, 0.08, 0.08), rough=0.2)
    white = M("pkblanco", (0.95, 0.95, 0.95), rough=0.25)
    black = M("pknegro", (0.03, 0.03, 0.03))
    bot = P('sphere', (0, 0, 0), (0.08, 0.08, 0.08), mat=white, seg=32)
    boolean(bot, P('cube', (0, 0, 0.05), (0.2, 0.2, 0.1)))
    top = P('sphere', (0, 0, 0), (0.08, 0.08, 0.08), mat=red, seg=32)
    boolean(top, P('cube', (0, 0, -0.05), (0.2, 0.2, 0.1)))
    top.location = (0, 0.04, 0.0); top.rotation_euler = (R(-28), 0, 0); top.location = (0, 0.0, 0.012)
    P('cyl', (0, 0, 0), (0.082, 0.082, 0.008), mat=black)
    P('cyl', (0, -0.04, 0), (0.026, 0.026, 0.006), rot=(90, 0, 0), mat=black)
    P('cyl', (0, -0.043, 0), (0.017, 0.017, 0.006), rot=(90, 0, 0), mat=white)
    for col, a in (((1, 0.85, 0.1), 30), ((0.1, 0.4, 1), -20)):
        c = P('cyl', (0, 0, 0), (0.004, 0.004, 0.09), mat=M("cable", col), seg=8)
        c.data.transform(__import__('mathutils').Matrix.Translation((0, 0, 0.045)))
        bend(c, 90, 'X')
        c.location = (0.01 * a / 30, 0.0, 0.008); c.rotation_euler = (R(80), 0, R(a))
    finish("pokebola", "Env", 'bottom')

def cubo_prison():
    P('cube', (0, 0, 0), (0.1, 0.1, 0.1), mat=M("prison", (0.28, 0.24, 0.32), rough=0.6), bevel=0.006)
    lid = M("parpado", (0.4, 0.33, 0.42), rough=0.5)
    for face in range(5):
        for i in range(3):
            for j in range(3):
                if (i + j) % 2:
                    continue
                u, v = (i - 1) * 0.03, (j - 1) * 0.03
                if face == 0: loc, rot = (u, -0.051, v), (90, 0, 0)
                elif face == 1: loc, rot = (u, 0.051, v), (90, 0, 0)
                elif face == 2: loc, rot = (-0.051, u, v), (90, 0, 90)
                elif face == 3: loc, rot = (0.051, u, v), (90, 0, 90)
                else: loc, rot = (u, v, 0.051), (0, 0, 0)
                P("sphere", loc, (0.018, 0.009, 0.007), rot=rot, mat=lid, seg=12)
    finish("cubo_prison", "Env", 'bottom')

def capibara():
    fur = M("capifur", (0.55, 0.38, 0.24), rough=0.85)
    dark = M("capioscuro", (0.32, 0.21, 0.13), rough=0.8)
    body = P('sphere', (0, 0.1, 0.22), (0.5, 0.95, 0.44), mat=fur, sub=1, seg=32)
    displace(body, 0.01, 0.03)
    P('sphere', (0, -0.33, 0.3), (0.32, 0.36, 0.3), mat=fur, sub=1)                    # cuello/hombros
    head = P('cube', (0, -0.55, 0.3), (0.22, 0.3, 0.22), mat=fur, bevel=0.07, sub=1)   # cabeza cuadrada tipica
    P('cube', (0, -0.69, 0.27), (0.2, 0.08, 0.17), mat=fur, bevel=0.05, sub=1)        # hocico
    for s in (-1, 1):
        P('sphere', (s * 0.035, -0.735, 0.31), (0.02, 0.012, 0.012), mat=dark)        # fosas
        P('sphere', (s * 0.09, -0.47, 0.42), (0.06, 0.04, 0.06), mat=dark)            # orejas
        P('sphere', (s * 0.15, -0.38, 0.06), (0.12, 0.2, 0.1), mat=fur)               # patas delanteras dobladas
        P('sphere', (s * 0.2, 0.42, 0.08), (0.16, 0.26, 0.14), mat=fur)               # patas traseras
    P('cube', (0, -0.71, 0.24), (0.06, 0.005, 0.005), mat=dark)                        # boca
    # Gafas VR modificadas por Rick
    P('cube', (0, -0.6, 0.36), (0.25, 0.1, 0.1), mat=M("vr", (0.08, 0.08, 0.09), rough=0.3), bevel=0.02)
    P('cube', (0, -0.652, 0.36), (0.18, 0.004, 0.05), mat=M("vrluz", (0.2, 1.0, 1.0), emit=3.0))
    P('torus', (0, -0.53, 0.36), (0.25, 0.3, 0.25), rot=(0, 0, 0), mat=M("correa", (0.1, 0.1, 0.1)))
    P('cyl', (0.1, -0.55, 0.43), (0.01, 0.01, 0.08), mat=M("antena2", (0.6, 0.6, 0.6), metal=0.8))
    finish("capibara", "Env", 'bottom')

def calavera():
    bone = M("hueso", (0.92, 0.88, 0.78), rough=0.6)
    skull = P('sphere', (0, 0, 0.1), (0.15, 0.18, 0.14), mat=bone, sub=1)
    for s in (-1, 1):
        boolean(skull, P('sphere', (s * 0.035, -0.085, 0.085), (0.045, 0.05, 0.045)))
    boolean(skull, P('cone', (0, -0.09, 0.05), (0.025, 0.04, 0.03), rot=(180, 0, 0)))
    P('cube', (0, -0.04, 0.02), (0.1, 0.09, 0.05), mat=bone, bevel=0.015)
    for i in range(6):
        P('cube', (-0.035 + i * 0.014, -0.086, 0.035), (0.01, 0.006, 0.014), mat=M("diente", (0.97, 0.97, 0.9)))
    finish("calavera", "Env", 'bottom')

def mazo():
    P('cyl', (0, 0.15, 0), (0.035, 0.035, 0.35), rot=(90, 0, 0), mat=M("mango", (0.4, 0.25, 0.12), rough=0.8))
    ball = P('ico', (0, -0.08, 0), (0.13, 0.13, 0.13), mat=M("bola", (0.3, 0.3, 0.32), metal=0.8, rough=0.35))
    for v in [Vector(c) for c in ((1, 0, 0), (-1, 0, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1), (0.6, -0.6, 0.5), (-0.6, -0.6, 0.5), (0.6, -0.6, -0.5), (-0.6, -0.6, -0.5))]:
        v.normalize()
        o = P('cone', tuple(Vector((0, -0.08, 0)) + v * 0.08), (0.03, 0.03, 0.06), mat=M("pua", (0.75, 0.75, 0.78), metal=0.9, rough=0.2))
        o.rotation_euler = v.to_track_quat('Z', 'Y').to_euler()
    finish("mazo", "Env", 'bottom')

def corona():
    gold = M("oro", (0.95, 0.75, 0.2), metal=1.0, rough=0.2)
    ring = P('cyl', (0, 0, 0.02), (0.18, 0.18, 0.04), mat=gold, seg=32)
    boolean(ring, P('cyl', (0, 0, 0.02), (0.16, 0.16, 0.06), seg=32))
    for i in range(7):
        a = i * 2 * math.pi / 7
        P('cone', (math.cos(a) * 0.085, math.sin(a) * 0.085, 0.08), (0.03, 0.03, 0.1 + 0.04 * (i % 2)), rot=(0, 0, 0), mat=gold)
        P('sphere', (math.cos(a) * 0.091, math.sin(a) * 0.091, 0.025), (0.02, 0.02, 0.02), mat=M("gema", (0.6, 0.1, 1.0), rough=0.05, emit=1.5))
    finish("corona", "Env", 'bottom')

def cabeza_monstruo():
    green = M("monstruo", (0.2, 0.6, 0.2), rough=0.6)
    P('sphere', (0, 0, 0.16), (0.3, 0.28, 0.26), mat=green, sub=1)
    P('sphere', (0, -0.08, 0.06), (0.26, 0.2, 0.1), mat=green, sub=1)                   # mandibula
    for i in range(7):
        P('cone', (-0.09 + i * 0.03, -0.155, 0.1), (0.02, 0.02, 0.045), rot=(180, 0, 0), mat=M("dientes", (0.97, 0.95, 0.85)))
        P('cone', (-0.09 + i * 0.03, -0.15, 0.07), (0.018, 0.018, 0.035), mat=M("dientes", (0.97, 0.95, 0.85)))
    for s in (-1, 1):
        eye((s * 0.07, -0.11, 0.21), 0.07, iris=(0.95, 0.85, 0.1))
        P('cube', (s * 0.07, -0.12, 0.26), (0.09, 0.03, 0.025), rot=(0, s * -15, 0), mat=M("ceja", (0.1, 0.35, 0.1)), bevel=0.008)
        P('cone', (s * 0.11, 0.0, 0.3), (0.04, 0.04, 0.12), rot=(0, s * 25, 0), mat=M("cuerno", (0.85, 0.8, 0.65)))
    finish("cabeza_monstruo", "Env", 'bottom')

def caja_carton():
    card = M("carton", (0.72, 0.55, 0.35), rough=0.9)
    P('cube', (0, 0, 0.14), (0.45, 0.35, 0.28), mat=card, bevel=0.004)
    P('cube', (0, 0, 0.281), (0.08, 0.352, 0.002), mat=M("cinta", (0.8, 0.75, 0.55), rough=0.3))
    for s in (-1, 1):
        P('cube', (s * 0.12, 0, 0.29), (0.2, 0.34, 0.004), rot=(0, s * -12, 0), mat=card)
    text("TIME TRAVEL\nSTUFF", (0, -0.176, 0.15), 0.06, M("marcador", (0.05, 0.05, 0.05)), extrude=0.0005)
    finish("caja_carton", "Env", 'bottom')

# ------------------------------------------------------------------ exterior (Env)
def puerta_garaje():
    paint = M("puertapintura", (0.82, 0.83, 0.82), metal=0.3, rough=0.45)
    groove = M("ranura", (0.45, 0.46, 0.47), metal=0.4)
    for i in range(5):
        z = 0.25 + i * 0.5
        P('cube', (0, 0, z), (3.5, 0.05, 0.48), mat=paint, bevel=0.01)
        for j in range(4):
            P('cube', (-1.31 + j * 0.875, -0.027, z), (0.78, 0.01, 0.3), mat=groove, bevel=0.006)
    P('cube', (0, -0.05, 0.12), (0.3, 0.04, 0.03), mat=M("manija", (0.2, 0.2, 0.2), metal=0.9, rough=0.3), bevel=0.01)
    finish("puerta_garaje", "Env", 'bottom')

def _windows(x_list, z, w, h, depth_y, frame, glass):
    for x in x_list:
        P('cube', (x, depth_y, z), (w + 0.16, 0.12, h + 0.16), mat=frame, bevel=0.01)
        P('cube', (x, depth_y - 0.04, z), (w, 0.06, h), mat=glass)
        P('cube', (x, depth_y - 0.075, z), (0.04, 0.02, h), mat=frame)
        P('cube', (x, depth_y - 0.075, z), (w, 0.02, 0.04), mat=frame)
        P('cube', (x, depth_y - 0.12, z - h / 2 - 0.08), (w + 0.25, 0.14, 0.06), mat=frame)

def _gable_roof(w, d, z, h, overhang, mat):
    bm = bmesh.new()
    hw, hd = w / 2 + overhang, d / 2 + overhang
    v = [bm.verts.new(p) for p in ((-hw, -hd, z), (hw, -hd, z), (hw, hd, z), (-hw, hd, z), (-hw, 0, z + h), (hw, 0, z + h))]
    for f in ((0, 1, 5, 4), (3, 4, 5, 2), (0, 4, 3), (1, 2, 5), (0, 3, 2, 1)):
        bm.faces.new([v[i] for i in f])
    me = bpy.data.meshes.new("roof"); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new("roof", me); bpy.context.scene.collection.objects.link(o)
    o.data.materials.append(mat)
    s = o.modifiers.new("sol", 'SOLIDIFY'); s.thickness = 0.12
    return o

def casa_smith():
    white = M("paredblanca", (0.93, 0.92, 0.88), rough=0.8)
    brick = M("ladrillo", (0.6, 0.2, 0.15), rough=0.9)
    trim = M("marcos", (0.97, 0.97, 0.97), rough=0.5)
    glass = M("vidriocasa", (0.45, 0.62, 0.8), rough=0.05, metal=0.2)
    roof = M("tejas", (0.42, 0.26, 0.17), rough=0.9)
    W, D, H = 10.0, 9.0, 6.0
    P('cube', (0, 0, H / 2), (W, D, H), mat=white)
    P('cube', (0, 0, 0.3), (W + 0.08, D + 0.08, 0.6), mat=brick)
    P('cube', (0, 0, 3.0), (W + 0.06, D + 0.06, 0.12), mat=trim)
    _gable_roof(W, D, H, 2.2, 0.45, roof)
    # tejas: lineas horizontales
    for i in range(7):
        y = -(D / 2 + 0.45) + i * 0.35 * 1.0
        z = H + (i * 0.35) * (2.2 / (D / 2 + 0.45)) + 0.13
        P('cube', (0, y + 0.0, z), (W + 0.9, 0.04, 0.03), mat=M("tejasosc", (0.33, 0.2, 0.13)))
    _windows((-3.2, 0.0, 3.2), 4.4, 1.2, 1.3, -D / 2, trim, glass)
    _windows((-3.2, 3.2), 1.7, 1.2, 1.3, -D / 2, trim, glass)
    # puerta principal con porche
    P('cube', (0.0, -D / 2 - 0.03, 1.15), (1.1, 0.1, 2.2), mat=M("puerta", (0.35, 0.22, 0.12), rough=0.6), bevel=0.02)
    P('sphere', (0.4, -D / 2 - 0.1, 1.1), (0.07, 0.07, 0.07), mat=M("perilla", (0.85, 0.7, 0.3), metal=1.0, rough=0.2))
    P('cube', (0.0, -D / 2 - 0.6, 0.1), (2.4, 1.2, 0.2), mat=M("porche", (0.6, 0.58, 0.55)))
    P('cube', (0.0, -D / 2 - 0.9, 2.5), (2.6, 1.6, 0.12), mat=trim)
    for s in (-1, 1):
        P('cyl', (s * 1.15, -D / 2 - 1.55, 1.25), (0.14, 0.14, 2.4), mat=trim)
    P('cube', (3.0, 1.5, H + 2.0), (0.8, 0.8, 2.6), mat=brick)                        # chimenea
    finish("casa_smith", "Env", 'bottom')

def uvbox(o, size):
    """UV por proyeccion de caja: 1 unidad de UV = 'size' metros (para texturas repetidas puestas en Unity)."""
    if not o.data.uv_layers:
        o.data.uv_layers.new(name="UVMap")
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=size, scale_to_bounds=False, correct_aspect=False)
    bpy.ops.object.mode_set(mode='OBJECT')

def casa_vecino():
    # Los materiales "vec_pared" y "vec_techo" se reemplazan en Unity por texturas (RickMortyModelPass.Retex).
    wall = M("vec_pared", (1.0, 0.62, 0.72), rough=0.85)
    roofm = M("vec_techo", (0.62, 0.25, 0.2), rough=0.9)
    trim = M("vec_marcos", (0.97, 0.96, 0.93), rough=0.5)
    red = M("rojovecino", (0.62, 0.14, 0.14), rough=0.55)
    glass = M("vidriovec", (0.55, 0.75, 0.95), rough=0.05)
    stone = M("vec_zocalo", (0.52, 0.5, 0.48), rough=0.9)
    W, D, H = 9.0, 7.0, 3.5
    uvbox(P('cube', (0, 0, H / 2), (W, D, H), mat=wall), 1.5)
    uvbox(P('cube', (0, 0, 0.22), (W + 0.1, D + 0.1, 0.44), mat=stone, bevel=0.02), 1.0)      # zocalo
    P('cube', (0, 0, H - 0.08), (W + 0.12, D + 0.12, 0.16), mat=trim)                          # cornisa
    for sx in (-1, 1):
        for sy in (-1, 1):
            P('cube', (sx * W / 2, sy * D / 2, H / 2 + 0.2), (0.16, 0.16, H - 0.4), mat=trim)  # esquineros
    roof = _gable_roof(W, D, H, 1.5, 0.45, roofm)
    uvbox(roof, 2.0)
    for sy in (-1, 1):                                                                         # frontones (triangulos) cerrados en blanco
        pass
    P('cube', (0, 0, H + 1.53), (W + 0.95, 0.14, 0.1), mat=trim)                               # cumbrera
    P('cube', (-2.6, 1.2, H + 1.3), (0.7, 0.7, 1.9), mat=stone, bevel=0.02)                    # chimenea
    P('cube', (-2.6, 1.2, H + 2.28), (0.84, 0.84, 0.1), mat=trim)
    _windows((-3.0, 3.0), 1.8, 1.3, 1.1, -D / 2, trim, glass)
    for x in (-3.0, 3.0):                                                                      # postigos rojos
        for s in (-1, 1):
            P('cube', (x + s * 0.93, -D / 2 - 0.03, 1.8), (0.36, 0.05, 1.22), mat=red, bevel=0.008)
            for k in range(5):
                P('cube', (x + s * 0.93, -D / 2 - 0.06, 1.34 + k * 0.23), (0.28, 0.02, 0.05), mat=M("postigoosc", (0.45, 0.1, 0.1)))
    # puerta con marco, tablero y perilla
    P('cube', (0, -D / 2 - 0.02, 1.16), (1.22, 0.1, 2.34), mat=trim)
    P('cube', (0, -D / 2 - 0.05, 1.1), (1.0, 0.1, 2.2), mat=red, bevel=0.02)
    for z in (0.62, 1.62):
        P('cube', (0, -D / 2 - 0.105, z), (0.72, 0.02, 0.78), mat=M("tablero", (0.5, 0.11, 0.11)), bevel=0.01)
    P('sphere', (0.38, -D / 2 - 0.13, 1.1), (0.07, 0.07, 0.07), mat=M("perillav", (0.85, 0.7, 0.3), metal=1.0, rough=0.2))
    # porche: escalones + alero con dos columnas
    P('cube', (0, -D / 2 - 0.55, 0.1), (2.2, 1.1, 0.2), mat=stone, bevel=0.02)
    P('cube', (0, -D / 2 - 0.95, 0.04), (2.6, 0.5, 0.08), mat=stone)
    uvbox(P('cube', (0, -D / 2 - 0.62, 2.62), (2.5, 1.3, 0.1), rot=(-8, 0, 0), mat=roofm), 2.0)
    for s in (-1, 1):
        P('cube', (s * 1.05, -D / 2 - 1.05, 1.36), (0.12, 0.12, 2.4), mat=trim)
    P('cube', (1.0, -D / 2 - 0.07, 2.05), (0.14, 0.08, 0.2), mat=M("farol", (1.0, 0.9, 0.6), emit=1.5))   # farolito
    for s in (-1, 1):   # jardineras
        P('cube', (s * 3.0, -D / 2 - 0.35, 0.3), (1.6, 0.4, 0.3), mat=M("maceta", (0.42, 0.26, 0.17)), bevel=0.02)
        for k in range(5):
            P('ico', (s * 3.0 - 0.6 + k * 0.3, -D / 2 - 0.35, 0.52), (0.28, 0.28, 0.24), mat=M("hojas", (0.2, 0.5, 0.18)))
            P('ico', (s * 3.0 - 0.6 + k * 0.3, -D / 2 - 0.4, 0.66), (0.13, 0.13, 0.13), mat=M("flores" + str(k % 3), ((0.95, 0.3, 0.5), (1, 0.85, 0.3), (0.95, 0.95, 0.95))[k % 3]))
    finish("casa_vecino", "Env", 'bottom')

def valla():
    white = M("valla", (0.96, 0.96, 0.94), rough=0.7)
    for i in range(8):
        x = -0.875 + i * 0.25
        p = P('cube', (x, 0, 0.42), (0.08, 0.025, 0.84), mat=white)
        P('cube', (x, 0, 0.86), (0.057, 0.025, 0.057), rot=(0, 45, 0), mat=white)
    for z in (0.25, 0.65):
        P('cube', (0, 0.025, z), (2.0, 0.025, 0.07), mat=white)
    finish("valla", "Env", 'bottom')

def camioneta():
    black = M("van", (0.04, 0.04, 0.05), metal=0.6, rough=0.25)
    glass = M("polarizado", (0.05, 0.07, 0.1), rough=0.02, metal=0.5)
    tire = M("llanta", (0.03, 0.03, 0.03), rough=0.9)
    rim = M("rin", (0.6, 0.6, 0.62), metal=1.0, rough=0.3)
    P('cube', (0, 0.4, 1.25), (2.0, 4.0, 1.9), mat=black, bevel=0.12)                  # caja
    P('cube', (0, -1.95, 0.95), (2.0, 1.1, 1.3), mat=black, bevel=0.15)               # cabina/capo
    ws = P('cube', (0, -1.45, 1.75), (1.85, 0.08, 0.9), rot=(-30, 0, 0), mat=glass)   # parabrisas
    for s in (-1, 1):
        P('cube', (s * 1.005, -1.15, 1.6), (0.02, 0.9, 0.6), mat=glass)              # ventanas laterales
        for y in (-1.6, 1.5):
            P('cyl', (s * 0.95, y, 0.38), (0.75, 0.75, 0.3), rot=(0, 90, 0), mat=tire, seg=24)
            P('cyl', (s * 1.1, y, 0.38), (0.42, 0.42, 0.04), rot=(0, 90, 0), mat=rim, seg=16)
        text("FBI", (s * 1.012, 0.6, 1.4), 0.65, M("fbi", (1, 0.85, 0.1)), rot=(90, 0, s * 90), extrude=0.005)
        P('cube', (s * 0.7, -2.51, 1.0), (0.3, 0.02, 0.14), mat=M("faro", (1, 1, 0.85), emit=3.0))
    P('cube', (0, -2.52, 0.55), (2.05, 0.15, 0.25), mat=M("parachoque", (0.15, 0.15, 0.15), metal=0.5))
    P('cube', (0, -2.5, 0.85), (0.9, 0.03, 0.3), mat=M("rejilla", (0.1, 0.1, 0.1), metal=0.8))
    P('cube', (0, 2.42, 0.55), (2.05, 0.15, 0.25), mat=M("parachoque", (0.15, 0.15, 0.15), metal=0.5))
    for s in (-1, 1):
        P('cube', (s * 0.55, -0.2, 2.24), (0.3, 0.12, 0.08), mat=M("sirena_r" if s < 0 else "sirena_a", (1, 0.1, 0.1) if s < 0 else (0.1, 0.3, 1), emit=3.0))
    finish("camioneta", "Env", 'bottom')

def barricada():
    white = M("barr_blanco", (0.97, 0.97, 0.97), rough=0.5)
    orange = M("barr_naranja", (1.0, 0.42, 0.05), rough=0.5)
    for z in (0.55, 0.85):
        for i in range(6):
            P('cube', (-0.75 + i * 0.3, 0, z), (0.3, 0.03, 0.2), mat=orange if (i + int(z * 10)) % 2 else white)
    for s in (-1, 1):
        for d in (-1, 1):
            P('cube', (s * 0.8, d * 0.15, 0.48), (0.06, 0.04, 1.0), rot=(d * 17, 0, 0), mat=M("pata", (0.6, 0.6, 0.62), metal=0.7))
        P('cyl', (s * 0.8, 0, 1.0), (0.1, 0.1, 0.08), rot=(90, 0, 0), mat=M("luzbarr", (1, 0.6, 0.1), emit=2.5))
    finish("barricada", "Env", 'bottom')

def cono():
    orange = M("cono", (1.0, 0.4, 0.05), rough=0.5)
    c = P('cone', (0, 0, 0.27), (0.3, 0.3, 0.5), mat=orange, seg=24)
    P('cyl', (0, 0, 0.3), (0.19, 0.19, 0.07), mat=M("reflectivo", (0.97, 0.97, 0.97), rough=0.2), seg=24)
    P('cube', (0, 0, 0.015), (0.4, 0.4, 0.03), mat=M("basecono", (0.08, 0.08, 0.08)), bevel=0.02)
    finish("cono", "Env", 'bottom')

def aro():
    P('cube', (0, 0, 0.45), (1.1, 0.04, 0.75), mat=M("tablero", (0.97, 0.97, 0.97), rough=0.3), bevel=0.01)
    red = M("cuadro", (0.85, 0.1, 0.1))
    for (x, z, w, h) in ((0, 0.42, 0.45, 0.025), (0, 0.18, 0.45, 0.025), (-0.21, 0.3, 0.025, 0.25), (0.21, 0.3, 0.025, 0.25)):
        P('cube', (x, -0.022, z), (w, 0.005, h), mat=red)
    P('torus', (0, -0.3, 0.1), (0.46, 0.46, 0.06), mat=M("aro", (1, 0.4, 0.05), metal=0.6, rough=0.3), seg=32)
    P('cube', (0, -0.08, 0.1), (0.12, 0.16, 0.03), mat=M("soporte", (0.4, 0.4, 0.42), metal=0.8))
    netm = M("red", (0.95, 0.95, 0.95))
    for i in range(10):
        a = i * 2 * math.pi / 10
        x, y = math.cos(a) * 0.2, -0.3 + math.sin(a) * 0.2
        o = P('cyl', (x * 0.85, y * 0.95 - 0.3 * 0.05, -0.08), (0.008, 0.008, 0.36), rot=(math.degrees(math.sin(a)) * 0.2, -math.degrees(math.cos(a)) * 0.2, 0), mat=netm, seg=6)
    finish("aro", "Env", 'top')


def hombre_eyeholes():
    # El Hombre Agujero de Ojo: musculoso, traje azul, guantes/botas/calzon naranja, mascara con dos agujeros.
    suit = M("traje", (0.16, 0.42, 0.85), rough=0.45)
    dark = M("trajeosc", (0.10, 0.27, 0.6), rough=0.5)
    oran = M("naranja", (0.98, 0.5, 0.08), rough=0.4)
    skin = M("piel", (0.93, 0.72, 0.58), rough=0.6)
    black = M("hueco", (0.02, 0.02, 0.03), rough=0.9)
    yellow = M("amar", (1.0, 0.85, 0.1), rough=0.35)
    # torso en V
    P('sphere', (0, 0, 1.42), (0.62, 0.36, 0.5), mat=suit, sub=1)            # pecho
    P('sphere', (0, 0, 1.12), (0.44, 0.3, 0.42), mat=suit, sub=1)            # abdomen
    P('sphere', (0, 0, 0.93), (0.46, 0.32, 0.26), mat=oran, sub=1)           # calzon
    P('cyl', (0, 0, 1.03), (0.47, 0.33, 0.05), mat=yellow)                   # cinturon
    P('cyl', (0, -0.165, 1.03), (0.1, 0.02, 0.09), rot=(90, 0, 0), mat=oran) # hebilla
    for s in (-1, 1):
        P('sphere', (s * 0.15, -0.13, 1.5), (0.26, 0.14, 0.2), mat=dark, sub=1)          # pectorales
        P('sphere', (s * 0.34, 0, 1.6), (0.26, 0.26, 0.24), mat=suit, sub=1)             # hombros
        # brazos levantados (pose de ataque)
        P('sphere', (s * 0.5, -0.02, 1.72), (0.19, 0.2, 0.36), rot=(0, s * -35, 0), mat=suit, sub=1)   # biceps
        P('sphere', (s * 0.58, -0.1, 2.0), (0.16, 0.17, 0.34), rot=(18, s * 12, 0), mat=suit, sub=1)   # antebrazo
        P('sphere', (s * 0.56, -0.18, 2.2), (0.17, 0.16, 0.2), mat=oran, sub=1)                         # puno
        P('cyl', (s * 0.57, -0.15, 2.09), (0.17, 0.17, 0.07), rot=(18, s * 12, 0), mat=oran)            # puno del guante
        # piernas
        P('sphere', (s * 0.14, 0, 0.62), (0.24, 0.26, 0.56), mat=suit, sub=1)
        P('sphere', (s * 0.15, 0.01, 0.26), (0.19, 0.2, 0.44), mat=suit, sub=1)
        P('sphere', (s * 0.15, -0.05, 0.06), (0.2, 0.34, 0.13), mat=oran, sub=1)                        # botas
        P('cyl', (s * 0.15, 0.01, 0.2), (0.2, 0.21, 0.08), mat=oran)
    # cuello y cabeza con mascara
    P('cyl', (0, 0, 1.72), (0.2, 0.2, 0.12), mat=suit)
    head = P('sphere', (0, -0.01, 1.92), (0.36, 0.38, 0.42), mat=suit, sub=1)
    P('sphere', (0, -0.14, 1.8), (0.24, 0.14, 0.17), mat=skin, sub=1)        # menton al descubierto
    P('cube', (0, -0.205, 1.795), (0.15, 0.02, 0.035), mat=black)            # boca (mueca)
    P('cube', (0, -0.21, 1.8), (0.13, 0.02, 0.012), mat=M("dientes", (0.97, 0.97, 0.92)))
    for s in (-1, 1):
        P('sphere', (s * 0.085, -0.17, 1.95), (0.12, 0.06, 0.13), mat=black)             # agujero
        P('torus', (s * 0.085, -0.175, 1.95), (0.135, 0.145, 0.05), rot=(90, 0, 0), mat=yellow)  # borde del agujero
        for e in eye((s * 0.085, -0.175, 1.95), 0.075, look=(0, -1, -0.15), iris=(0.55, 0.1, 0.08)):
            pass
        P('cube', (s * 0.085, -0.2, 2.03), (0.14, 0.03, 0.025), rot=(0, s * 22, 0), mat=dark)   # ceno fruncido
    P('cube', (0, -0.17, 1.52), (0.16, 0.02, 0.16), rot=(0, 45, 0), mat=yellow)                 # emblema rombo
    P('sphere', (0, -0.182, 1.52), (0.07, 0.02, 0.07), mat=black)
    finish("hombre_eyeholes", "Env", 'bottom')


def _crystal(x, y, h, r, tilt, col):
    """Cristal hexagonal con punta, inclinado 'tilt' = (rx, ry) grados. Base en z=0."""
    rx, ry = tilt
    body = P('cyl', (x, y, h * 0.36), (r * 2, r * 2, h * 0.72), rot=(rx, ry, random.uniform(0, 60)), mat=col, seg=6, smooth=False)
    # punta: se coloca siguiendo la inclinacion del cuerpo
    import mathutils
    up = mathutils.Euler((R(rx), R(ry), 0)).to_matrix() @ Vector((0, 0, 1))
    tip = Vector((x, y, h * 0.36)) + up * (h * 0.36 + h * 0.14)
    P('cone', tuple(tip), (r * 2, r * 2, h * 0.28), rot=(rx, ry, random.uniform(0, 60)), mat=col, seg=6, smooth=False)

def relleno_kalaxianos():
    # Contenido del frasco de Cristales Kalaxianos: racimo de cristales violeta. Caja unidad (1x1x1), pivote abajo.
    c1 = M("cristalA", (0.56, 0.2, 0.92), rough=0.08, emit=0.9)
    c2 = M("cristalB", (0.78, 0.45, 1.0), rough=0.05, emit=1.2)
    c3 = M("cristalC", (0.36, 0.1, 0.7), rough=0.15, emit=0.5)
    P('sphere', (0, 0, 0.05), (0.9, 0.9, 0.16), mat=M("cristalbase", (0.25, 0.1, 0.4), rough=0.6), seg=16)
    _crystal(0, 0, 0.95, 0.13, (4, -3), c2)
    for i in range(7):
        a = i * math.pi * 2 / 7 + 0.3
        d = 0.24 + 0.06 * (i % 2)
        h = 0.5 + 0.28 * ((i * 37) % 5) / 4
        _crystal(math.cos(a) * d, math.sin(a) * d, h, 0.085 + 0.02 * (i % 3), (math.sin(a) * -22, math.cos(a) * 22), (c1, c3, c2)[i % 3])
    for i in range(6):
        a = i * math.pi * 2 / 6
        _crystal(math.cos(a) * 0.38, math.sin(a) * 0.38, 0.26, 0.06, (math.sin(a) * -38, math.cos(a) * 38), (c3, c1)[i % 2])
    finish("relleno_kalaxianos", "Env", 'bottom', max_tris=2600)

def relleno_esporas():
    # Esporas parasitarias: bolitas magenta con puas, amontonadas.
    body = M("espora", (0.72, 0.16, 0.42), rough=0.55)
    spike = M("puas", (0.98, 0.55, 0.75), rough=0.4, emit=0.4)
    spots = [(0, 0, 0.2, 0.4), (0.24, 0.1, 0.17, 0.32), (-0.22, 0.14, 0.16, 0.3), (0.05, -0.26, 0.15, 0.28), (-0.16, -0.18, 0.13, 0.24),
             (0.1, 0.12, 0.5, 0.3), (-0.12, -0.02, 0.47, 0.27), (0.12, -0.14, 0.44, 0.24), (0, 0.03, 0.74, 0.25), (0.26, -0.16, 0.13, 0.22)]
    for (x, y, z, s) in spots:
        P('sphere', (x, y, z), (s, s, s), mat=body, seg=10)
        for k in range(10):
            # puas repartidas con la espiral de Fibonacci
            t = (k + 0.5) / 10
            inc = math.acos(1 - 2 * t); az = k * 2.39996
            d = Vector((math.sin(inc) * math.cos(az), math.sin(inc) * math.sin(az), math.cos(inc)))
            if d.z < -0.5:
                continue
            p = Vector((x, y, z)) + d * s * 0.52
            rot = d.to_track_quat('Z', 'Y').to_euler()
            P('cone', tuple(p), (s * 0.16, s * 0.16, s * 0.34), rot=(math.degrees(rot.x), math.degrees(rot.y), math.degrees(rot.z)), mat=spike, seg=8)
    finish("relleno_esporas", "Env", 'bottom', max_tris=2600)

def relleno_plumbus_polvo():
    # Polvo de plumbus: monticulo rosado de polvo con grumos.
    dust = M("polvo", (0.96, 0.68, 0.64), rough=0.95)
    lump = M("grumo", (0.9, 0.55, 0.55), rough=0.9)
    m = P('sphere', (0, 0, 0.12), (0.98, 0.98, 0.62), mat=dust, seg=32)
    displace(m, 0.07, 0.22)
    P('sphere', (0.08, -0.05, 0.36), (0.6, 0.6, 0.42), mat=dust, seg=24)
    for i in range(14):
        a = i * 2.39996; d = 0.12 + 0.3 * ((i * 7) % 10) / 10
        s = 0.07 + 0.05 * ((i * 3) % 4) / 3
        P('sphere', (math.cos(a) * d, math.sin(a) * d, 0.34 + 0.2 * (1 - d / 0.45) + 0.03), (s, s, s * 0.8), mat=lump, seg=10)
    # recorte inferior: nada por debajo de z=0
    cutter = P('cube', (0, 0, -0.5), (2, 2, 1))
    boolean(m, cutter)
    finish("relleno_plumbus_polvo", "Env", 'bottom', max_tris=2600)

def relleno_marihuana():
    # Marihuana cosmica: cogollos verde-morados con pelitos naranja.
    g1 = M("cogollo", (0.3, 0.55, 0.2), rough=0.85)
    g2 = M("cogollomorado", (0.5, 0.25, 0.62), rough=0.85)
    hair = M("pistilo", (1.0, 0.55, 0.15), rough=0.6, emit=0.3)
    spots = [(0, 0, 0.24, 0.46, 0.5), (0.2, 0.14, 0.2, 0.36, 0.42), (-0.2, 0.1, 0.2, 0.34, 0.44), (0.02, -0.22, 0.2, 0.36, 0.4),
             (0.06, 0.06, 0.62, 0.4, 0.46), (-0.1, -0.06, 0.86, 0.26, 0.3)]
    for n, (x, y, z, s, h) in enumerate(spots):
        b = P('sphere', (x, y, z), (s, s, h), mat=g1 if n % 2 == 0 else g2, seg=10)
        displace(b, 0.05, 0.09)
        for k in range(9):
            t = (k + 0.5) / 9
            inc = math.acos(1 - 2 * t); az = k * 2.39996 + n
            d = Vector((math.sin(inc) * math.cos(az), math.sin(inc) * math.sin(az), math.cos(inc)))
            p = Vector((x, y, z)) + Vector((d.x * s, d.y * s, d.z * h)) * 0.46
            P('sphere', tuple(p), (s * 0.3, s * 0.3, s * 0.3), mat=g2 if (n + k) % 3 == 0 else g1)
            if k % 3 == 0:
                rot = d.to_track_quat('Z', 'Y').to_euler()
                P('cyl', tuple(p + d * s * 0.16), (0.012, 0.012, s * 0.3), rot=(math.degrees(rot.x), math.degrees(rot.y), math.degrees(rot.z)), mat=hair, seg=5)
    finish("relleno_marihuana", "Env", 'bottom', max_tris=2600)


ALL = [relleno_kalaxianos, relleno_esporas, relleno_plumbus_polvo, relleno_marihuana, hombre_eyeholes, papa_rellena, sr_pantalones, tio_steve, mini_cronenberg, casco_snowball, cereal_eyeholes, vhs_balistica,
       bateria_microverso, semilla_sillas, bloque_minecraft, casco_ironman, pokebola, cubo_prison, esfera_dragon,
       figura_miku, llama, capibara, calavera, mazo, corona, cabeza_monstruo, caja_carton, puerta_garaje,
       casa_smith, casa_vecino, valla, camioneta, barricada, cono, aro]

# Estos ya se reemplazaron por modelos descargados de Sketchfab (process_models.py, entradas SF2):
# solo se regeneran si se piden explicitamente por id, para no pisar los descargados.
REPLACED = {"tio_steve", "hombre_eyeholes", "mini_cronenberg", "vhs_balistica", "bateria_microverso", "semilla_sillas", "casco_ironman", "pokebola",
            "cubo_prison", "figura_miku", "llama", "capibara", "calavera", "mazo", "corona", "cabeza_monstruo",
            "caja_carton", "camioneta", "casa_smith", "barricada", "cono", "valla", "aro"}

for fn in ALL:
    if ONLY and fn.__name__ not in ONLY:
        continue
    if not ONLY and fn.__name__ in REPLACED:
        continue
    try:
        reset()
        fn()
    except Exception as e:
        import traceback
        print("ERR", fn.__name__, e)
        traceback.print_exc()
