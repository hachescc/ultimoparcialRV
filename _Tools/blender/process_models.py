# Procesa los modelos descargados: rota, une, decima, escala a metros, ajusta pivote,
# reduce texturas, recolorea y exporta GLB optimizado a Assets/_RickMorty/Models.
# Convencion: en Blender +Z arriba y el FRENTE (canon/punta) hacia -Y  => en Unity +Z adelante.
# Uso: blender -b -P process_models.py -- <src_dir> <dst_dir> [solo_id]
import bpy, sys, os, math
from mathutils import Vector, Matrix, Quaternion

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, DST = argv[0], argv[1]
ONLY = argv[2].split(",") if len(argv) > 2 else None

# fit: ('h', alto) | ('len', largo en Y) | ('max', mayor dimension) | ('w', ancho en X) | ('dims', (x,y,z))
# pivot: center | bottom | top ; rot: grados XYZ aplicados primero ; flip: gira 180 en Z
# color: reemplaza el color base (quita texturas); tris: maximo de triangulos ; tex: tamano max de textura
CFG = [
    # ---------- Escenario ----------
    dict(id="rick", src="rick_sanchez_-_ready_for_ue4_rigged.glb", dir="Env", fit=('h', 1.75), pivot='bottom', tex=1024, rig=True),
    dict(id="mesa", src="workbench (1).glb", dir="Env", fit=('dims', (2.4, 1.0, 0.9)), pivot='bottom', tex=512),
    dict(id="estanteria", src="rusty_metal_shelving.glb", dir="Env", fit=('dims', (2.0, 0.5, 2.45)), pivot='bottom', tex=1024, tris=20000),
    dict(id="brazo_robotico", src="industrial_assembly_arm_animated.glb", dir="Env", fit=('h', 1.3), pivot='bottom', color=(0.42, 0.5, 0.44)),
    dict(id="tazon", src="glass_bowl..glb", dir="Env", fit=('dims', (0.47, 0.47, 0.17)), pivot='bottom', tris=3000),
    dict(id="lavadora", src="washing_machine.glb", dir="Env", fit=('h', 0.88), pivot='bottom', tex=1024, fixnormals=True),
    dict(id="fregadero", src="sink_mtal_-_18mb.glb", dir="Env", fit=('h', 0.92), pivot='bottom', tex=1024),
    dict(id="lampara_fluorescente", src="old_fluorescent_light.glb", dir="Env", rot=(0, 0, 90), fit=('max', 1.3), pivot='top', tex=512),
    dict(id="caja_herramientas", src="jagged_toolbox__game_ready.glb", dir="Env", rot=(0, 0, 90), fit=('max', 0.45), pivot='bottom', tex=512),
    dict(id="escoba", src="broom_wood_-_5mb.glb", dir="Env", fit=('h', 1.35), pivot='bottom', tex=512),
    dict(id="recogedor", src="dustpan.glb", dir="Env", fit=('max', 0.3), pivot='bottom'),
    dict(id="meeseeks_npc", src="mr_meeseks.glb", dir="Env", fit=('h', 1.05), pivot='bottom', tris=9000, tex=512),

    # ---------- Objetos (ids de items.json) ----------
    dict(id="mega_semilla", src="erlenmeyer_flask.glb", fit=('h', 0.19), pivot='center'),
    dict(id="meeseeks", src="transparent_glass_jar_with_green_lid_-_3d_model.glb", fit=('h', 0.14), pivot='center'),
    dict(id="bola_meeseeks", src="meeseks_box.glb", fit=('h', 0.2), pivot='center', tex=512),
    dict(id="plumbus", src="plumbus_keychain_rick_and_morty_3d_printable.glb", fit=('h', 0.22), pivot='center', tris=4000, color=(0.95, 0.58, 0.68)),
    dict(id="espada_zord", src="greatsword.glb", rot=(0, 0, 90), fit=('len', 1.1), pivot='center', tex=512, color=(0.09, 0.08, 0.1), flip=True),
    dict(id="katana", src="katana.glb", rot=(0, 0, 90), fit=('len', 1.0), pivot='center', tex=512),
    dict(id="sable_luz", src="lightsaber_hilt.glb", rot=(0, 0, 90), fit=('len', 0.27), pivot='center', tex=512),
    dict(id="cuchillo_csgo", src="karambit.glb", fit=('len', 0.22), pivot='center', tex=512),
    dict(id="kunai", src="kunai.glb", rot=(0, 0, 90), fit=('len', 0.25), pivot='center', tex=512),
    dict(id="scar", src="fortnite_scar.glb", fit=('len', 0.7), pivot='center', tex=512, flip=True),
    dict(id="pistola_laser", src="westar-34_blaster_pistol_-_star_wars.glb", fit=('len', 0.3), pivot='center', tex=512),
    dict(id="pistola_portales", src="rick_portal_gun.glb", fit=('len', 0.32), pivot='center', tris=8000, flip=True),
    dict(id="portal_carton", src="rick_portal_gun.glb", fit=('len', 0.32), pivot='center', tris=5000, color=(0.6, 0.44, 0.27), rough=1.0, flip=True),
    dict(id="guantelete", src="infinity_gauntlet_textured_no_rig.glb", fit=('h', 0.32), pivot='center', tex=512),
    dict(id="token_fortnite", src="golden_fortnite_v-bucks.glb", fit=('max', 0.2), pivot='center', tex=512),
    dict(id="aguardiente", src="aguardiente_antioqueno_model.glb", fit=('h', 0.24), pivot='center', tex=512),
    dict(id="szechuan", src="fire_sauce.glb", fit=('max', 0.09), pivot='center', tex=256),
    dict(id="empanada", src="empanada.glb", fit=('max', 0.15), pivot='center', tris=3000, tex=512),
    dict(id="camiseta_america", src="football_shirt_fc_barcelona.glb", fit=('h', 0.5), pivot='center', tris=5000, color=(0.75, 0.05, 0.1)),
    dict(id="pepino_mutante", src="retopo_rick.glb", fit=('h', 0.25), pivot='center', tex=512),
    dict(id="meeseeks_kirkland", src="mr_meeseks.glb", fit=('h', 0.32), pivot='center', tris=6000, color=(0.7, 0.25, 0.22)),
    dict(id="clon_rick", src="crystal_ball.glb", fit=('max', 0.1), pivot='center', tris=1500),
    dict(id="toallin", src="towelie.glb", fit=('h', 0.26), pivot='center', tex=512, tris=8000, png=True),
    dict(id="peluche_teto", src="kasane_teto_plush.glb", fit=('h', 0.28), pivot='center', tris=8000, tex=512, png=True),
    dict(id="dakimakura", src="kurumi_body_pillow.glb", fit=('h', 0.85), pivot='center', tex=512),
]
# ---------- Reemplazos descargados de Sketchfab (2026-11-16) para modelos que antes se generaban ----------
# Fuente: _ModelosOriginales/sketchfab2/<id>.glb (autores y licencias en _ModelosOriginales/sketchfab2/CREDITOS.md)
SF2 = "sketchfab2/"
SF3 = "sketchfab3/"
CFG += [
    dict(id="mini_cronenberg", src=SF2 + "mini_cronenberg.glb", fit=('max', 0.27), pivot='center', tris=8000, tex=512),
    dict(id="vhs_balistica", src=SF2 + "vhs_balistica.glb", fit=('w', 0.19), pivot='center', tex=512),
    dict(id="bateria_microverso", src=SF2 + "bateria_microverso.glb", fit=('w', 0.2), pivot='center', tex=512),
    dict(id="semilla_sillas", src=SF2 + "semilla_sillas.glb", fit=('h', 0.135), pivot='center', tex=512),
    dict(id="casco_ironman", src=SF2 + "casco_ironman.glb", dir="Env", fit=('h', 0.285), pivot='bottom', tris=15000, tex=1024),
    dict(id="pokebola", src=SF2 + "pokebola.glb", dir="Env", fit=('max', 0.09), pivot='bottom', tex=512),
    dict(id="cubo_prison", src=SF2 + "cubo_prison.glb", dir="Env", fit=('max', 0.1), pivot='bottom', tex=512),
    dict(id="figura_miku", src=SF2 + "figura_miku.glb", dir="Env", drop=["kage"], fit=('h', 0.217), pivot='bottom', tex=512, png=True),
    dict(id="llama", src=SF2 + "llama.glb", dir="Env", rot=(0, 0, 90), fit=('h', 0.332), pivot='bottom', tris=15000, tex=512),
    dict(id="capibara", src=SF2 + "capibara.glb", dir="Env", fit=('len', 1.1), pivot='bottom', tris=12000, tex=1024),
    dict(id="calavera", src=SF2 + "calavera.glb", dir="Env", fit=('h', 0.175), pivot='bottom', tris=10000, tex=1024),
    dict(id="mazo", src=SF2 + "mazo.glb", dir="Env", rot=(90, 0, 0), fit=('len', 0.515), pivot='bottom', tex=512),
    dict(id="corona", src=SF2 + "corona.glb", dir="Env", fit=('max', 0.2), pivot='bottom', tex=1024),
    dict(id="cabeza_monstruo", src=SF2 + "cabeza_monstruo.glb", dir="Env", fit=('max', 0.36), pivot='bottom', tex=1024),
    dict(id="caja_carton", src=SF2 + "caja_carton.glb", dir="Env", fit=('dims', (0.45, 0.353, 0.313)), pivot='bottom', tex=512),
    dict(id="camioneta", src=SF2 + "camioneta.glb", dir="Env", drop=["flag"], fit=('len', 5.09), pivot='bottom', tris=20000, tex=1024),
    dict(id="casa_smith", src=SF2 + "casa_smith.glb", dir="Env", fit=('w', 10.9), pivot='bottom', tex=1024),
    dict(id="barricada", src=SF2 + "barricada.glb", dir="Env", rot=(0, 0, 90), fit=('w', 1.8), pivot='bottom', tex=1024),
    dict(id="cono", src=SF2 + "cono.glb", dir="Env", fit=('h', 0.52), pivot='bottom', tex=512),
]
# ---------- Ronda 2 (entorno): casas del barrio, valla, aro, caneca, boton, vegetacion, mobiliario urbano ----------
CFG += [
    dict(id="casa_barrio1", src=SF2 + "casa_barrio1.glb", dir="Env", fit=('w', 7.5), pivot='bottom', tris=6000, tex=1024),
    dict(id="casa_barrio2", src=SF2 + "casa_barrio2.glb", dir="Env", fit=('w', 8.0), pivot='bottom', tris=8000, tex=512),
    dict(id="valla", src=SF2 + "valla.glb", dir="Env", rot=(0, 0, 90), fit=('dims', (2.0, 0.05, 0.9)), pivot='bottom', tex=512),
    dict(id="aro", src=SF2 + "aro.glb", dir="Env", rot=(0, 0, -90), fit=('h', 1.05), pivot='top', tex=512),
    dict(id="caneca", src=SF2 + "caneca.glb", dir="Env", fit=('h', 0.6), pivot='bottom', tex=512),
    dict(id="boton_rojo", src=SF2 + "boton_rojo.glb", dir="Env", fit=('w', 0.15), pivot='bottom', tris=5000, tex=512),
    dict(id="arbol", src=SF2 + "arbol.glb", dir="Env", fit=('h', 6.5), pivot='bottom', tris=6000, tex=1024),
    dict(id="arbusto", src=SF2 + "arbusto.glb", dir="Env", fit=('h', 0.9), pivot='bottom', tex=512),
    dict(id="buzon", src=SF2 + "buzon.glb", dir="Env", fit=('h', 1.25), pivot='bottom', tex=512),
    dict(id="farola", src=SF2 + "farola.glb", dir="Env", fit=('h', 5.0), pivot='bottom', tris=2500, tex=512),
    dict(id="hidrante", src=SF2 + "hidrante.glb", dir="Env", fit=('h', 0.75), pivot='bottom', tex=512),
    dict(id="reloj_pared", src=SF2 + "reloj_pared.glb", dir="Env", fit=('max', 0.32), pivot='center', tex=512),
    # --- Ronda 3 (2026-11-19): personajes que seguian generados + utileria del garaje
    dict(id="hombre_eyeholes", src=SF3 + "hombre_eyeholes.glb", dir="Env", drop=["Ring", "Icosphere"], rot=(0, 0, 45), fit=('h', 2.05), pivot='bottom', tris=12000, tex=1024),
    dict(id="tio_steve", src=SF3 + "tio_steve.glb", fit=('max', 0.34), pivot='center', tris=8000, tex=512),
    dict(id="casco_snowball", src=SF3 + "casco_snowball.glb", rot=(0, 0, 180), fit=('max', 0.31), pivot='center', color=(0.72, 0.76, 0.8), rough=0.3, tex=512),
    dict(id="barril", src=SF3 + "barril.glb", dir="Env", fit=('h', 0.9), pivot='bottom', tex=1024),
    dict(id="llantas", src=SF3 + "llantas.glb", dir="Env", fit=('h', 0.82), pivot='bottom', tex=1024),
    dict(id="gabinete", src=SF3 + "gabinete.glb", dir="Env", fit=('h', 1.35), pivot='bottom', tris=9000, tex=1024),
    dict(id="bicicleta", src=SF3 + "bicicleta.glb", dir="Env", fit=('len', 1.75), pivot='bottom', tex=512, png=True),
]
# Ingredientes que comparten modelo
ALIASES = {"mega_semilla": ["fleeb", "acido", "materia_oscura"], "meeseeks": ["plumbus_polvo", "esporas", "kalaxianos", "marihuana", "semen"], "bola_meeseeks": ["caja_contrabando"]}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def bounds(objs):
    bpy.context.view_layer.update()
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in objs:
        if o.type != 'MESH':
            continue
        dg = bpy.context.evaluated_depsgraph_get()
        eo = o.evaluated_get(dg)
        me = eo.to_mesh()
        for v in me.vertices:
            w = o.matrix_world @ v.co
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
        eo.to_mesh_clear()
    return mn, mx


def tri_count(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs if o.type == 'MESH')


def make_static(objs):
    """Hornea modificadores (armature) y transformaciones; une todo en un solo mesh."""
    vl = bpy.context.view_layer.objects
    junk = [o for o in objs if o.type == 'MESH' and (o.name not in vl or any(col.name.startswith('glTF_not_exported') for col in o.users_collection))]
    for o in junk:
        bpy.data.objects.remove(o, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    for o in meshes:
        bpy.context.view_layer.objects.active = o
        for m in list(o.modifiers):
            try:
                with bpy.context.temp_override(object=o, active_object=o):
                    bpy.ops.object.modifier_apply(modifier=m.name)
            except Exception as e:
                o.modifiers.remove(m)
    for o in meshes:
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    for o in [o for o in bpy.data.objects if o.type != 'MESH']:
        bpy.data.objects.remove(o, do_unlink=True)
    for o in meshes:
        if o.data.users > 1:
            o.data = o.data.copy()
        o.data.transform(o.matrix_world)
        o.matrix_world = Matrix.Identity(4)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    return bpy.context.view_layer.objects.active


def set_color(color, rough=None):
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        for n in mat.node_tree.nodes:
            if n.type == 'BSDF_PRINCIPLED':
                inp = n.inputs['Base Color']
                for l in list(inp.links):
                    mat.node_tree.links.remove(l)
                inp.default_value = (color[0], color[1], color[2], 1)
                if rough is not None:
                    n.inputs['Roughness'].default_value = rough
                    n.inputs['Metallic'].default_value = 0.0


def shrink_textures(maxsize):
    for img in bpy.data.images:
        if img.size[0] == 0:
            continue
        w, h = img.size
        if max(w, h) > maxsize:
            k = maxsize / max(w, h)
            img.scale(max(4, int(w * k)), max(4, int(h * k)))


def fit_and_pivot(root_objs, transform_target, c):
    mn, mx = bounds(root_objs)
    d = mx - mn
    kind, val = c['fit']
    if kind == 'dims':
        s = Vector((val[0] / max(d.x, 1e-6), val[1] / max(d.y, 1e-6), val[2] / max(d.z, 1e-6)))
    else:
        ref = {'h': d.z, 'len': d.y, 'w': d.x, 'max': max(d)}[kind]
        k = val / max(ref, 1e-6)
        s = Vector((k, k, k))
    piv = (mn + mx) / 2
    if c.get('pivot') == 'bottom':
        piv.z = mn.z
    elif c.get('pivot') == 'top':
        piv.z = mx.z
    M = Matrix.Diagonal(s.to_4d()) @ Matrix.Translation(-piv)
    transform_target(M)


def process(c):
    reset()
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, c['src']))
    # keep: solo las mallas cuyo nombre contenga alguno de estos textos; drop: quitar las que lo contengan
    if c.get('keep') or c.get('drop'):
        for o in list(bpy.data.objects):
            if o.type != 'MESH':
                continue
            if (c.get('keep') and not any(k in o.name for k in c['keep'])) or any(d in o.name for d in c.get('drop', [])):
                bpy.data.objects.remove(o, do_unlink=True)
    objs = list(bpy.data.objects)
    rig = c.get('rig', False)
    rot = Matrix.Identity(4)
    if c.get('rot'):
        r = c['rot']
        rot = (Matrix.Rotation(math.radians(r[2]), 4, 'Z') @ Matrix.Rotation(math.radians(r[1]), 4, 'Y') @ Matrix.Rotation(math.radians(r[0]), 4, 'X'))
    if c.get('flip'):
        rot = Matrix.Rotation(math.pi, 4, 'Z') @ rot

    if not rig:
        obj = make_static(objs)
        obj.data.transform(rot)
        if c.get('tris') and tri_count([obj]) > c['tris']:
            ratio = c['tris'] / tri_count([obj])
            m = obj.modifiers.new("dec", 'DECIMATE')
            m.ratio = ratio
            with bpy.context.temp_override(object=obj, active_object=obj):
                bpy.ops.object.modifier_apply(modifier="dec")
        fit_and_pivot([obj], lambda M: obj.data.transform(M), c)
        obj.name = c['id']
        objs = [obj]
    else:
        # Con rig: transformar el objeto raiz y aplicar escala/rotacion a todo
        roots = [o for o in objs if o.parent is None]
        root = bpy.data.objects.new("root", None)
        bpy.context.scene.collection.objects.link(root)
        for o in roots:
            o.parent = root
        root.matrix_world = rot
        bpy.context.view_layer.update()

        def apply_to_root(M):
            root.matrix_world = M @ root.matrix_world
        fit_and_pivot(objs, apply_to_root, c)
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action='SELECT')
        bpy.context.view_layer.objects.active = root
        # aplicar transformaciones a la jerarquia completa
        for o in [o for o in bpy.data.objects if o.parent == root]:
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
        bpy.data.objects.remove(root, do_unlink=True)
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        if c['id'] == 'rick':
            rick_animations()
        objs = list(bpy.data.objects)

    if c.get('color'):
        set_color(c['color'], c.get('rough'))
        # los colores por vertice multiplican el color base: quitarlos
        for o in bpy.data.objects:
            if o.type == 'MESH':
                for a in list(o.data.color_attributes):
                    o.data.color_attributes.remove(a)
    if c.get('fixnormals'):
        for o in [o for o in bpy.data.objects if o.type == 'MESH']:
            bpy.context.view_layer.objects.active = o
            bpy.ops.object.mode_set(mode='EDIT')
            bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.mesh.normals_make_consistent(inside=False)
            bpy.ops.object.mode_set(mode='OBJECT')
        for m in bpy.data.materials:
            m.use_backface_culling = False
    shrink_textures(c.get('tex', 1024))
    out_dir = os.path.join(DST, c.get('dir', 'Items'))
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, c['id'] + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', export_animations=rig,
                              export_animation_mode='ACTIONS', export_apply=True, export_yup=True,
                              export_image_format='AUTO' if c.get('png') else 'JPEG', export_jpeg_quality=85)
    mn, mx = bounds(objs)
    d = mx - mn
    print(f"OUT {c['id']}: tris {tri_count(objs)} dims {d.x:.2f}x{d.y:.2f}x{d.z:.2f} -> {os.path.getsize(path)/1048576:.2f}MB")
    return path


# ---------------------------------------------------------------------------
# Animaciones de Rick: brazos abajo como pose de reposo + Idle / Walk / Talk
# ---------------------------------------------------------------------------

def _find(arm, *keys):
    for b in arm.pose.bones:
        n = b.name.lower()
        if all(k in n for k in keys):
            return b
    return None


def _tail_world(arm, pb):
    return arm.matrix_world @ pb.tail


def _best_axis(arm, pb, probe, score):
    """Prueba rotar pb en X/Y/Z (+/-) y elige la que maximiza score(desplazamiento del probe)."""
    best = None
    base = _tail_world(arm, probe).copy()
    for ax in ('X', 'Y', 'Z'):
        for sgn in (1, -1):
            axis = Vector((1 if ax == 'X' else 0, 1 if ax == 'Y' else 0, 1 if ax == 'Z' else 0))
            pb.rotation_mode = 'QUATERNION'
            pb.rotation_quaternion = Quaternion(axis, math.radians(30) * sgn)
            bpy.context.view_layer.update()
            sc = score(_tail_world(arm, probe) - base)
            if best is None or sc > best[0]:
                best = (sc, axis * sgn)
            pb.rotation_quaternion = Quaternion()
            bpy.context.view_layer.update()
    return best[1]


def rick_animations():
    arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
    if arm is None:
        print("WARN sin armature")
        return
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    ua_l, ua_r = _find(arm, 'upperarm_l'), _find(arm, 'upperarm_r')
    la_l, la_r = _find(arm, 'lowerarm_l'), _find(arm, 'lowerarm_r')
    th_l, th_r = _find(arm, 'thigh_l'), _find(arm, 'thigh_r')
    cf_l, cf_r = _find(arm, 'calf_l'), _find(arm, 'calf_r')
    head = _find(arm, 'head')
    spine = _find(arm, 'spine_03') or _find(arm, 'spine')
    print("BONES", [b.name if b else None for b in (ua_l, ua_r, th_l, th_r, head, spine)])

    # 1) Brazos abajo (de pose T a pose relajada) y fijarlo como reposo
    # Se calcula la rotacion exacta que lleva el hueso (su eje Y) a "abajo y un poco afuera",
    # en vez de adivinar un eje local (eso giraba los brazos hacia atras y quedaban escondidos).
    from mathutils import Euler

    def world_dir(pb):
        # direccion REAL de la extremidad: de esta articulacion a la siguiente (la punta del hueso
        # importado de glTF es una heuristica y no sigue la malla)
        nxt = next((c for c in pb.children if 'twist' not in c.name.lower()), None)
        end = nxt.head if nxt is not None else pb.tail
        return ((arm.matrix_world @ end) - (arm.matrix_world @ pb.head)).normalized()

    def aim(pb, target):
        """Rotacion exacta: la diferencia entre la direccion real del brazo y la deseada, llevada
        del espacio del mundo al espacio local del hueso en reposo (padres en reposo)."""
        pb.rotation_mode = 'QUATERNION'
        for _ in range(3):   # 3 pasadas para corregir el error residual
            cur = world_dir(pb)
            rw = cur.rotation_difference(target)
            arm_q = arm.matrix_world.to_quaternion()
            axis_arm = (arm_q.inverted() @ rw.axis).normalized()
            rest = pb.bone.matrix_local.to_3x3().normalized()
            r = Quaternion(axis_arm, rw.angle).to_matrix()
            pb.rotation_quaternion = (rest.inverted() @ r @ rest).to_quaternion() @ pb.rotation_quaternion
            bpy.context.view_layer.update()
        print("AIM", pb.name, "error grados:", round(math.degrees(world_dir(pb).angle(target)), 1))

    for ua in (ua_l, ua_r):
        if ua is None:
            continue
        side = 1 if (arm.matrix_world @ ua.head).x > 0 else -1
        aim(ua, Vector((0.16 * side, -0.04, -1.0)).normalized())
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode='OBJECT')
    for m in [o for o in bpy.data.objects if o.type == 'MESH']:
        for mod in list(m.modifiers):
            if mod.type == 'ARMATURE':
                with bpy.context.temp_override(object=m, active_object=m):
                    bpy.ops.object.modifier_apply(modifier=mod.name)
        nm = m.modifiers.new("Armature", 'ARMATURE')
        nm.object = arm
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.armature_apply(selected=False)
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = Quaternion()

    # 2) Animaciones: cada hueso rota alrededor de un eje del MUNDO (X = lateral, para
    #    balancear adelante/atras), convertido al espacio local del hueso en reposo.
    arm_rot = arm.matrix_world.to_quaternion()

    def local_q(pb, world_axis, deg):
        axis_arm = (arm_rot.inverted() @ Vector(world_axis)).normalized()
        rest = pb.bone.matrix_local.to_3x3().normalized()
        r = Quaternion(axis_arm, math.radians(deg)).to_matrix()
        return (rest.inverted() @ r @ rest).to_quaternion()

    def key(pb, deg, f, axis=(1, 0, 0)):
        if pb is None:
            return
        pb.rotation_quaternion = local_q(pb, axis, deg)
        pb.keyframe_insert("rotation_quaternion", frame=f)

    def new_action(name):
        if arm.animation_data is None:
            arm.animation_data_create()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
        for pb in arm.pose.bones:
            pb.rotation_quaternion = Quaternion()
        return act

    # Walk: ciclo de 1 s, piernas +-24, rodillas, brazos opuestos +-14
    new_action("Walk")
    for f in range(0, 25, 3):
        s = math.sin(f / 24 * 2 * math.pi)
        key(th_l, 24 * s, f); key(th_r, -24 * s, f)
        key(cf_l, -max(0.0, -s) * 30, f); key(cf_r, -max(0.0, s) * 30, f)
        key(ua_l, -14 * s, f); key(ua_r, 14 * s, f)
        key(spine, 3, f)
    # Idle: respiracion y leve balanceo
    new_action("Idle")
    for f in range(0, 49, 6):
        s = math.sin(f / 48 * 2 * math.pi)
        key(spine, 2 * s, f)
        key(head, 3 * math.sin(f / 48 * 2 * math.pi + 1), f)
        key(ua_l, 2 * s, f); key(ua_r, -2 * s, f)
    # Talk: cabeceo + gesto con el antebrazo derecho hacia adelante
    new_action("Talk")
    for f in range(0, 49, 4):
        ph = f / 48 * 2 * math.pi
        key(head, 7 * math.sin(ph * 2), f)
        key(spine, 2 * math.sin(ph), f)
        key(ua_r, -18 + 6 * math.sin(ph * 2), f)
        key(la_r, -55 + 15 * math.sin(ph * 3), f)
        key(ua_l, 3 * math.sin(ph), f)
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.rotation_quaternion = Quaternion()
    bpy.ops.object.mode_set(mode='OBJECT')
    print("ANIMS", [a.name for a in bpy.data.actions])


import shutil
for c in CFG:
    if ONLY and c["id"] not in ONLY:
        continue
    try:
        p = process(c)
        for alias in ALIASES.get(c['id'], []):
            shutil.copyfile(p, os.path.join(os.path.dirname(p), alias + ".glb"))
    except Exception as e:
        import traceback
        print("ERR", c['id'], e)
        traceback.print_exc()
