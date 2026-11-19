using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RickMorty.EditorTools
{
    /// <summary>
    /// Segundo paso del builder: reemplaza los placeholders de primitivas por los modelos reales
    /// de Assets/_RickMorty/Models/Env (descargados y optimizados, o generados en Blender).
    /// Oculta los renderers del placeholder pero conserva sus colliders y componentes de juego.
    /// Si un modelo no existe, el placeholder queda como estaba.
    /// </summary>
    public static class RickMortyModelPass
    {
        const string EnvDir = RickMortySetup.Root + "/Models/Env/";

        // Alturas de las repisas del modelo de estanteria (medidas en Blender)
        static readonly float[] ShelfLevels = { 0.21f, 0.64f, 1.08f, 1.51f, 1.94f, 2.38f };

        public static void Apply()
        {
            int before = s_Placed;
            Desk();
            Shelf();
            Laundry();
            Corner();
            Lamps();
            GarageDoor();
            Exterior();
            Perimeter();
            Rick();
            Revamp();
            Debug.Log($"[RickMorty] Modelos colocados en la escena: {s_Placed - before}");
        }

        static int s_Placed;

        // ------------------------------------------------------------------ helpers

        static GameObject Model(string id, Transform parent, Vector3 worldPos, Quaternion worldRot, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvDir + id + ".glb");
            if (prefab == null) { Debug.LogWarning("[RickMorty] Falta modelo: " + id); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = id + " (modelo)";
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(worldPos, worldRot);
            var ls = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(scale / ls.x, scale / ls.y, scale / ls.z);
            s_Placed++;
            return go;
        }

        /// <summary>Texto "pintado" sobre un modelo (sin fondo), hijo del modelo para que siga su rotacion.</summary>
        static void Sticker(GameObject model, string text, Vector3 localPos, Quaternion localRot, float fontSize, Color color)
        {
            if (model == null) return;
            var go = new GameObject("Rotulo_" + text.Replace("\n", " "));
            go.transform.SetParent(model.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var ls = model.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            var t = go.AddComponent<TMPro.TextMeshPro>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = TMPro.FontStyles.Bold;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(fontSize * 0.4f, fontSize * 0.2f);
            t.enableWordWrapping = false;
        }

        /// <summary>Marca un modelo de fondo como estatico (static batching: menos draw calls en Quest).</summary>
        static GameObject MakeStatic(GameObject go)
        {
            if (go == null) return null;
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        static bool Exists(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(EnvDir + id + ".glb") != null;

        static GameObject Find(string path) => GameObject.Find(path);

        static IEnumerable<GameObject> Children(string groupPath, string name)
        {
            var g = Find(groupPath);
            if (g == null) yield break;
            foreach (Transform t in g.transform.Cast<Transform>().ToList())
                if (t.name == name) yield return t.gameObject;
        }

        static void Hide(GameObject go, GameObject except = null)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (except == null || !r.transform.IsChildOf(except.transform)) r.enabled = false;
        }

        static void Kill(GameObject go)
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        /// <summary>Reemplaza el collider de un objeto por una caja ajustada a su modelo.</summary>
        static void RefitCollider(GameObject host, GameObject model)
        {
            foreach (var c in host.GetComponents<Collider>()) Object.DestroyImmediate(c);
            var b = new Bounds(); bool init = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds);
            }
            var box = host.AddComponent<BoxCollider>();
            var ls = host.transform.lossyScale;
            box.center = host.transform.InverseTransformPoint(b.center);
            var sz = host.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
        }

        /// <summary>Easter egg o prop con componentes: oculta el placeholder y le cuelga el modelo.</summary>
        static GameObject Dress(GameObject host, string id, Vector3? worldPos = null, Quaternion? worldRot = null, bool refit = true)
        {
            if (host == null || !Exists(id)) return null;
            if (worldPos.HasValue) host.transform.position = worldPos.Value;
            var m = Model(id, host.transform, host.transform.position, worldRot ?? host.transform.rotation);
            Hide(host, m);
            if (refit) RefitCollider(host, m);
            return m;
        }

        static void AddMeshColliders(GameObject go)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static void AddBox(GameObject go)
        {
            if (go == null) return;
            var tmpHost = go;
            var b = new Bounds(); bool init = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds);
            }
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            var sz = go.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
        }

        // ------------------------------------------------------------------ texturas (Poly Haven, CC0)

        /// <summary>Material URP Lit con textura de color + normal, repetida 'tiling' veces. Se guarda como asset.</summary>
        static Material TexMat(string id, Vector2 tiling, Color tint, float smooth = 0.15f)
        {
            string dir = RickMortySetup.Root + "/Textures/" + id + "/";
            var col = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + id + "_color.jpg");
            if (col == null) return null;
            string np = dir + id + "_normal.jpg";
            if (AssetImporter.GetAtPath(np) is TextureImporter imp && imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
            }
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
            // Reutilizar el asset si ya existe (borrarlo dejaba en magenta a las superficies que ya lo usaban)
            string path = $"{RickMortySetup.Root}/Materials/Generated/Tex_{id}_{tiling.x:0.#}x{tiling.y:0.#}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", col);
            m.SetColor("_BaseColor", tint);
            m.mainTextureScale = tiling;
            m.SetTextureScale("_BaseMap", tiling);
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", smooth);
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Texturiza una caja: la repeticion sale del tamano real de su cara mas grande.</summary>
        static void ApplyTex(GameObject go, string id, float tileMeters, Color tint, float smooth = 0.15f)
        {
            if (go == null) return;
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var s = go.transform.lossyScale;
            Vector2 face = s.y <= Mathf.Min(s.x, s.z) ? new Vector2(s.x, s.z)      // piso / techo
                         : s.x <= s.z ? new Vector2(s.z, s.y)                       // pared delgada en X
                         : new Vector2(s.x, s.y);                                   // pared delgada en Z
            var m = TexMat(id, new Vector2(Mathf.Max(1, Mathf.Round(face.x / tileMeters)), Mathf.Max(1, Mathf.Round(face.y / tileMeters))), tint, smooth);
            if (m != null) r.sharedMaterial = m;
        }

        // ------------------------------------------------------------------ ronda 2: revamp del entorno

        static void Revamp()
        {
            // --- Texturas: garaje y exterior
            var garage = Find("Entorno/Garaje");
            if (garage != null)
                foreach (Transform t in garage.transform)
                {
                    if (t.name == "Piso") ApplyTex(t.gameObject, "garage_floor", 2f, Color.white, 0.25f);
                    else if (t.name.StartsWith("Pared")) ApplyTex(t.gameObject, "wood_plank_wall", 1.5f, new Color(1.5f, 1.38f, 1.25f));   // >1: aclara la madera (quedaba muy oscuro el garaje)
                }
            const string ex = "Entorno/Exterior";
            var ext = Find(ex);
            if (ext == null) return;
            foreach (Transform t in ext.transform)
            {
                if (t.name == "Cesped") ApplyTex(t.gameObject, "leafy_grass", 3f, new Color(0.78f, 1f, 0.72f));
                else if (t.name == "Calle") ApplyTex(t.gameObject, "asphalt_02", 4f, new Color(0.85f, 0.85f, 0.85f));
                else if (t.name.StartsWith("Acera")) ApplyTex(t.gameObject, "concrete_pavement", 1f, Color.white);
                else if (t.name.StartsWith("Fachada")) ApplyTex(t.gameObject, "painted_plaster_wall", 1.5f, new Color(1.5f, 1.47f, 1.42f));   // >1: la textura es gris, la fachada debe verse blanca
                else if (t.name == "TechoGaraje") ApplyTex(t.gameObject, "roof_09", 2f, new Color(0.75f, 0.62f, 0.55f));
            }
            // casa del vecino: conserva el modelo (sus ventanas son parte del juego) pero con revoque rosado y tejas
            var nbBody = Find(ex + "/Casa del Vecino/Cuerpo");
            if (nbBody != null) ApplyTex(nbBody, "painted_plaster_wall", 1.5f, new Color(1f, 0.72f, 0.78f));
            var nbRoof = Find(ex + "/Casa del Vecino/Techo");
            if (nbRoof != null) ApplyTex(nbRoof, "roof_09", 2f, new Color(1f, 0.55f, 0.5f));

            // --- Casas del barrio (reemplazan las cajas del horizonte, mirando a la calle)
            if (Exists("casa_barrio1") && Exists("casa_barrio2"))
            {
                int k = 0;
                foreach (var h in Children(ex, "CasaHorizonte").ToList())
                {
                    Hide(h);
                    var p = h.transform.position;
                    MakeStatic(Model(k % 2 == 0 ? "casa_barrio1" : "casa_barrio2", ext.transform, new Vector3(p.x + 1f, 0, p.z), Quaternion.Euler(0, -90, 0)));
                    k++;
                }
                foreach (var h in Children(ex, "TechoHorizonte")) Hide(h);
            }

            // --- Vegetacion y mobiliario urbano (fuera de las zonas de juego: patio entre garaje y vecino libre)
            void Place(string id, float x, float y, float z, float yaw, float scale = 1f)
            {
                if (Exists(id)) MakeStatic(Model(id, ext.transform, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), scale));
            }
            var rnd = new System.Random(7);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            foreach (var (x, z) in new[] { (37f, -33f), (37f, -19f), (37f, -5f), (37f, 9f), (37f, 23f), (37f, 35f),
                                           (-15f, 9f), (-8f, 10.5f), (-2f, 9f), (-13f, -9.5f), (-18f, -2f), (5f, 21f), (11f, 21f) })
                Place("arbol", x, 0, z, R(0, 360), R(0.8f, 1.1f));
            foreach (var (x, z) in new[] { (-12.8f, -4.3f), (-11f, -4.3f), (-8.4f, -4.3f), (6.8f, 9.4f), (12.3f, 9.4f), (3.6f, 9.4f) })
                Place("arbusto", x, 0, z, R(0, 360), R(0.85f, 1.15f));
            Place("buzon", -5.6f, 0, -8.8f, 180);
            for (float z = -32f; z <= 32f; z += 16f)
            {
                Place("farola", 13.15f, 0.07f, z, 180);      // el brazo de la farola apunta a -X del modelo
                Place("farola", 22.85f, 0.07f, z + 8f, 0);
            }
            Place("hidrante", 13.45f, 0.07f, -5.5f, 30);

            // --- Garaje: caneca del agujero negro (conserva el nucleo morado), boton MEZCLAR y reloj
            var bin = GameObject.Find("Agujero Negro (Basura)");
            if (bin != null && Exists("caneca"))
            {
                foreach (Transform t in bin.transform)
                    if (t.name == "Fondo" || t.name == "Pared") Hide(t.gameObject);
                Model("caneca", bin.transform, bin.transform.position, Quaternion.identity);
            }
            var btn = GameObject.Find("Boton MEZCLAR");
            if (btn != null && Exists("boton_rojo"))
            {
                var cap = btn.transform.Find("Cap");
                foreach (Transform t in btn.transform)
                    if (t.name == "Base" || t.name == "Cap") Hide(t.gameObject);
                // hijo del Cap: al presionar, MixButton baja el Cap y el modelo baja con el
                Model("boton_rojo", cap != null ? cap : btn.transform, btn.transform.position, btn.transform.rotation);
                var sc = btn.GetComponent<SphereCollider>();
                if (sc != null) { sc.center = new Vector3(0, 0.1f, 0); sc.radius = 0.065f; }
            }
            var clock = GameObject.Find("RelojPared");
            if (clock != null && Exists("reloj_pared"))
            {
                Hide(clock);
                Model("reloj_pared", clock.transform.parent, new Vector3(-0.4f, 2.6f, 3.975f), Quaternion.Euler(0, 180, 0));
            }
        }

        // ------------------------------------------------------------------ zonas

        static void Desk()
        {
            const string g = "Entorno/Mesa de Trabajo";
            if (Exists("mesa"))
            {
                Hide(Find(g + "/Tablero"));
                foreach (var p in Children(g, "Pata")) Hide(p);
                Kill(Find(g + "/Repisa"));
                Model("mesa", Find(g).transform, new Vector3(0, 0, 2.0f), Quaternion.Euler(0, 180, 0));
                // repisa inferior del modelo (a 0.31 m): colision invisible para apoyar cosas
                var shelf = new GameObject("RepisaInferior (colision)");
                shelf.transform.SetParent(Find(g).transform, false);
                shelf.transform.position = new Vector3(0, 0.29f, 2.0f);
                shelf.AddComponent<BoxCollider>().size = new Vector3(2.2f, 0.04f, 0.85f);
                shelf.AddComponent<SurfaceType>().kind = SurfaceKind.Wood;
            }
            var bowl = Find(g + "/Tazon de Mezclas");
            if (bowl != null && Exists("tazon"))
            {
                Hide(Find(g + "/Tazon de Mezclas/Fondo"));
                foreach (Transform t in bowl.transform)
                    if (t.name == "Pared") Hide(t.gameObject);
                Model("tazon", bowl.transform, bowl.transform.position, Quaternion.identity);
            }
            var arm = Find(g + "/Brazo Robotico");
            if (arm != null && Exists("brazo_robotico"))
            {
                Hide(arm);
                Model("brazo_robotico", arm.transform, new Vector3(0, 3.0f, 2.25f), Quaternion.Euler(180, 30, 0));
            }
            var tool = Find(g + "/CajaHerramientas");
            if (tool != null && Exists("caja_herramientas"))
            {
                Kill(tool);
                var m = Model("caja_herramientas", Find(g).transform, new Vector3(0.75f, 0.31f, 2.15f), Quaternion.Euler(0, 15, 0));
                AddBox(m);
            }
            Dress(Find("Casco Iron Man (easter egg)"), "casco_ironman", new Vector3(0.3f, 0.33f, 2.05f), Quaternion.Euler(0, 200, 0));
            // "Chatarra": 5 cubos grises sueltos en la repisa inferior que se veian de placeholder → 2 cajitas de carton
            if (Exists("caja_carton"))
            {
                foreach (var c in Children(g, "Chatarra")) Kill(c);
                foreach (var (x, z, yaw, s) in new[] { (-0.7f, 1.95f, 12f, 0.5f), (-0.25f, 2.1f, -20f, 0.4f) })
                    Model("caja_carton", Find(g).transform, new Vector3(x, 0.31f, z), Quaternion.Euler(0, yaw, 0), s);
            }
        }

        static void Shelf()
        {
            const string g = "Entorno/Estanteria";
            var grp = Find(g);
            if (grp == null || !Exists("estanteria")) return;
            foreach (var n in new[] { "Poste", "Nivel" })
                foreach (var o in Children(g, n)) Kill(o);
            foreach (var x in new[] { -1.0f, 1.0f })
            {
                var m = Model("estanteria", grp.transform, new Vector3(x, 0, 3.7f), Quaternion.Euler(0, 180, 0));
                if (m != null) AddMeshColliders(m);
            }
            float L(int i) => ShelfLevels[i];
            GameObject Prop(string placeholder, string id, Vector3 pos, float yaw = 180)
            {
                var host = Find(g + "/" + placeholder);
                if (!Exists(id)) return null;
                if (host != null) Hide(host);
                return Model(id, grp.transform, pos, Quaternion.Euler(0, yaw, 0));
            }
            Prop("Calavera", "calavera", new Vector3(-1.55f, L(2), 3.62f), 200);
            Prop("Mazo", "mazo", new Vector3(-1.0f, L(2), 3.62f), 100);
            Hide(Find(g + "/MazoCabeza"));
            Prop("CoronaAlien", "corona", new Vector3(-0.35f, L(2), 3.62f));
            Prop("CabezaMonstruo", "cabeza_monstruo", new Vector3(0.5f, L(2), 3.62f), 170);
            var ttBox = Prop("Caja TIME TRAVEL STUFF", "caja_carton", new Vector3(1.3f, L(3), 3.62f));
            if (ttBox != null)
            {
                Kill(Find(g + "/Label_TIME TRAVEL"));
                // el collider del placeholder (oculto) debe coincidir con la caja nueva: si sobresale,
                // empuja al Cubo Prison Realm (que va encima) fuera de la repisa
                var ttHost = Find(g + "/Caja TIME TRAVEL STUFF");
                if (ttHost != null)
                {
                    ttHost.transform.position = new Vector3(1.3f, L(3) + 0.1565f, 3.62f);
                    ttHost.transform.localScale = new Vector3(0.45f, 0.313f, 0.353f);
                }
                // rotulo escrito con marcador en la cara que mira al cuarto (+Z local del modelo)
                Sticker(ttBox, "TIME TRAVEL\nSTUFF", new Vector3(0, 0.16f, 0.181f), Quaternion.Euler(0, 180, -4), 0.55f, new Color(0.12f, 0.08f, 0.05f));
            }
            // frascos de formol apoyados en la repisa: se mueven juntas todas sus piezas (vidrio, formol, tapa, muestras)
            float jarDy = float.NaN;
            foreach (var j in Children(g, "FrascoConserva")) { jarDy = L(5) + 0.1f - j.transform.position.y; break; }
            if (!float.IsNaN(jarDy))
                foreach (var n in new[] { "FrascoConserva", "Formol", "TapaFrasco", "RoscaTapa", "MuestraOjo", "MuestraPupila", "MuestraPepino" })
                    foreach (var j in Children(g, n)) j.transform.position += Vector3.up * jarDy;
            foreach (var p in Children(g, "PlanosDoblados"))
                p.transform.position = new Vector3(1.3f, L(3) + 0.315f, 3.6f);
            foreach (var n in new[] { "CajaPlastico1", "CajaPlastico2" })
                foreach (var b in Children(g, n))
                {
                    b.transform.position = new Vector3(b.transform.position.x, L(5) + 0.14f, b.transform.position.z);
                    // la caja lisa gris se veia de placeholder: modelo de carton (mismo tamano aprox.), collider intacto
                    if (Exists("caja_carton"))
                    {
                        Hide(b);
                        Model("caja_carton", grp.transform, new Vector3(b.transform.position.x, L(5), 3.66f), Quaternion.Euler(0, n.EndsWith("1") ? 176 : 184, 0));
                    }
                }

            Dress(Find("Cubo Prison Realm (easter egg)"), "cubo_prison", new Vector3(1.22f, L(3) + 0.32f, 3.6f), Quaternion.Euler(0, 160, 0));
            var ball = Find("Esfera del Dragon (easter egg)");
            if (ball != null) Dress(ball, "esfera_dragon", new Vector3(-1.28f, L(5) + 0.09f, 3.7f), Quaternion.Euler(0, 180, 0));
            Dress(Find("Figura Miku (easter egg)"), "figura_miku", new Vector3(-0.65f, L(5), 3.62f), Quaternion.Euler(0, 180, 0));
            Dress(Find("Llama Fortnite (easter egg)"), "llama", new Vector3(1.3f, L(5) + 0.12f, 3.6f), Quaternion.Euler(0, 180, 0));
        }

        static void Laundry()
        {
            const string g = "Entorno/Lavanderia";
            var grp = Find(g);
            if (grp == null || !Exists("lavadora")) return;
            // Los modelos reales son mas anchos: se reacomoda la lavanderia a lo largo de la pared izquierda
            void Appliance(string name, string id, float z, Vector3 colliderSize, bool fillShell = false)
            {
                var host = Find(g + "/" + name);
                if (host == null) return;
                host.transform.position = new Vector3(-3.62f, colliderSize.y / 2, z);
                host.transform.localScale = new Vector3(colliderSize.x, colliderSize.y, colliderSize.z);
                Hide(host);
                if (fillShell)
                {
                    // El modelo de lavadora no tiene caras arriba/atras: la caja crema del placeholder,
                    // un poco mas chica, rellena el hueco (sin sus hijos, que son la puerta vieja)
                    host.GetComponent<Renderer>().enabled = true;
                    host.transform.localScale = new Vector3(colliderSize.x * 0.94f, colliderSize.y * 0.985f, colliderSize.z * 0.94f);
                    host.transform.position = new Vector3(-3.64f, colliderSize.y * 0.985f / 2, z);
                }
                Model(id, grp.transform, new Vector3(-3.62f, 0, z), Quaternion.Euler(0, 90, 0));
            }
            Appliance("Lavadora", "lavadora", 0.0f, new Vector3(0.66f, 0.88f, 0.97f), true);
            Appliance("Secadora", "lavadora", 1.05f, new Vector3(0.66f, 0.88f, 0.97f), true);
            if (Exists("fregadero"))
                Appliance("Fregadero", "fregadero", 2.6f, new Vector3(0.7f, 0.92f, 1.58f));
            Hide(Find(g + "/Mugre"));
            var flag = Find("Calcomania Colombia (easter egg)");
            if (flag != null) flag.transform.position = new Vector3(-3.27f, 0.72f, 0.0f);
            foreach (var s in Children(g, "Calcetin")) s.transform.position += new Vector3(0, 0, -0.7f);
            var ball = Find("Pokebola (easter egg)");
            if (ball != null) Dress(ball, "pokebola", new Vector3(-3.0f, 0.0f, 0.55f), Quaternion.Euler(0, 120, 0));
            var spot = Find("Marcadores/Cuantico_Lavadora");
            if (spot != null) spot.transform.position = new Vector3(-3.6f, 0.9f, 0.0f);
        }

        static void Corner()
        {
            const string g = "Entorno/Rincon Derecho";
            var grp = Find(g);
            if (grp == null) return;
            if (Exists("escoba"))
            {
                Hide(Find(g + "/Escoba")); Hide(Find(g + "/Cerdas"));
                Model("escoba", grp.transform, new Vector3(3.8f, 0, 3.72f), Quaternion.Euler(-8, 90, 6));
            }
            if (Exists("recogedor"))
            {
                Hide(Find(g + "/Recogedor"));
                Model("recogedor", grp.transform, new Vector3(3.55f, 0, 3.8f), Quaternion.Euler(0, 200, 0));
            }
            var capy = Find("Capibara (easter egg)");
            if (capy != null && Exists("capibara"))
            {
                var m = Dress(capy, "capibara", new Vector3(3.3f, 0.08f, 3.0f), Quaternion.LookRotation(new Vector3(-1, 0, -0.2f)));
                var c = capy.GetComponent<Capybara>();
                if (c != null) c.ear = null;
                if (m != null)
                {
                    // gafas VR (el modelo descargado no las trae): carcasa negra + visor azul + correa
                    var vr = new GameObject("Gafas VR").transform;
                    vr.SetParent(m.transform, false);
                    vr.localPosition = new Vector3(0, 0.6f, 0.43f);
                    var ls = m.transform.lossyScale;
                    vr.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
                    void Part(PrimitiveType t, Vector3 pos, Vector3 size, Color col, float emi = 0)
                    {
                        var p = GameObject.CreatePrimitive(t);
                        Object.DestroyImmediate(p.GetComponent<Collider>());
                        p.name = "GafasPieza"; p.transform.SetParent(vr, false);
                        p.transform.localPosition = pos; p.transform.localScale = size;
                        p.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(col, emi);
                    }
                    Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.2f, 0.09f, 0.09f), new Color(0.06f, 0.06f, 0.07f));
                    Part(PrimitiveType.Cube, new Vector3(0, 0, 0.047f), new Vector3(0.17f, 0.06f, 0.01f), new Color(0.2f, 0.55f, 1f), 1.2f);
                    Part(PrimitiveType.Cube, new Vector3(0, 0.01f, -0.12f), new Vector3(0.215f, 0.025f, 0.2f), new Color(0.12f, 0.12f, 0.13f));
                }
            }
        }

        static void Lamps()
        {
            if (!Exists("lampara_fluorescente")) return;
            var grp = Find("Entorno/Tubos Fluorescentes");
            if (grp == null) return;
            foreach (Transform root in grp.transform)
            {
                foreach (Transform c in root)
                    if (c.name != "Luz") Hide(c.gameObject);
                Model("lampara_fluorescente", root, root.position + Vector3.up * 0.18f, Quaternion.identity);
                var fl = root.GetComponent<FlickerLight>();
                if (fl != null) fl.tube = null;
            }
        }

        static void GarageDoor()
        {
            var door = Find("Entorno/Garaje/PuertaGaraje");
            if (door == null || !Exists("puerta_garaje")) return;
            var gd = door.GetComponent<global::RickMorty.GarageDoor>();
            var panel = Find("Entorno/Garaje/PuertaGaraje/Panel");
            var root = new GameObject("PanelMovil (bisagra)").transform;
            root.SetParent(door.transform, false);
            root.localPosition = new Vector3(0, 2.5f, 0);   // bisagra en el borde superior
            panel.transform.SetParent(root, true);
            Hide(panel);
            Model("puerta_garaje", root, new Vector3(4.02f, 0, 0.5f), Quaternion.Euler(0, 90, 0));
            gd.panel = root;
            gd.swingUp = true;
            gd.swingAngle = -90f;
        }

        static void Exterior()
        {
            const string g = "Entorno/Exterior";
            var grp = Find(g);
            if (grp == null) return;
            if (Exists("aro"))
            {
                foreach (var n in new[] { "Tablero", "Aro" })
                    foreach (var o in Children(g, n)) Hide(o);
                Model("aro", grp.transform, new Vector3(4.62f, 3.95f, 0.5f), Quaternion.Euler(0, 90, 0));   // el tablero queda en la fachada (x=4.16); el aro sobresale hacia la entrada
            }
            var house = Find(g + "/Casa Smith");
            if (house != null && Exists("casa_smith"))
            {
                Hide(house);
                foreach (var o in Children(g, "Baldosa")) { }
                Model("casa_smith", grp.transform, new Vector3(-9.1f, 0, 1.0f), Quaternion.Euler(0, 180, 0));
            }
            var nb = Find(g + "/Casa del Vecino");
            if (nb != null && Exists("casa_vecino"))
            {
                foreach (Transform t in nb.transform.Cast<Transform>().ToList())
                {
                    if (t.name == "VentanaVecino" && Mathf.Abs(t.position.x - 9f) < 0.1f) { Kill(t.gameObject); continue; }
                    if (t.name.StartsWith("Label")) continue;
                    Hide(t.gameObject);
                }
                Model("casa_vecino", nb.transform, new Vector3(7.5f, 0, 13.5f), Quaternion.Euler(0, 180, 0));
            }
            if (Exists("valla"))
            {
                foreach (var n in new[] { "Valla", "VallaRiel", "VallaRiel2" })
                    foreach (var o in Children(g, n)) Hide(o);
                foreach (var (x, sx) in new[] { (5.3f, 1f), (7.15f, 0.85f), (10.2f, 1f), (12.2f, 1f) })
                {
                    var m = Model("valla", grp.transform, new Vector3(x, 0, 7.5f), Quaternion.identity);
                    if (m != null) m.transform.localScale = new Vector3(sx, 1, 1);
                }
            }
        }

        static void Perimeter()
        {
            const string g = "Entorno/Perimetro FBI";
            var grp = Find(g);
            if (grp == null) return;
            bool barr = Exists("barricada"), cone = Exists("cono"), van = Exists("camioneta");
            foreach (Transform t in grp.transform.Cast<Transform>().ToList())
            {
                var p = t.position;
                if (barr && t.name == "Barricada") { Hide(t.gameObject); Model("barricada", grp.transform, new Vector3(p.x, 0, p.z), Quaternion.identity); }
                else if (barr && t.name == "FranjaNaranja") Hide(t.gameObject);
                else if (cone && t.name == "Cono") { Hide(t.gameObject); Model("cono", grp.transform, new Vector3(p.x, 0, p.z), Quaternion.Euler(0, Random.Range(0, 90f), 0)); }
                else if (van && t.name == "Camioneta FBI")
                {
                    Hide(t.gameObject);
                    var m = Model("camioneta", grp.transform, new Vector3(p.x, 0, p.z), Quaternion.Euler(0, p.z > 0 ? 180 : 0, 0));
                    // rotulos FBI amarillos a ambos lados (el modelo descargado es una SUV negra sin marcas)
                    foreach (var s in new[] { -1f, 1f })
                        Sticker(m, "FBI", new Vector3(s * 1.07f, 1.05f, -0.3f), Quaternion.Euler(0, s * -90f, 0), 5f, new Color(1f, 0.85f, 0.1f));
                }
                else if (van && t.name == "Label_FBI") Kill(t.gameObject);
            }
        }

        static void Rick()
        {
            if (!Exists("rick")) return;
            var rick = GameObject.Find("Rick (placeholder - pedir modelo)");
            if (rick != null)
            {
                var vis = rick.transform.Find("Visual");
                Hide(vis.gameObject);
                var m = Model("rick", vis, vis.position, vis.rotation);
                rick.name = "Rick";
                foreach (Transform t in rick.transform.Cast<Transform>().ToList())
                    if (t.name.StartsWith("Label")) Kill(t.gameObject);
                var mouth = rick.GetComponent<RickNPC>().mouth;
                if (mouth != null) mouth.SetParent(rick.transform, true);
            }
            var holo = GameObject.Find("Holograma de Rick");
            if (holo != null)
            {
                var hv = holo.transform.Find("Visual");
                Hide(hv.gameObject);
                var m = Model("rick", hv, hv.position, hv.rotation, 0.35f);
                var glass = MatLib.Transparent(new Color(0.4f, 0.9f, 1f, 0.45f));
                var rends = m.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = glass;
                    r.sharedMaterials = mats;
                }
                var anim = m.GetComponentInChildren<Animation>();
                if (anim != null) Object.DestroyImmediate(anim);
                holo.GetComponent<RickHologram>().parts = rends;
            }
        }
    }
}
