using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace RickMorty.EditorTools
{
    /// <summary>
    /// Paso 2: construye la escena completa con placeholders (primitivas + colores del diseno):
    /// garaje, mesa de mezclas, estanteria, lavanderia, easter eggs, exterior suburbano,
    /// casa del vecino, perimetro del FBI, XR Origin con reloj y todos los sistemas conectados.
    /// Se puede re-ejecutar: regenera la escena desde cero.
    /// </summary>
    public static class RickMortySceneBuilder
    {
        public const string ScenePath = RickMortySetup.Root + "/Scenes/RickGarage.unity";

        // Colores del diseno
        static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
        static readonly Color WoodDark = new Color(0.36f, 0.24f, 0.13f);
        static readonly Color Concrete = new Color(0.52f, 0.52f, 0.5f);
        static readonly Color MetalGray = new Color(0.45f, 0.47f, 0.5f);
        static readonly Color GreenGray = new Color(0.42f, 0.5f, 0.44f);
        static readonly Color White = new Color(0.93f, 0.93f, 0.9f);
        static readonly Color Grass = new Color(0.33f, 0.6f, 0.25f);
        static readonly Color Asphalt = new Color(0.2f, 0.2f, 0.22f);
        static readonly Color Pink = new Color(1f, 0.55f, 0.7f);
        static readonly Color Brick = new Color(0.62f, 0.2f, 0.16f);
        static readonly Color Roof = new Color(0.45f, 0.28f, 0.18f);

        static Transform s_Env;

        [MenuItem("Rick y Morty/2. Construir escena del garaje", priority = 2)]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Construir escena", "Esto regenera la escena " + ScenePath + " desde cero. ¿Continuar?", "Si", "Cancelar"))
                return;
            Build();
        }

        public static void BatchBuild()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogError("[RickMorty] Build de escena fallo: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        public static void Build()
        {
            GameDatabase.Load();
            RickMortySetup.SetupFont();
            RickMortySetup.CreateMaterialLibrary();
            AssetDatabase.SaveAssets();
            RickMortySetup.BeginPersistMaterials();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                s_Env = new GameObject("Entorno").transform;

                BuildLighting();
                BuildGarageShell(out var garageDoor, out var windowPos);
                var flickers = BuildFluorescents();
                var desk = BuildDesk();
                BuildShelfAndBoards(out var monitor, out var cork);
                BuildLaundryAndCorner();
                var exterior = BuildExterior();
                var boundary = BuildPerimeter();

                // ---------------- Jugador ----------------
                var rig = BuildPlayer(out var watch, out var hologram, out var leftController, out var rightController, out var head);

                // ---------------- Sistemas ----------------
                var systems = new GameObject("Sistemas");
                var comms = systems.AddComponent<RickComms>();
                comms.voiceSource = watch.transform;
                var timer = systems.AddComponent<GameTimer>();
                var score = systems.AddComponent<ScoreManager>();
                systems.AddComponent<DebugCheats>();
                var music = systems.AddComponent<AudioSource>();
                music.loop = true;
                music.playOnAwake = false;
                music.volume = 0.25f;
                music.spatialBlend = 0f;
                var bgm = Resources.Load<AudioClip>("Audio/Music/bgm");
                if (bgm != null) music.clip = bgm;

                var director = new GameObject("Director de Eventos").AddComponent<EventDirector>();
                director.transform.position = new Vector3(0, 0, 1);
                director.lights = flickers;
                director.steveSpawn = Marker("SpawnTioSteve", new Vector3(7f, 0.05f, 0.5f));
                director.meeseeksSpawn = Marker("SpawnMeeseeksPirata", new Vector3(5.2f, 0.05f, 1.5f));

                var pause = new GameObject("Menu de Pausa").AddComponent<PauseMenu>();
                pause.head = head;
                pause.rightControllerRoot = rightController;

                var endRoom = BuildEndRoom();

                // Rick y su holograma
                var rick = BuildRick(out var rickHolo);

                var gmGo = new GameObject("GameManager");
                var gm = gmGo.AddComponent<GameManager>();
                gm.rig = rig;
                gm.comms = comms;
                gm.monitor = monitor;
                gm.corkBoard = cork;
                gm.mixingBowl = desk.bowl;
                gm.finalStation = desk.station;
                gm.trashHole = desk.trash;
                gm.timer = timer;
                gm.score = score;
                gm.eventDirector = director;
                gm.endScreen = endRoom;
                gm.garageDoor = garageDoor;
                gm.rickNPC = rick;
                gm.rickHologram = rickHolo;
                gm.playerStart = Marker("InicioJugador", new Vector3(0, 0, 0.95f), Quaternion.identity);
                gm.onboardingMarker = BuildOnboardingMarker(new Vector3(-1.6f, 0.012f, -0.4f));
                gm.ceilingPortalPoint = Marker("PortalTecho", new Vector3(0.0f, 2.75f, 1.8f));
                gm.eyeholesSpot = Marker("SpotEyeholes", windowPos + new Vector3(0.75f, -0.1f, 0), Quaternion.LookRotation(Vector3.left));
                gm.quantumSpots = new[]
                {
                    Marker("Cuantico_Lavadora", new Vector3(-3.5f, 0.92f, 1.2f), Quaternion.LookRotation(Vector3.right)),
                    Marker("Cuantico_BrazoRobotico", new Vector3(2.9f, 0f, 3.2f), Quaternion.LookRotation(Vector3.left)),
                    Marker("Cuantico_Estanteria", new Vector3(-2.6f, 0f, -1.2f), Quaternion.LookRotation(Vector3.forward)),
                    Marker("Cuantico_Entrada", new Vector3(8f, 0f, 0.5f), Quaternion.LookRotation(Vector3.left)),
                    Marker("Cuantico_JuntoMesa", new Vector3(-1.5f, 0f, 1.3f), Quaternion.LookRotation(Vector3.forward)),
                };
                gm.music = music;
                boundary.respawnPoint = gm.playerStart;

                // Modelos reales (descargados/generados) sobre los placeholders
                RickMortySetup.CreateVisualRegistry();
                RickMortyModelPass.Apply();

                // Guardar
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
                var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[RickMorty] Escena construida y guardada en " + ScenePath);
            }
            finally
            {
                RickMortySetup.EndPersistMaterials();
            }
        }

        // ==================================================================
        //  Helpers de geometria
        // ==================================================================

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
            Quaternion? rot = null, bool collider = true, SurfaceKind? surface = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : s_Env, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // El collider del cilindro de Unity es una capsula: aplastada se vuelve una esfera
                // enorme que sobresale (empujaba los frascos fuera del tazon). Usar una caja.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<BoxCollider>();
            }
            if (surface.HasValue) go.AddComponent<SurfaceType>().kind = surface.Value;
            go.isStatic = parent == null || parent == s_Env;
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Color color, SurfaceKind? surface = null, bool collider = true, float emission = 0f, Quaternion? rot = null)
            => Prim(PrimitiveType.Cube, name, parent, pos, size, MatLib.Lit(color, emission), rot, collider, surface);

        static Material[] s_Blueprints;

        /// <summary>Guarda como PNG una textura generada por codigo (para que el material de la escena la conserve).</summary>
        static Texture2D TexAsset(string name, Func<Texture2D> build, bool alpha)
        {
            string dir = RickMortySetup.Root + "/Textures/Generadas";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(RickMortySetup.Root + "/Textures", "Generadas");
            string path = dir + "/" + name + ".png";
            var src = build();
            System.IO.File.WriteAllBytes(path, src.EncodeToPNG());
            Object.DestroyImmediate(src);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp && (imp.alphaIsTransparency != alpha || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.alphaIsTransparency = alpha;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Material con textura guardado como asset (se reutiliza entre construcciones de la escena).</summary>
        static Material TexturedMat(string name, Texture2D tex, Color tint, bool transparent, float smooth = 0.2f)
        {
            string path = RickMortySetup.GenMatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find(transparent ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            if (transparent) MatLib.MakeTransparent(m);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            if (!transparent) m.SetFloat("_Smoothness", smooth);
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Color color, bool collider = true, float emission = 0f, Quaternion? rot = null)
            => Prim(PrimitiveType.Cylinder, name, parent, pos, size, MatLib.Lit(color, emission), rot, collider);

        static GameObject Sph(string name, Transform parent, Vector3 pos, Vector3 size, Color color, bool collider = true, float emission = 0f)
            => Prim(PrimitiveType.Sphere, name, parent, pos, size, MatLib.Lit(color, emission), null, collider);

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : s_Env, false);
            return t;
        }

        static Transform Marker(string name, Vector3 pos, Quaternion? rot = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(GetMarkers(), false);
            t.position = pos;
            t.rotation = rot ?? Quaternion.identity;
            return t;
        }

        static Transform s_Markers;
        static Transform GetMarkers()
        {
            if (s_Markers == null) s_Markers = new GameObject("Marcadores").transform;
            return s_Markers;
        }

        /// <summary>Copia del material de la fuente que solo dibuja la cara delantera (asset en Fonts/).</summary>
        static Material FrontFaceFontMaterial(TMP_FontAsset font)
        {
            const string path = RickMortySetup.Root + "/Fonts/Fuente Solo Frente.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null && font != null)
            {
                m = new Material(font.material) { name = "Fuente Solo Frente" };
                m.SetFloat("_CullMode", 2f);   // 2 = Back: oculta la cara trasera
                AssetDatabase.CreateAsset(m, path);
            }
            return m != null ? m : font.material;
        }

        static TextMeshPro Label(string text, Transform parent, Vector3 pos, Quaternion rot, float size, Color color, float width = 1f)
        {
            var go = new GameObject("Label_" + text.Split('\n')[0]);
            go.transform.SetParent(parent != null ? parent : s_Env, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var t = go.AddComponent<TextMeshPro>();
            // fondo oscuro detras del texto: se lee sobre cualquier fondo (brillos verdes, paredes claras)
            const string mark = "<mark=#07111DD0 padding=\"18,18,8,8\">";
            t.text = mark + text.Replace("\n", "</mark>\n" + mark) + "</mark>";
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.sizeDelta = new Vector2(width, size * 0.2f + 0.05f);
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        static EasterEgg MakeEgg(GameObject go, string id, string display, string comment, bool grabbable)
        {
            if (grabbable)
            {
                if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
                var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // objetos chicos lanzados fuerte atravesaban paredes de 5 cm y caian para siempre
                var g = go.AddComponent<XRGrabInteractable>();
                g.useDynamicAttach = true;
                go.isStatic = false;
            }
            else
            {
                if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
                go.AddComponent<XRSimpleInteractable>();
            }
            var egg = go.AddComponent<EasterEgg>();
            egg.eggId = id;
            egg.displayName = display;
            egg.rickComment = comment;
            return egg;
        }

        // ==================================================================
        //  Iluminacion
        // ==================================================================

        /// <summary>Cielo real (Poly Haven "kloofendal 48d partly cloudy", CC0) en vez del cielo por defecto de Unity.</summary>
        static Material SkyMaterial()
        {
            string tex = RickMortySetup.Root + "/Textures/sky/sky_partly_cloudy.jpg";
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
            if (t == null) return RenderSettings.skybox;
            if (AssetImporter.GetAtPath(tex) is TextureImporter imp && (imp.maxTextureSize != 4096 || imp.wrapModeV != TextureWrapMode.Clamp || imp.mipmapEnabled))
            {
                imp.maxTextureSize = 4096;
                imp.wrapModeU = TextureWrapMode.Repeat;
                imp.wrapModeV = TextureWrapMode.Clamp;
                imp.mipmapEnabled = false;          // el cielo siempre se ve a la misma escala: los mipmaps solo lo emborronan
                imp.SaveAndReimport();
                t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
            }
            string path = RickMortySetup.Root + "/Materials/Generated/Cielo.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Skybox/Panoramic"));
            m.SetTexture("_MainTex", t);
            m.SetFloat("_Exposure", 1.0f);
            m.SetFloat("_Rotation", SkyRotation);
            m.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }
        const float SkyRotation = 110f;   // gira el panorama para que su sol coincida con la luz direccional

        static Light BuildLighting()
        {
            var sunGo = new GameObject("Sol");
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.38f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.15f);
            RenderSettings.sun = sun;
            RenderSettings.fog = false;
            RenderSettings.skybox = SkyMaterial();

            var ls = new LightingSettings { bakedGI = false, realtimeGI = false };
            ls.name = "RickGarageLighting";
            var lsPath = RickMortySetup.Root + "/Scenes/RickGarageLighting.lighting";
            System.IO.Directory.CreateDirectory(RickMortySetup.Root + "/Scenes");
            AssetDatabase.DeleteAsset(lsPath);
            AssetDatabase.CreateAsset(ls, lsPath);
            Lightmapping.lightingSettings = ls;
            return sun;
        }

        static List<FlickerLight> BuildFluorescents()
        {
            var list = new List<FlickerLight>();
            var g = Group("Tubos Fluorescentes");
            float[] zs = { -0.6f, 1.4f, 3.5f };   // la de atras en 3.5: en 3.0 el tubo atravesaba la pantalla del monitor
            foreach (var z in zs)
            {
                var root = new GameObject("Fluorescente").transform;
                root.SetParent(g, false);
                root.position = new Vector3(0, 2.8f, z);
                Box("Cadena1", root, new Vector3(-0.5f, 0.1f, 0), new Vector3(0.01f, 0.2f, 0.01f), MetalGray, collider: false);
                Box("Cadena2", root, new Vector3(0.5f, 0.1f, 0), new Vector3(0.01f, 0.2f, 0.01f), MetalGray, collider: false);
                Box("Carcasa", root, new Vector3(0, 0.02f, 0), new Vector3(1.3f, 0.04f, 0.14f), MetalGray, collider: false);
                var tube = Prim(PrimitiveType.Cylinder, "Tubo", root, Vector3.zero, new Vector3(0.05f, 0.62f, 0.05f), MatLib.Lit(new Color(0.9f, 1f, 0.95f), 2.5f), Quaternion.Euler(0, 0, 90), false);
                var lightGo = new GameObject("Luz");
                lightGo.transform.SetParent(root, false);
                lightGo.transform.localPosition = new Vector3(0, -0.15f, 0);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 5f;   // 3 luces de 6.5 m cubrian TODO el garaje por pixel (caro en Quest)
                l.intensity = 2.2f;
                l.color = new Color(0.92f, 1f, 0.95f);
                l.shadows = LightShadows.None;
                var fl = root.gameObject.AddComponent<FlickerLight>();
                fl.targetLight = l;
                fl.tube = tube.GetComponent<Renderer>();
                fl.baseIntensity = 2.2f;
                list.Add(fl);
            }
            return list;
        }

        // ==================================================================
        //  Garaje
        // ==================================================================

        static void BuildGarageShell(out GarageDoor door, out Vector3 windowPos)
        {
            var g = Group("Garaje");
            Box("Piso", g, new Vector3(0, -0.05f, 0.5f), new Vector3(8f, 0.1f, 7f), Concrete, SurfaceKind.Ground);
            Box("Techo", g, new Vector3(0, 3.05f, 0.5f), new Vector3(8.2f, 0.1f, 7.2f), WoodDark, SurfaceKind.Wood);
            Box("ParedFondo", g, new Vector3(0, 1.5f, 4.05f), new Vector3(8.2f, 3f, 0.1f), Wood, SurfaceKind.Wood);
            Box("ParedFrontal", g, new Vector3(0, 1.5f, -3.05f), new Vector3(8.2f, 3f, 0.1f), Wood, SurfaceKind.Wood);
            Box("ParedIzquierda", g, new Vector3(-4.05f, 1.5f, 0.5f), new Vector3(0.1f, 3f, 7f), Wood, SurfaceKind.Wood);

            // Pared derecha con abertura de la puerta del garaje (z -1.25..2.25) y ventana (z 2.75..3.45, y 1.2..1.9)
            Box("ParedDer_A", g, new Vector3(4.05f, 1.5f, -2.125f), new Vector3(0.1f, 3f, 1.75f), Wood, SurfaceKind.Wood);
            Box("ParedDer_SobrePuerta", g, new Vector3(4.05f, 2.75f, 0.5f), new Vector3(0.1f, 0.5f, 3.5f), Wood, SurfaceKind.Wood);
            Box("ParedDer_B1", g, new Vector3(4.05f, 1.5f, 2.5f), new Vector3(0.1f, 3f, 0.5f), Wood, SurfaceKind.Wood);
            Box("ParedDer_B2", g, new Vector3(4.05f, 1.5f, 3.725f), new Vector3(0.1f, 3f, 0.55f), Wood, SurfaceKind.Wood);
            Box("ParedDer_BajoVentana", g, new Vector3(4.05f, 0.6f, 3.1f), new Vector3(0.1f, 1.2f, 0.7f), Wood, SurfaceKind.Wood);
            Box("ParedDer_SobreVentana", g, new Vector3(4.05f, 2.45f, 3.1f), new Vector3(0.1f, 1.1f, 0.7f), Wood, SurfaceKind.Wood);
            var glass = Prim(PrimitiveType.Cube, "Ventana", g, new Vector3(4.05f, 1.55f, 3.1f), new Vector3(0.02f, 0.7f, 0.7f), MatLib.Transparent(new Color(0.7f, 0.85f, 1f, 0.18f)));
            glass.AddComponent<SurfaceType>().kind = SurfaceKind.Glass;
            windowPos = new Vector3(4.05f, 1.55f, 3.1f);
            Box("MarcoVentana", g, new Vector3(4.0f, 1.55f, 3.1f), new Vector3(0.04f, 0.04f, 0.72f), WoodDark, collider: false);

            // Vigas y pilares expuestos
            for (int i = 0; i < 5; i++)
                Box("Viga", g, new Vector3(0, 2.92f, -2.5f + i * 1.6f), new Vector3(8f, 0.16f, 0.14f), WoodDark, collider: false);
            foreach (var x in new[] { -3.9f, 3.9f })
                foreach (var z in new[] { -2.9f, 0.5f, 3.9f })
                    Box("Pilar", g, new Vector3(x, 1.5f, z), new Vector3(0.16f, 3f, 0.16f), WoodDark, SurfaceKind.Wood);

            // Puerta de la casa (izquierda, bloqueada) + lampara naranja manchada
            Box("PuertaCasa", g, new Vector3(-3.97f, 1.05f, -1.6f), new Vector3(0.06f, 2.1f, 0.95f), WoodDark, SurfaceKind.Wood);
            Sph("Perilla", g, new Vector3(-3.92f, 1.0f, -1.25f), Vector3.one * 0.06f, new Color(0.8f, 0.7f, 0.3f), false);
            Label("BLOQUEADA", g, new Vector3(-3.92f, 1.6f, -1.6f), Quaternion.Euler(0, -90, 0), 0.8f, new Color(1, 0.4f, 0.3f));
            var lamp = Sph("LamparaNaranja", g, new Vector3(-3.9f, 2.0f, -0.85f), new Vector3(0.22f, 0.28f, 0.22f), new Color(1f, 0.55f, 0.15f), false, 1.2f);
            var ll = new GameObject("LuzLampara").AddComponent<Light>();
            ll.transform.SetParent(lamp.transform, false);
            ll.type = LightType.Point; ll.range = 2.5f; ll.intensity = 1f; ll.color = new Color(1f, 0.6f, 0.3f); ll.shadows = LightShadows.None;
            for (int i = 0; i < 4; i++)
                Sph("Mancha", lamp.transform, UnityEngine.Random.onUnitSphere * 0.45f, Vector3.one * 0.15f, new Color(0.2f, 0.12f, 0.05f), false);

            // Puerta metalica del garaje
            var doorRoot = new GameObject("PuertaGaraje").transform;
            doorRoot.SetParent(g, false);
            doorRoot.position = new Vector3(4.02f, 0, 0.5f);
            var panel = Box("Panel", doorRoot, new Vector3(0, 1.25f, 0), new Vector3(0.07f, 2.5f, 3.5f), new Color(0.75f, 0.77f, 0.78f), SurfaceKind.Metal);
            for (int i = 0; i < 5; i++)
                Box("Ranura", panel.transform, new Vector3(-0.6f, -0.4f + i * 0.2f, 0), new Vector3(0.3f, 0.01f, 0.98f), MetalGray, collider: false);
            door = doorRoot.gameObject.AddComponent<GarageDoor>();
            door.panel = panel.transform;
            door.openHeight = 2.35f;
        }

        // ==================================================================
        //  Mesa de trabajo (centro de accion)
        // ==================================================================

        struct DeskRefs
        {
            public MixingBowl bowl;
            public FinalAssemblyStation station;
            public BlackHoleTrash trash;
        }

        static DeskRefs BuildDesk()
        {
            var refs = new DeskRefs();
            var g = Group("Mesa de Trabajo");
            const float top = 0.9f;
            Box("Tablero", g, new Vector3(0, top - 0.025f, 2.0f), new Vector3(2.4f, 0.05f, 1.0f), Wood, SurfaceKind.Wood);
            foreach (var x in new[] { -1.12f, 1.12f })
                foreach (var z in new[] { 1.56f, 2.44f })
                    Box("Pata", g, new Vector3(x, (top - 0.05f) * 0.5f, z), new Vector3(0.08f, top - 0.05f, 0.08f), WoodDark, SurfaceKind.Wood);
            Box("Repisa", g, new Vector3(0, 0.25f, 2.0f), new Vector3(2.2f, 0.03f, 0.9f), WoodDark, SurfaceKind.Wood);

            // Planos esparcidos
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(UnityEngine.Random.Range(-1f, 1f), top + 0.002f, UnityEngine.Random.Range(1.6f, 2.4f));
                if (Mathf.Abs(p.x) < 0.5f) continue;
                var plano = Box("Plano", g, p, new Vector3(0.3f, 0.002f, 0.22f), new Color(0.2f, 0.35f, 0.7f), collider: false,
                    rot: Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0));
                // plano tecnico dibujado (antes era un rectangulo azul liso)
                s_Blueprints ??= new[]
                {
                    TexturedMat("Plano_pistola", TexAsset("plano_pistola", () => ProcTex.BuildBlueprint(0), false), Color.white, false, 0.05f),
                    TexturedMat("Plano_portal", TexAsset("plano_portal", () => ProcTex.BuildBlueprint(1), false), Color.white, false, 0.05f)
                };
                plano.GetComponent<Renderer>().sharedMaterial = s_Blueprints[i % 2];
            }
            // Cristales morados brillando en base metalica (decoracion)
            var cbase = Cyl("BaseCristales", g, new Vector3(-0.45f, top + 0.02f, 2.38f), new Vector3(0.18f, 0.02f, 0.18f), MetalGray);
            // racimo de cristales (mismo modelo que va dentro del frasco de Cristales Kalaxianos); si falta, las 3 barritas de antes
            var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RickMortySetup.Root + "/Models/Env/relleno_kalaxianos.glb");
            if (crystalPrefab != null)
            {
                var cg = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab);
                cg.name = "CristalesDeco (modelo)";
                cg.transform.SetParent(cbase.transform.parent, true);
                cg.transform.position = new Vector3(-0.45f, top + 0.04f, 2.38f);
                cg.transform.localScale = Vector3.one * 0.16f;
            }
            else for (int i = 0; i < 3; i++)
                Box("CristalDeco", cbase.transform.parent, new Vector3(-0.45f + (i - 1) * 0.05f, top + 0.09f, 2.38f), new Vector3(0.035f, 0.12f, 0.035f),
                    new Color(0.6f, 0.2f, 1f), collider: false, emission: 2f, rot: Quaternion.Euler(UnityEngine.Random.Range(-15, 15), 0, UnityEngine.Random.Range(-15, 15)));

            // ---------- Tazon de mezclas ----------
            var bowlRoot = new GameObject("Tazon de Mezclas").transform;
            bowlRoot.SetParent(g, false);
            bowlRoot.position = new Vector3(0, top, 2.05f);
            var glassMat = MatLib.Transparent(new Color(0.8f, 0.95f, 1f, 0.25f));
            Prim(PrimitiveType.Cylinder, "Fondo", bowlRoot, new Vector3(0, 0.012f, 0), new Vector3(0.44f, 0.012f, 0.44f), glassMat);
            int segments = 14;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var seg = Prim(PrimitiveType.Cube, "Pared", bowlRoot, dir * 0.235f + Vector3.up * 0.08f, new Vector3(0.11f, 0.15f, 0.02f), glassMat,
                    Quaternion.LookRotation(dir) * Quaternion.Euler(12f, 0, 0));
                seg.isStatic = true;
            }
            var liquid = Prim(PrimitiveType.Cylinder, "Liquido", bowlRoot, new Vector3(0, 0.03f, 0), new Vector3(0.42f, 0.01f, 0.42f),
                MatLib.Transparent(new Color(0.3f, 0.8f, 0.4f, 0.6f)), collider: false);
            // halo suave alrededor del tazon (antes un disco verde plano de 60 cm que parecia un plato)
            var glow = Prim(PrimitiveType.Quad, "Brillo", bowlRoot, new Vector3(0, 0.003f, 0), new Vector3(0.95f, 0.95f, 1f),
                TexturedMat("Halo_tazon", TexAsset("halo_suave", () => ProcTex.BuildSoftGlow(), true), new Color(0.3f, 1f, 0.4f, 0.14f), true),
                Quaternion.Euler(90, 0, 0), collider: false);
            var bl = new GameObject("LuzTazon").AddComponent<Light>();
            bl.transform.SetParent(bowlRoot, false);
            bl.transform.localPosition = new Vector3(0, 0.25f, 0);
            bl.type = LightType.Point; bl.range = 1.0f; bl.intensity = 0.3f; bl.shadows = LightShadows.None;
            var bowl = bowlRoot.gameObject.AddComponent<MixingBowl>();
            bowl.innerCenter = new Vector3(0, 0.1f, 0);
            bowl.innerRadius = 0.23f;
            bowl.innerHeight = 0.3f;
            bowl.liquid = liquid.GetComponent<Renderer>();
            bowl.glow = glow.GetComponent<Renderer>();
            bowl.bowlLight = bl;
            Label("TAZÓN DE MEZCLAS", g, new Vector3(0, top + 0.2f, 2.33f), Quaternion.identity, 0.5f, new Color(0.7f, 1f, 0.7f));

            // Cuchara de madera y pinzas (decoracion)
            Box("Cuchara", g, new Vector3(0.3f, top + 0.012f, 2.3f), new Vector3(0.03f, 0.015f, 0.3f), new Color(0.75f, 0.55f, 0.3f), collider: false, rot: Quaternion.Euler(0, 20, 0));
            Box("Pinzas", g, new Vector3(-0.3f, top + 0.01f, 2.3f), new Vector3(0.015f, 0.012f, 0.2f), MetalGray, collider: false, rot: Quaternion.Euler(0, -25, 0));

            // ---------- Zona de salida ----------
            // Bandeja de salida: plato de metal oscuro con un aro verde luminoso (antes un disco verde liso)
            Cyl("BandejaAro", g, new Vector3(0, top + 0.003f, 1.66f), new Vector3(0.265f, 0.003f, 0.265f), new Color(0.25f, 1f, 0.35f), false, 0.9f);
            var outPad = Prim(PrimitiveType.Cylinder, "BandejaSalida", g, new Vector3(0, top + 0.006f, 1.66f), new Vector3(0.24f, 0.006f, 0.24f),
                MatLib.Lit(new Color(0.16f, 0.17f, 0.19f), 0f, 0.7f, 0.85f), collider: false);
            Cyl("BandejaCentro", g, new Vector3(0, top + 0.0125f, 1.66f), new Vector3(0.05f, 0.0005f, 0.05f), new Color(0.25f, 1f, 0.35f), false, 0.9f);
            var output = new GameObject("PuntoSalida").transform;
            output.SetParent(g, false);
            output.position = new Vector3(0, top + 0.06f, 1.66f);
            bowl.outputPoint = output;

            // ---------- Boton MEZCLAR ----------
            var btnRoot = new GameObject("Boton MEZCLAR").transform;
            btnRoot.SetParent(g, false);
            btnRoot.position = new Vector3(-0.42f, top, 1.72f);
            Cyl("Base", btnRoot, new Vector3(0, 0.02f, 0), new Vector3(0.14f, 0.02f, 0.14f), new Color(0.15f, 0.15f, 0.15f));
            var cap = Cyl("Cap", btnRoot, new Vector3(0, 0.052f, 0), new Vector3(0.1f, 0.015f, 0.1f), new Color(0.9f, 0.1f, 0.1f), false, 0.6f);
            var btnCol = btnRoot.gameObject.AddComponent<SphereCollider>();
            btnCol.center = new Vector3(0, 0.06f, 0);
            btnCol.radius = 0.06f;
            var simple = btnRoot.gameObject.AddComponent<XRSimpleInteractable>();
            btnRoot.gameObject.AddComponent<XRPokeFilter>();
            var mixBtn = btnRoot.gameObject.AddComponent<MixButton>();
            mixBtn.cap = cap.transform;
            mixBtn.bowl = bowl;
            Label("MEZCLAR", btnRoot, new Vector3(0, 0.16f, 0.02f), Quaternion.Euler(20, 0, 0), 0.6f, new Color(1f, 0.4f, 0.35f));
            refs.bowl = bowl;

            // ---------- Soporte de ensamblaje final ----------
            var stRoot = new GameObject("Soporte de Ensamblaje").transform;
            stRoot.SetParent(g, false);
            stRoot.position = new Vector3(0.4f, top, 1.66f);
            // plataforma metalica con aro luminoso por debajo y dos horquillas que sostienen la pistola
            Cyl("Anillo", stRoot, new Vector3(0, 0.004f, 0), new Vector3(0.255f, 0.004f, 0.255f), new Color(0.3f, 1f, 0.3f), false, 0.9f);
            Prim(PrimitiveType.Cylinder, "Plataforma", stRoot, new Vector3(0, 0.015f, 0), new Vector3(0.23f, 0.015f, 0.23f),
                MatLib.Lit(new Color(0.2f, 0.21f, 0.23f), 0f, 0.7f, 0.85f));
            var sockGo = new GameObject("SocketPortalCarton");
            sockGo.transform.SetParent(stRoot, false);
            sockGo.transform.localPosition = new Vector3(0, 0.12f, 0);
            sockGo.transform.localRotation = Quaternion.LookRotation(Vector3.right);
            var sc = sockGo.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.12f;
            var stSocket = sockGo.AddComponent<IngredientSocket>();
            stSocket.acceptedIds = new List<string> { "portal_carton" };
            var station = stRoot.gameObject.AddComponent<FinalAssemblyStation>();
            station.socket = stSocket;
            station.zoneRadius = 0.3f;
            Label("SOPORTE DE\nENSAMBLAJE", stRoot, new Vector3(0, 0.28f, 0.05f), Quaternion.Euler(15, 0, 0), 0.4f, new Color(0.6f, 1f, 0.6f));
            refs.station = station;

            // ---------- Ingredientes base en sus soportes ----------
            BuildIngredientRacks(g, top);

            // ---------- Agujero negro de basura ----------
            var bin = new GameObject("Agujero Negro (Basura)").transform;
            bin.SetParent(g, false);
            bin.position = new Vector3(-1.6f, 0, 1.5f);
            Cyl("Fondo", bin, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.02f, 0.5f), new Color(0.1f, 0.1f, 0.12f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Prim(PrimitiveType.Cube, "Pared", bin, dir * 0.24f + Vector3.up * 0.3f, new Vector3(0.16f, 0.6f, 0.03f), MatLib.Lit(new Color(0.15f, 0.15f, 0.18f), 0f, 0.6f, 0.7f),
                    Quaternion.LookRotation(dir));
            }
            var core = Prim(PrimitiveType.Sphere, "Nucleo", bin, new Vector3(0, 0.25f, 0), Vector3.one * 0.25f, MatLib.Unlit(Color.black), collider: false);
            Prim(PrimitiveType.Cylinder, "Disco", core.transform, Vector3.zero, new Vector3(1.8f, 0.01f, 1.8f), MatLib.Lit(new Color(0.6f, 0.2f, 1f), 3f), collider: false);
            var trig = bin.gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0, 0.35f, 0);
            trig.size = new Vector3(0.4f, 0.5f, 0.4f);
            var trash = bin.gameObject.AddComponent<BlackHoleTrash>();
            trash.core = core.transform;
            Label("BASURA\n(agujero negro)", bin, new Vector3(0, 0.85f, 0), Quaternion.LookRotation(Vector3.Scale(bin.position - new Vector3(0, 0, 1.3f), new Vector3(1, 0, 1)).normalized), 0.5f, new Color(0.8f, 0.6f, 1f));
            refs.trash = trash;

            // ---------- Brazo robotico ----------
            var arm = Group("Brazo Robotico", g);
            Cyl("Base", arm, new Vector3(0, 2.98f, 2.2f), new Vector3(0.3f, 0.04f, 0.3f), GreenGray, false);
            Cyl("Seg1", arm, new Vector3(0, 2.62f, 2.2f), new Vector3(0.09f, 0.35f, 0.09f), GreenGray, false);
            Sph("Junta", arm, new Vector3(0, 2.26f, 2.2f), Vector3.one * 0.14f, MetalGray, false);
            Cyl("Seg2", arm, new Vector3(0, 2.05f, 2.05f), new Vector3(0.07f, 0.25f, 0.07f), GreenGray, false, 0f, Quaternion.Euler(-35, 0, 0));
            Box("Pinza1", arm, new Vector3(0.04f, 1.78f, 1.9f), new Vector3(0.02f, 0.14f, 0.03f), MetalGray, collider: false, rot: Quaternion.Euler(0, 0, 15));
            Box("Pinza2", arm, new Vector3(-0.04f, 1.78f, 1.9f), new Vector3(0.02f, 0.14f, 0.03f), MetalGray, collider: false, rot: Quaternion.Euler(0, 0, -15));

            // ---------- Bajo la mesa: caja de herramientas, chatarra y casco de Iron Man ----------
            Box("CajaHerramientas", g, new Vector3(0.75f, 0.39f, 2.15f), new Vector3(0.4f, 0.25f, 0.22f), new Color(0.7f, 0.1f, 0.1f), SurfaceKind.Metal);
            for (int i = 0; i < 5; i++)
                Box("Chatarra", g, new Vector3(UnityEngine.Random.Range(-0.9f, 0.0f), 0.37f, UnityEngine.Random.Range(1.7f, 2.3f)), Vector3.one * UnityEngine.Random.Range(0.06f, 0.14f), MetalGray,
                    collider: false, rot: Quaternion.Euler(UnityEngine.Random.Range(0, 90), UnityEngine.Random.Range(0, 90), 0));
            var helmet = Sph("Casco Iron Man (easter egg)", null, new Vector3(0.35f, 0.42f, 2.1f), new Vector3(0.22f, 0.26f, 0.24f), new Color(0.7f, 0.08f, 0.08f), true);
            Box("Mascara", helmet.transform, new Vector3(0, -0.05f, -0.42f), new Vector3(0.75f, 0.6f, 0.2f), new Color(0.85f, 0.7f, 0.2f), collider: false);
            Box("OjoBueno", helmet.transform, new Vector3(0.18f, 0.1f, -0.52f), new Vector3(0.2f, 0.06f, 0.05f), new Color(0.8f, 1f, 1f), collider: false, emission: 2f);
            MakeEgg(helmet, "ironman", "Casco de Iron Man", "Ese casco tenía una IA decente. Ahora controla mi tostadora.", true);

            return refs;
        }

        /// <summary>Parte un nombre largo en dos lineas por el espacio mas cercano a la mitad.</summary>
        static string SplitTwoLines(string s)
        {
            int best = -1, mid = s.Length / 2;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == ' ' && (best < 0 || Mathf.Abs(i - mid) < Mathf.Abs(best - mid))) best = i;
            return best < 0 ? s : s.Substring(0, best) + "\n" + s.Substring(best + 1);
        }

        static void BuildIngredientRacks(Transform g, float top)
        {
            var racks = Group("Ingredientes Base", g);
            // Liquidos a la izquierda, solidos a la derecha (los cristales atras para no activar el ensamblaje)
            var layout = new (string id, Vector3 pos)[]
            {
                ("mega_semilla",   new Vector3(-0.95f, 0, 1.88f)),
                ("fleeb",          new Vector3(-0.70f, 0, 1.88f)),
                ("acido",          new Vector3(-0.95f, 0, 2.22f)),
                ("materia_oscura", new Vector3(-0.70f, 0, 2.22f)),
                ("meeseeks",       new Vector3(0.72f, 0, 1.92f)),
                ("plumbus_polvo",  new Vector3(0.97f, 0, 1.92f)),
                ("esporas",        new Vector3(0.72f, 0, 2.25f)),
                ("kalaxianos",     new Vector3(0.97f, 0, 2.25f)),
            };
            Box("SoporteLiquidos", racks, new Vector3(-0.825f, top + 0.01f, 2.05f), new Vector3(0.55f, 0.02f, 0.6f), MetalGray, SurfaceKind.Metal);
            Box("SoporteSolidos", racks, new Vector3(0.845f, top + 0.01f, 2.085f), new Vector3(0.55f, 0.02f, 0.6f), MetalGray, SurfaceKind.Metal);
            Label("LÍQUIDOS", racks, new Vector3(-0.825f, top + 0.3f, 2.4f), Quaternion.identity, 0.5f, new Color(1f, 0.85f, 0.4f));
            Label("SÓLIDOS", racks, new Vector3(0.845f, top + 0.3f, 2.43f), Quaternion.identity, 0.5f, new Color(0.6f, 0.85f, 1f));

            foreach (var (id, p) in layout)
            {
                var def = GameDatabase.GetItem(id);
                float h = def.shape == "flask" ? 0.095f : 0.085f;
                var pos = new Vector3(p.x, top + 0.02f + h, p.z);

                var socketGo = new GameObject("Soporte_" + id);
                socketGo.transform.SetParent(racks, false);
                socketGo.transform.position = pos;
                var col = socketGo.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.07f;
                var socket = socketGo.AddComponent<IngredientSocket>();
                socket.acceptedIds = new List<string> { id };
                socket.recycleDelayTime = 0.5f;
                socket.showInteractableHoverMeshes = false;
                Cyl("Aro", socketGo.transform, new Vector3(0, -h + 0.002f, 0), new Vector3(0.11f, 0.002f, 0.11f), def.Color, false, 1f);

                var flask = ItemFactory.Build(id, pos, Quaternion.identity, false);
                flask.transform.SetParent(racks, true);
                var bi = flask.AddComponent<BaseIngredient>();
                bi.homeSocket = socket;
                bi.homePosition = pos;
                bi.homeRotation = Quaternion.identity;
                socket.startingSelectedInteractable = flask.GetComponent<XRGrabInteractable>();

                // Placa fija con el nombre (2 lineas). Antes el nombre solo flotaba al acercarse a < 1.1 m y las
                // etiquetas vecinas se montaban; desde el puesto de trabajo no se sabia cual frasco era cual.
                bool backRow = p.z > 2.1f;
                string plate = def.name.Length > 13 && def.name.Contains(" ") ? SplitTwoLines(def.name) : def.name;
                if (backRow) Label(plate, racks, new Vector3(p.x, top + 0.30f, p.z + 0.13f), Quaternion.Euler(8, 0, 0), 0.27f, Color.Lerp(def.Color, Color.white, 0.55f));
                else Label(plate, racks, new Vector3(p.x, top + 0.035f, p.z - 0.15f), Quaternion.Euler(62, 0, 0), 0.27f, Color.Lerp(def.Color, Color.white, 0.55f));
            }
        }

        // ==================================================================
        //  Estanteria, tablero de corcho y monitor
        // ==================================================================

        static void BuildShelfAndBoards(out RickMonitor monitor, out CorkBoard cork)
        {
            var g = Group("Estanteria");
            float x0 = -2f, x1 = 2f, zf = 3.45f, zb = 3.95f;
            foreach (var x in new[] { x0, x1 })
                foreach (var z in new[] { zf, zb })
                    Box("Poste", g, new Vector3(x, 1.25f, z), new Vector3(0.05f, 2.5f, 0.05f), MetalGray, SurfaceKind.Metal);
            float[] levels = { 0.4f, 1.0f, 1.6f, 2.2f };
            foreach (var y in levels)
                Box("Nivel", g, new Vector3(0, y, (zf + zb) * 0.5f), new Vector3(4.05f, 0.03f, 0.5f), MetalGray, SurfaceKind.Metal);

            // Objetos canonicos
            Sph("Calavera", g, new Vector3(-1.6f, 1.12f, 3.7f), new Vector3(0.18f, 0.2f, 0.2f), new Color(0.95f, 0.93f, 0.85f));
            var mace = Cyl("Mazo", g, new Vector3(-1.1f, 1.08f, 3.7f), new Vector3(0.04f, 0.2f, 0.04f), WoodDark, true, 0, Quaternion.Euler(0, 0, 80));
            Sph("MazoCabeza", g, new Vector3(-0.85f, 1.1f, 3.7f), Vector3.one * 0.14f, MetalGray, false);
            Cyl("CoronaAlien", g, new Vector3(-0.3f, 1.06f, 3.7f), new Vector3(0.2f, 0.05f, 0.2f), new Color(0.9f, 0.75f, 0.2f), true, 0.5f);
            var monster = Sph("CabezaMonstruo", g, new Vector3(0.5f, 1.15f, 3.7f), new Vector3(0.3f, 0.26f, 0.28f), new Color(0.2f, 0.65f, 0.2f));
            for (int i = 0; i < 5; i++)
                Box("Diente", monster.transform, new Vector3(-0.3f + i * 0.15f, -0.3f, -0.45f), new Vector3(0.06f, 0.2f, 0.06f), Color.white, collider: false);

            var ttBox = Box("Caja TIME TRAVEL STUFF", g, new Vector3(1.3f, 1.75f, 3.72f), new Vector3(0.45f, 0.28f, 0.35f), new Color(0.7f, 0.55f, 0.35f));
            Label("TIME TRAVEL\nSTUFF", g, new Vector3(1.3f, 1.75f, 3.54f), Quaternion.identity, 0.5f, new Color(0.15f, 0.1f, 0.05f), 0.45f);

            // Easter Egg 4: Cubo de Gojo (Prison Realm) como pisapapeles
            var gojo = Box("Cubo Prison Realm (easter egg)", null, new Vector3(0.95f, 1.66f, 3.62f), Vector3.one * 0.09f, new Color(0.25f, 0.2f, 0.3f));
            for (int i = 0; i < 6; i++)
                Box("OjoCerrado", gojo.transform, new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.3f, 0.3f), -0.51f), new Vector3(0.25f, 0.04f, 0.02f), new Color(0.1f, 0.05f, 0.1f), collider: false);
            var folded = Box("PlanosDoblados", g, new Vector3(0.95f, 1.625f, 3.62f), new Vector3(0.25f, 0.01f, 0.2f), new Color(0.2f, 0.35f, 0.7f), collider: false);
            // planos dibujados (el cubo es el "pisapapeles" que los sostiene); antes era una tabla azul lisa
            folded.GetComponent<Renderer>().sharedMaterial = TexturedMat("Plano_portal", TexAsset("plano_portal", () => ProcTex.BuildBlueprint(1), false), Color.white, false, 0.05f);
            MakeEgg(gojo, "gojo", "Cubo Prison Realm", "Es un buen pisapapeles. No lo abras, adentro hay un tipo de pelo blanco muy molesto.", true);

            // Easter Egg 5: Esfera del dragon de 4 estrellas en un frasco de formol
            var jarPos = new Vector3(-1.5f, 2.33f, 3.7f);
            for (int i = 0; i < 3; i++)
            {
                var jp = jarPos + Vector3.right * i * 0.22f;
                // vidrio casi transparente + formol verdoso adentro + tapa metalica con borde
                // sin colider: si no, el frasco tapaba el rayo y la esfera del dragon no se podia agarrar
                Prim(PrimitiveType.Cylinder, "FrascoConserva", g, jp, new Vector3(0.15f, 0.1f, 0.15f),
                    MatLib.Transparent(new Color(0.85f, 0.95f, 0.95f, 0.16f)), collider: false);
                Prim(PrimitiveType.Cylinder, "Formol", g, jp + Vector3.down * 0.02f, new Vector3(0.138f, 0.075f, 0.138f),
                    MatLib.Transparent(new Color(0.75f, 0.85f, 0.35f, 0.32f)), collider: false);
                Cyl("TapaFrasco", g, jp + Vector3.up * 0.108f, new Vector3(0.158f, 0.012f, 0.158f), MetalGray, false);
                Cyl("RoscaTapa", g, jp + Vector3.up * 0.094f, new Vector3(0.152f, 0.006f, 0.152f), new Color(0.35f, 0.33f, 0.3f), false);
            }
            // los otros dos frascos tambien tienen "muestras" de Rick
            Sph("MuestraOjo", g, jarPos + new Vector3(0, -0.03f, 0), Vector3.one * 0.06f, new Color(0.95f, 0.92f, 0.85f), false);
            Sph("MuestraPupila", g, jarPos + new Vector3(0, -0.03f, -0.026f), new Vector3(0.025f, 0.025f, 0.012f), new Color(0.1f, 0.35f, 0.15f), false);
            Prim(PrimitiveType.Capsule, "MuestraPepino", g, jarPos + new Vector3(0.44f, -0.025f, 0), new Vector3(0.045f, 0.055f, 0.045f),
                MatLib.Lit(new Color(0.3f, 0.5f, 0.15f)), collider: false);
            var ball = Sph("Esfera del Dragon (easter egg)", null, jarPos + new Vector3(0.22f, 0, 0), Vector3.one * 0.07f, new Color(1f, 0.55f, 0.1f), true, 1.5f);
            MakeEgg(ball, "dragonball", "Esfera del Dragón de 4 estrellas", "No la toques, Morty. Ya pedí mi deseo y el dragón me debe plata.", false);

            // Easter Egg 6: figura de Hatsune Miku con parche cibernetico, cerca del reloj
            var clock = Cyl("RelojPared", g, new Vector3(-0.4f, 2.6f, 3.98f), new Vector3(0.3f, 0.02f, 0.3f), White, false, 0, Quaternion.Euler(90, 0, 0));
            var miku = new GameObject("Figura Miku (easter egg)");
            miku.transform.position = new Vector3(-0.65f, 2.33f, 3.65f);
            Prim(PrimitiveType.Capsule, "Cuerpo", miku.transform, Vector3.zero, new Vector3(0.06f, 0.08f, 0.06f), MatLib.Lit(new Color(0.2f, 0.2f, 0.25f)), null, false);
            Prim(PrimitiveType.Sphere, "Cabeza", miku.transform, new Vector3(0, 0.1f, 0), Vector3.one * 0.06f, MatLib.Lit(new Color(1f, 0.9f, 0.8f)), null, false);
            Prim(PrimitiveType.Capsule, "ColetaIzq", miku.transform, new Vector3(-0.05f, 0.04f, 0), new Vector3(0.025f, 0.08f, 0.025f), MatLib.Lit(new Color(0.2f, 0.8f, 0.8f)), null, false);
            Prim(PrimitiveType.Capsule, "ColetaDer", miku.transform, new Vector3(0.05f, 0.04f, 0), new Vector3(0.025f, 0.08f, 0.025f), MatLib.Lit(new Color(0.2f, 0.8f, 0.8f)), null, false);
            Prim(PrimitiveType.Cube, "Parche", miku.transform, new Vector3(0.012f, 0.11f, -0.028f), new Vector3(0.02f, 0.015f, 0.005f), MatLib.Lit(Color.red, 2f), null, false);
            var mc = miku.AddComponent<BoxCollider>(); mc.center = new Vector3(0, 0.03f, 0); mc.size = new Vector3(0.14f, 0.2f, 0.08f);
            MakeEgg(miku, "miku", "Figura de Hatsune Miku", "Le instalé un parche cibernético. Ahora canta en frecuencias que solo oyen los perros.", true);

            // Easter Egg 7: Llama de Fortnite asomando de una caja de plastico gris
            Box("CajaPlastico1", g, new Vector3(0.4f, 2.36f, 3.7f), new Vector3(0.45f, 0.28f, 0.4f), new Color(0.6f, 0.6f, 0.62f));
            Box("CajaPlastico2", g, new Vector3(1.3f, 2.36f, 3.7f), new Vector3(0.45f, 0.28f, 0.4f), new Color(0.6f, 0.6f, 0.62f));
            var llama = new GameObject("Llama Fortnite (easter egg)");
            llama.transform.position = new Vector3(1.3f, 2.58f, 3.62f);
            Prim(PrimitiveType.Cube, "Cabeza", llama.transform, Vector3.zero, new Vector3(0.12f, 0.16f, 0.12f), MatLib.Lit(new Color(0.6f, 0.4f, 0.85f)), null, false);
            Prim(PrimitiveType.Cube, "OrejaIzq", llama.transform, new Vector3(-0.04f, 0.11f, 0), new Vector3(0.03f, 0.07f, 0.03f), MatLib.Lit(new Color(0.3f, 0.5f, 0.9f)), null, false);
            Prim(PrimitiveType.Cube, "OrejaDer", llama.transform, new Vector3(0.04f, 0.11f, 0), new Vector3(0.03f, 0.07f, 0.03f), MatLib.Lit(new Color(0.3f, 0.5f, 0.9f)), null, false);
            llama.AddComponent<BoxCollider>().size = new Vector3(0.14f, 0.25f, 0.14f);
            MakeEgg(llama, "llama", "Llama de Fortnite", "La usé para guardar pólvora interdimensional. Guarda mejor que una caja fuerte.", true);

            // Monitor colgante (objetivo + subtitulos)
            var monGo = new GameObject("Monitor de Rick");
            // A 1.75 m de quien trabaja en la mesa (antes 2.0-2.3 m) e inclinado hacia abajo, hacia el jugador
            // (antes 8 grados hacia ARRIBA). El borde inferior queda a 1.8 m: nadie mete la cabeza en la pantalla.
            monGo.transform.position = new Vector3(0, 2.22f, 2.95f);
            monGo.transform.rotation = Quaternion.Euler(-14f, 0, 0);
            Box("Carcasa", monGo.transform, new Vector3(0, 0, 0.03f), new Vector3(1.48f, 0.86f, 0.05f), new Color(0.15f, 0.17f, 0.15f));
            Box("Soporte", monGo.transform, new Vector3(0, 0.75f, 0.05f), new Vector3(0.05f, 0.7f, 0.05f), MetalGray, collider: false);
            monitor = monGo.AddComponent<RickMonitor>();
            monitor.sizeMeters = new Vector2(1.4f, 0.8f);

            // Tablero de corcho
            var corkGo = new GameObject("Tablero de Corcho");
            corkGo.transform.position = new Vector3(-2.95f, 1.6f, 3.97f);
            var corkCol = Box("CorchoFisico", corkGo.transform, new Vector3(0, 0, 0.02f), new Vector3(1.25f, 0.95f, 0.04f), new Color(0.62f, 0.45f, 0.28f), SurfaceKind.Cork);
            cork = corkGo.AddComponent<CorkBoard>();
            cork.sizeMeters = new Vector2(1.2f, 0.9f);
            var cc = cork.transform.Find("CorchoFisico");
            if (cc != null) cc.localPosition = new Vector3(0, 0, 0.03f);
        }

        // ==================================================================
        //  Lavanderia y rincon derecho
        // ==================================================================

        static void BuildLaundryAndCorner()
        {
            var g = Group("Lavanderia");
            var washer = Box("Lavadora", g, new Vector3(-3.6f, 0.45f, 1.2f), new Vector3(0.65f, 0.9f, 0.65f), new Color(0.92f, 0.9f, 0.82f), SurfaceKind.Metal);
            Cyl("PuertaLavadora", washer.transform, new Vector3(0.51f, 0.05f, 0), new Vector3(0.6f, 0.02f, 0.6f), new Color(0.3f, 0.35f, 0.4f), false, 0, Quaternion.Euler(0, 0, 90));
            var dryer = Box("Secadora", g, new Vector3(-3.6f, 0.45f, 1.95f), new Vector3(0.65f, 0.9f, 0.65f), new Color(0.9f, 0.86f, 0.75f), SurfaceKind.Metal);
            Cyl("PuertaSecadora", dryer.transform, new Vector3(0.51f, 0.05f, 0), new Vector3(0.6f, 0.02f, 0.6f), new Color(0.3f, 0.35f, 0.4f), false, 0, Quaternion.Euler(0, 0, 90));
            Box("Fregadero", g, new Vector3(-3.6f, 0.45f, 2.8f), new Vector3(0.65f, 0.9f, 0.75f), new Color(0.45f, 0.45f, 0.42f), SurfaceKind.Metal);
            Box("Mugre", g, new Vector3(-3.6f, 0.905f, 2.8f), new Vector3(0.5f, 0.01f, 0.6f), new Color(0.3f, 0.28f, 0.2f), collider: false);

            // Easter Egg 1: calcomania desgastada de Colombia en la lavadora
            var flag = new GameObject("Calcomania Colombia (easter egg)");
            flag.transform.position = new Vector3(-3.27f, 0.75f, 1.2f);
            flag.transform.rotation = Quaternion.Euler(0, 90, 0);
            Prim(PrimitiveType.Cube, "Amarillo", flag.transform, new Vector3(0, 0.025f, 0), new Vector3(0.15f, 0.05f, 0.004f), MatLib.Lit(new Color(0.85f, 0.75f, 0.3f)), null, false);
            Prim(PrimitiveType.Cube, "Azul", flag.transform, new Vector3(0, -0.0125f, 0), new Vector3(0.15f, 0.025f, 0.004f), MatLib.Lit(new Color(0.3f, 0.35f, 0.6f)), null, false);
            Prim(PrimitiveType.Cube, "Rojo", flag.transform, new Vector3(0, -0.0375f, 0), new Vector3(0.15f, 0.025f, 0.004f), MatLib.Lit(new Color(0.65f, 0.25f, 0.25f)), null, false);
            flag.AddComponent<BoxCollider>().size = new Vector3(0.16f, 0.11f, 0.03f);
            MakeEgg(flag, "colombia", "Calcomanía de Colombia", "Esa calcomanía la puso Morty hace años. O un Morty. Ya no sé cuál.", false);

            // Easter Egg 2: Pokebola con cables entre calcetines sucios
            var ball = new GameObject("Pokebola (easter egg)");
            ball.transform.position = new Vector3(-3.0f, 0.06f, 0.75f);
            Prim(PrimitiveType.Sphere, "Arriba", ball.transform, new Vector3(0, 0.01f, 0), new Vector3(0.1f, 0.06f, 0.1f), MatLib.Lit(new Color(0.9f, 0.1f, 0.1f)), null, false);
            Prim(PrimitiveType.Sphere, "Abajo", ball.transform, new Vector3(0, -0.015f, 0), new Vector3(0.1f, 0.06f, 0.1f), MatLib.Lit(Color.white), null, false);
            Prim(PrimitiveType.Cylinder, "Cable1", ball.transform, new Vector3(0.05f, 0.03f, 0), new Vector3(0.005f, 0.04f, 0.005f), MatLib.Lit(Color.yellow), Quaternion.Euler(0, 0, 50), false);
            Prim(PrimitiveType.Cylinder, "Cable2", ball.transform, new Vector3(0.04f, 0.03f, 0.02f), new Vector3(0.005f, 0.04f, 0.005f), MatLib.Lit(Color.blue), Quaternion.Euler(30, 0, 60), false);
            var bc = ball.AddComponent<SphereCollider>(); bc.radius = 0.05f;
            MakeEgg(ball, "pokeball", "Pokebola vieja", "Tenía un buen capacitor adentro. El bicho amarillo no estaba contento.", true);
            for (int i = 0; i < 2; i++)
                Box("Calcetin", g, new Vector3(-3.1f + i * 0.2f, 0.015f, 0.6f + i * 0.25f), new Vector3(0.08f, 0.03f, 0.22f), new Color(0.85f, 0.85f, 0.75f), collider: false, rot: Quaternion.Euler(0, 30 + i * 50, 0));

            // Rincon derecho: escoba, recogedor, robot arana, capibara
            var c = Group("Rincon Derecho");
            Cyl("Escoba", c, new Vector3(3.8f, 0.7f, 3.75f), new Vector3(0.03f, 0.7f, 0.03f), WoodDark, true, 0, Quaternion.Euler(-10, 0, 8));
            Box("Cerdas", c, new Vector3(3.75f, 0.08f, 3.7f), new Vector3(0.3f, 0.12f, 0.06f), new Color(0.7f, 0.6f, 0.3f), collider: false);
            Box("Recogedor", c, new Vector3(3.6f, 0.03f, 3.8f), new Vector3(0.25f, 0.04f, 0.22f), new Color(0.2f, 0.5f, 0.8f));
            var robot = Sph("Robot Arana", c, new Vector3(2.7f, 0.15f, 3.5f), new Vector3(0.3f, 0.18f, 0.3f), MetalGray);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Cyl("Pata", c, new Vector3(2.7f, 0.08f, 3.5f) + dir * 0.22f, new Vector3(0.02f, 0.12f, 0.02f), MetalGray, false, 0, Quaternion.LookRotation(dir) * Quaternion.Euler(60, 0, 0));
            }

            // Easter Egg 8: capibara con gafas VR sobre un saco de dormir
            Box("SacoDormir", c, new Vector3(3.3f, 0.04f, 3.1f), new Vector3(0.7f, 0.08f, 1.1f), new Color(0.3f, 0.45f, 0.3f), SurfaceKind.Ground);
            var capy = new GameObject("Capibara (easter egg)");
            capy.transform.position = new Vector3(3.3f, 0.25f, 3.1f);
            capy.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var capyBrown = MatLib.Lit(new Color(0.55f, 0.38f, 0.22f));
            Prim(PrimitiveType.Capsule, "Cuerpo", capy.transform, Vector3.zero, new Vector3(0.35f, 0.3f, 0.3f), capyBrown, Quaternion.Euler(90, 0, 0), false);
            Prim(PrimitiveType.Sphere, "Cabeza", capy.transform, new Vector3(0, 0.08f, 0.32f), new Vector3(0.22f, 0.2f, 0.26f), capyBrown, null, false);
            Prim(PrimitiveType.Cube, "GafasVR", capy.transform, new Vector3(0, 0.13f, 0.42f), new Vector3(0.2f, 0.08f, 0.08f), MatLib.Lit(new Color(0.1f, 0.1f, 0.1f)), null, false);
            Prim(PrimitiveType.Cube, "LedGafas", capy.transform, new Vector3(0.06f, 0.13f, 0.465f), new Vector3(0.02f, 0.02f, 0.005f), MatLib.Lit(new Color(0.3f, 1f, 1f), 3f), null, false);
            var ear = Prim(PrimitiveType.Sphere, "Oreja", capy.transform, new Vector3(0.08f, 0.2f, 0.3f), new Vector3(0.05f, 0.05f, 0.03f), capyBrown, null, false);
            Prim(PrimitiveType.Sphere, "Oreja2", capy.transform, new Vector3(-0.08f, 0.2f, 0.3f), new Vector3(0.05f, 0.05f, 0.03f), capyBrown, null, false);
            var capyCol = capy.AddComponent<BoxCollider>();
            capyCol.center = new Vector3(0, 0, 0.08f);
            capyCol.size = new Vector3(0.35f, 0.35f, 0.75f);
            var cr = capy.AddComponent<Creature>();
            cr.displayName = "Capibara";
            cr.splatColor = new Color(0.8f, 0.05f, 0.05f);
            cr.moralPenalty = 200;
            cr.killLineId = "kill_capybara";
            cr.invulnerableToLaser = true;
            capy.AddComponent<Capybara>().ear = ear.transform;
            MakeEgg(capy, "capybara", "Capibara cuántico", "Shhh. Está alcanzando el nirvana en realidad virtual.", false);
        }

        // ==================================================================
        //  Exterior: casa Smith, calle, casa del vecino
        // ==================================================================

        static Transform BuildExterior()
        {
            var g = Group("Exterior");
            Box("Cesped", g, new Vector3(5f, -0.06f, 2f), new Vector3(70f, 0.1f, 90f), Grass, SurfaceKind.Ground);
            // Entrada de concreto con grietas
            Box("EntradaConcreto", g, new Vector3(9f, -0.005f, 0.5f), new Vector3(10f, 0.02f, 4.2f), Concrete, SurfaceKind.Ground);
            for (int i = 0; i < 6; i++)
                Box("Grieta", g, new Vector3(UnityEngine.Random.Range(5f, 13f), 0.007f, UnityEngine.Random.Range(-1f, 2f)), new Vector3(UnityEngine.Random.Range(0.4f, 1.2f), 0.002f, 0.02f),
                    new Color(0.25f, 0.25f, 0.25f), collider: false, rot: Quaternion.Euler(0, UnityEngine.Random.Range(0, 180f), 0));

            // Fachada exterior del garaje (blanca) + zocalo de ladrillo + techo de tejas
            Box("FachadaDer_A", g, new Vector3(4.13f, 1.5f, -2.125f), new Vector3(0.05f, 3f, 1.75f), White, collider: false);
            Box("FachadaDer_B1", g, new Vector3(4.13f, 1.5f, 2.575f), new Vector3(0.05f, 3f, 0.35f), White, collider: false);
            Box("FachadaDer_B2", g, new Vector3(4.13f, 1.5f, 3.725f), new Vector3(0.05f, 3f, 0.55f), White, collider: false);
            Box("FachadaDer_B3", g, new Vector3(4.13f, 0.6f, 3.1f), new Vector3(0.05f, 1.2f, 0.7f), White, collider: false);
            Box("FachadaDer_B4", g, new Vector3(4.13f, 2.45f, 3.1f), new Vector3(0.05f, 1.1f, 0.7f), White, collider: false);
            Box("FachadaSobrePuerta", g, new Vector3(4.13f, 2.75f, 0.5f), new Vector3(0.05f, 0.5f, 3.5f), White, collider: false);
            Box("FachadaFrente", g, new Vector3(0, 1.5f, -3.13f), new Vector3(8.3f, 3f, 0.05f), White, collider: false);
            Box("FachadaFondo", g, new Vector3(0, 1.5f, 4.13f), new Vector3(8.3f, 3f, 0.05f), White, collider: false);
            Box("Zocalo1", g, new Vector3(4.16f, 0.25f, -2.125f), new Vector3(0.04f, 0.5f, 1.75f), Brick, collider: false);
            Box("Zocalo2", g, new Vector3(4.16f, 0.25f, 3.2f), new Vector3(0.04f, 0.5f, 1.6f), Brick, collider: false);
            Box("TechoGaraje", g, new Vector3(0, 3.2f, 0.5f), new Vector3(8.6f, 0.2f, 7.6f), Roof);
            // Aro de baloncesto sobre la puerta
            Box("Tablero", g, new Vector3(4.2f, 3.55f, 0.5f), new Vector3(0.04f, 0.6f, 0.9f), White, collider: false);
            Cyl("Aro", g, new Vector3(4.45f, 3.35f, 0.5f), new Vector3(0.45f, 0.01f, 0.45f), new Color(1f, 0.45f, 0.1f), false);

            // Casa Smith (dos pisos) a la izquierda del garaje
            var house = Group("Casa Smith", g);
            Box("Cuerpo", house, new Vector3(-9.1f, 3f, 1f), new Vector3(10f, 6f, 9f), White);
            Box("Techo", house, new Vector3(-9.1f, 6.6f, 1f), new Vector3(10.6f, 1.2f, 9.6f), Roof);
            Box("Zocalo", house, new Vector3(-9.1f, 0.25f, 1f), new Vector3(10.1f, 0.5f, 9.1f), Brick, collider: false);
            for (int i = 0; i < 3; i++)
                Box("VentanaCasa", house, new Vector3(-12f + i * 2.5f, 4.2f, -3.53f), new Vector3(1f, 1.1f, 0.05f), new Color(0.5f, 0.7f, 0.9f), collider: false, emission: 0.2f);
            Box("PuertaPrincipal", house, new Vector3(-6.5f, 1.1f, -3.53f), new Vector3(1f, 2.2f, 0.06f), WoodDark, collider: false);
            for (int i = 0; i < 6; i++)
                Box("Baldosa", g, new Vector3(-6.5f, -0.004f, -4.2f - i * 0.8f), new Vector3(0.8f, 0.02f, 0.5f), new Color(0.6f, 0.58f, 0.55f), SurfaceKind.Ground);

            // Calle y aceras
            Box("Calle", g, new Vector3(18f, -0.01f, 0f), new Vector3(8f, 0.04f, 90f), Asphalt, SurfaceKind.Ground);
            for (int i = -10; i <= 10; i++)
                Box("Linea", g, new Vector3(18f, 0.012f, i * 4f), new Vector3(0.15f, 0.005f, 1.8f), new Color(0.95f, 0.85f, 0.2f), collider: false);
            Box("AceraOeste", g, new Vector3(13.5f, 0.02f, 0f), new Vector3(1f, 0.1f, 90f), new Color(0.7f, 0.7f, 0.68f), SurfaceKind.Ground);
            Box("AceraEste", g, new Vector3(22.5f, 0.02f, 0f), new Vector3(1f, 0.1f, 90f), new Color(0.7f, 0.7f, 0.68f), SurfaceKind.Ground);

            // Casa del vecino (rosada, un piso) con ventanas "diana"
            var nb = Group("Casa del Vecino", g);
            Box("Cuerpo", nb, new Vector3(7.5f, 1.75f, 13.5f), new Vector3(9f, 3.5f, 7f), Pink);
            Box("Techo", nb, new Vector3(7.5f, 3.8f, 13.5f), new Vector3(9.5f, 0.6f, 7.5f), new Color(0.75f, 0.2f, 0.2f));
            Box("Puerta", nb, new Vector3(7.5f, 1.1f, 9.98f), new Vector3(1f, 2.2f, 0.06f), new Color(0.75f, 0.15f, 0.15f), collider: false);
            foreach (var x in new[] { 4.5f, 10.5f, 6f })
            {
                var w = Box("VentanaVecino", nb, new Vector3(x == 6f ? 9f : x, 1.8f, 9.97f), new Vector3(1.3f, 1.1f, 0.06f), new Color(0.55f, 0.75f, 0.95f), SurfaceKind.Glass, true, 0.3f);
                w.AddComponent<NeighborWindow>();
            }
            Label("¡NO ME TIREN BICHOS\nA LAS VENTANAS!", nb, new Vector3(7.5f, 3.1f, 9.9f), Quaternion.identity, 1.2f, Color.white, 4f);
            // Valla blanca con puerta
            for (float x = 4.3f; x <= 13f; x += 0.5f)
            {
                if (x > 8f && x < 9.2f) continue; // puerta transitable
                Box("Valla", g, new Vector3(x, 0.45f, 7.5f), new Vector3(0.08f, 0.9f, 0.05f), White);
            }
            Box("VallaRiel", g, new Vector3(6.15f, 0.7f, 7.5f), new Vector3(3.8f, 0.06f, 0.04f), White, collider: false);
            Box("VallaRiel2", g, new Vector3(11.1f, 0.7f, 7.5f), new Vector3(3.8f, 0.06f, 0.04f), White, collider: false);

            // Horizonte: otras casas suburbanas de colores
            var colors = new[] { new Color(0.6f, 0.8f, 0.95f), new Color(0.95f, 0.9f, 0.6f), new Color(0.7f, 0.95f, 0.7f), new Color(0.95f, 0.75f, 0.55f), new Color(0.85f, 0.7f, 0.95f) };
            for (int i = 0; i < 8; i++)
            {
                float z = -35f + i * 10f;
                Box("CasaHorizonte", g, new Vector3(29f, 2f, z), new Vector3(7f, 4f, 7f), colors[i % colors.Length], collider: false);
                Box("TechoHorizonte", g, new Vector3(29f, 4.4f, z), new Vector3(7.4f, 0.8f, 7.4f), Roof, collider: false);
            }
            // Cajas de carton y sandias alienigenas en el patio (para las armas de corte)
            for (int i = 0; i < 4; i++)
            {
                var box = Box("CajaCarton", null, new Vector3(9f + i * 0.9f, 0.2f, 3.8f), Vector3.one * 0.4f, new Color(0.7f, 0.55f, 0.35f));
                var rb = box.AddComponent<Rigidbody>(); rb.mass = 1f;
                box.isStatic = false;
            }
            return g;
        }

        static PlayAreaBoundary BuildPerimeter()
        {
            var g = Group("Perimetro FBI");
            var b = g.gameObject.AddComponent<PlayAreaBoundary>();
            b.allowedZones = new List<Bounds>
            {
                new Bounds(new Vector3(4.7f, 0, 3.45f), new Vector3(17.6f, 10f, 13.1f)),   // garaje + entrada + jardin vecino: x[-4.1,13.5] z[-3.1,10]
                new Bounds(new Vector3(18.25f, 0, 0f), new Vector3(9.5f, 10f, 76f)),       // calle: x[13.5,23] z[-38,38]
            };
            var yellow = new Color(1f, 0.85f, 0.1f);
            void Tape(Vector3 a, Vector3 c)
            {
                var mid = (a + c) * 0.5f + Vector3.up * 0.9f;
                var d = c - a;
                var len = d.magnitude;
                var rot = Quaternion.LookRotation(d.normalized);
                Box("Cinta", g, mid, new Vector3(0.02f, 0.08f, len), yellow, collider: false, emission: 0.3f, rot: rot);
                for (float t = 2f; t < len; t += 6f)
                {
                    var p = a + d.normalized * t;
                    Box("Poste", g, new Vector3(p.x, 0.5f, p.z), new Vector3(0.06f, 1f, 0.06f), new Color(1f, 0.45f, 0.1f), collider: false);
                    // legible desde ambos lados: dos letreros espalda con espalda, cada uno solo con su cara
                    // delantera (si no, TMP dibuja tambien la de atras y se ve un texto espejado encima)
                    foreach (var yaw in new[] { 90f, -90f })
                    {
                        var r = rot * Quaternion.Euler(0, yaw, 0);
                        var lbl = Label("FBI   NO PASAR", g, p + Vector3.up * 1.05f - r * Vector3.forward * 0.01f, r, 0.9f, new Color(1f, 0.85f, 0.1f), 2f);
                        lbl.fontSharedMaterial = FrontFaceFontMaterial(lbl.font);
                    }
                }
            }
            Tape(new Vector3(4.2f, 0, -3.1f), new Vector3(13.5f, 0, -3.1f));
            Tape(new Vector3(-4.1f, 0, 4.2f), new Vector3(-4.1f, 0, 10f));
            Tape(new Vector3(-4.1f, 0, 10f), new Vector3(3f, 0, 10f));
            Tape(new Vector3(13.5f, 0, 10f), new Vector3(13.5f, 0, 38f));
            Tape(new Vector3(13.5f, 0, -3.1f), new Vector3(13.5f, 0, -38f));
            Tape(new Vector3(13.5f, 0, 38f), new Vector3(23f, 0, 38f));
            Tape(new Vector3(13.5f, 0, -38f), new Vector3(23f, 0, -38f));
            Tape(new Vector3(23f, 0, -38f), new Vector3(23f, 0, 38f));

            // Barricadas, conos y camionetas del FBI en los extremos de la calle
            foreach (var z in new[] { -39f, 39f })
            {
                for (int i = 0; i < 4; i++)
                {
                    Box("Barricada", g, new Vector3(14.5f + i * 2.2f, 0.5f, z), new Vector3(1.8f, 0.25f, 0.1f), Color.white, emission: 0.1f);
                    Box("FranjaNaranja", g, new Vector3(14.5f + i * 2.2f, 0.5f, z - 0.051f), new Vector3(0.4f, 0.25f, 0.01f), new Color(1f, 0.4f, 0f), collider: false);
                    Cyl("Cono", g, new Vector3(15.5f + i * 2.2f, 0.25f, z + Mathf.Sign(-z) * 1f), new Vector3(0.25f, 0.25f, 0.25f), new Color(1f, 0.45f, 0.05f), false);
                }
                var van = Box("Camioneta FBI", g, new Vector3(18f, 1.1f, z + Mathf.Sign(z) * 4f), new Vector3(2.2f, 2f, 5f), new Color(0.05f, 0.05f, 0.07f));
                Label("FBI", g, new Vector3(16.85f, 1.3f, z + Mathf.Sign(z) * 4f), Quaternion.Euler(0, 90, 0), 4f, Color.white, 3f);
            }
            b.safeInterior = new Bounds(new Vector3(0, 1.5f, 0.5f), new Vector3(8f, 4f, 7f));
            return b;
        }

        // ==================================================================
        //  Jugador XR
        // ==================================================================

        static PlayerRig BuildPlayer(out WristWatch watch, out DiscoveryHologram hologram, out Transform left, out Transform right, out Transform head)
        {
            var originPrefab = FindPrefab("XR Origin (XR Rig)");
            if (originPrefab == null)
                throw new Exception("No se encontro el prefab 'XR Origin (XR Rig)'. Ejecuta primero 'Rick y Morty/1. Configurar proyecto'.");

            // Interaction Manager + EventSystem para UI XR
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<XRUIInputModule>();

            var originGo = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
            originGo.transform.position = new Vector3(0, 0, 0.95f);
            var origin = originGo.GetComponent<XROrigin>();
            var cam = origin.Camera;
            head = cam.transform;
            cam.nearClipPlane = 0.05f;   // 2 cm hacia parpadear (z-fighting) calcomanias y lineas lejanas; 5 cm sigue por delante de los fundidos (12 cm)
            var camData = cam.GetUniversalAdditionalCameraData();
            // apagado por defecto (en Quest cuesta una pasada extra por cuadro); ScreenFX lo enciende solo durante sus efectos
            if (camData != null) camData.renderPostProcessing = false;

            left = FindDeep(originGo.transform, "Left Controller");
            right = FindDeep(originGo.transform, "Right Controller");

            if (originGo.GetComponent<CharacterController>() == null)
            {
                var cc = originGo.AddComponent<CharacterController>();
                cc.radius = 0.2f;
                cc.height = 1.6f;
                cc.center = new Vector3(0, 0.8f, 0);
                cc.stepOffset = 0.1f;
            }

            CleanUpRig(originGo);
            originGo.layer = 2;   // "Ignore Raycast": los disparos (portal, laser) no chocan con la capsula del jugador

            // Manos que nos dio el profesor (carpeta Assets/Oculus Hands): cada prefab va como hijo de su control,
            // igual que en clase. Traen su Animator y el script AnimateHandOnInput (gatillo = pinza, grip = puno).
            AddProfessorHand(left, "Left Hand Model");
            AddProfessorHand(right, "Right Hand Model");

            var rig = originGo.AddComponent<PlayerRig>();
            rig.origin = origin;
            rig.head = head;
            rig.leftHand = left;
            rig.rightHand = right;

            var headCol = cam.gameObject.AddComponent<SphereCollider>();
            headCol.isTrigger = true;
            headCol.radius = 0.14f;
            cam.gameObject.AddComponent<PlayerHead>();
            cam.gameObject.AddComponent<ScreenFX>();
            var vignette = cam.gameObject.AddComponent<ComfortVignette>();
            vignette.origin = originGo.transform;

            // Reloj Multiversal en la muneca izquierda
            var watchGo = new GameObject("Reloj Multiversal");
            watchGo.transform.SetParent(left != null ? left : originGo.transform, false);
            watchGo.transform.localPosition = new Vector3(0, 0.03f, -0.15f);   // en la muneca, detras de la mano del profesor
            watchGo.transform.localRotation = Quaternion.identity;
            watch = watchGo.AddComponent<WristWatch>();
            watch.head = head;
            var holoGo = new GameObject("Holograma de Descubrimientos");
            holoGo.transform.SetParent(watchGo.transform, false);
            hologram = holoGo.AddComponent<DiscoveryHologram>();
            hologram.head = head;
            watch.hologram = hologram;

            // Simulador XR solo para el Editor (mouse/teclado)
            var simPrefab = FindPrefab("XR Interaction Simulator");
            if (simPrefab != null)
            {
                var simRoot = new GameObject("Simulador (solo Editor)");
                simRoot.tag = "EditorOnly";
                var sim = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                sim.transform.SetParent(simRoot.transform, false);
                // En el PC, WASD mueve la "cabeza" simulada: a 0.2 se sentia muy lento (comentario del equipo)
                foreach (var mb in sim.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    var so = new SerializedObject(mb);
                    var px = so.FindProperty("m_TranslateXSpeed");
                    if (px == null) continue;
                    px.floatValue = 0.42f;
                    var pz = so.FindProperty("m_TranslateZSpeed"); if (pz != null) pz.floatValue = 0.42f;
                    var py = so.FindProperty("m_TranslateYSpeed"); if (py != null) py.floatValue = 0f;   // sin volar (Q/E atravesaban el techo)
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                sim.SetActive(false);
                simRoot.AddComponent<EditorSimulatorToggle>().simulator = sim;
            }
            else Debug.LogWarning("[RickMorty] No se encontro el XR Interaction Simulator (opcional).");

            return rig;
        }

        /// <summary>
        /// El rig de los Starter Assets trae activos salto (boton A: sube la vista 1.25 m), escalada, "grab move" y
        /// teletransporte (palanca derecha hacia adelante: arco sin destino valido). El juego es solo caminar y girar.
        /// </summary>
        static void CleanUpRig(GameObject originGo)
        {
            foreach (var t in originGo.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == "Jump" || n == "Climb" || n == "Teleportation" || n == "Grab Move" || n == "Climb Teleport" || n == "Teleport Interactor")
                    t.gameObject.SetActive(false);
            }
            foreach (var mb in originGo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string tn = mb.GetType().Name;
                if (tn == "ControllerInputActionManager")
                {
                    // sin referencia al interactor de teletransporte, el sample no lo vuelve a encender
                    var so = new SerializedObject(mb);
                    var p = so.FindProperty("m_TeleportInteractor");
                    if (p != null) { p.objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo(); }
                }
                else if (mb is UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider snap)
                {
                    snap.enableTurnAround = false;   // palanca hacia atras = giro brusco de 180 grados
                    EditorUtility.SetDirty(snap);
                }
                else if (mb is UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider move)
                {
                    move.moveSpeed = 1.8f;           // 2.5 m/s es trotar; ~1.4-1.8 es lo comodo en VR
                    EditorUtility.SetDirty(move);
                }
            }
        }

        /// <summary>Pone la mano del profesor dentro del control y oculta el modelo del control de Quest.</summary>
        static void AddProfessorHand(Transform controller, string prefabName)
        {
            if (controller == null) return;
            var prefab = FindPrefab(prefabName);
            if (prefab == null) { Debug.LogWarning("[RickMorty] Falta el prefab " + prefabName + " (Assets/Oculus Hands)."); return; }
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(prefab, controller);
            hand.name = prefabName;
            foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = ShadowCastingMode.Off;

            // Ocultamos solo los renderers del modelo del control (del objeto cuelgan otras piezas del rig)
            foreach (var t in controller.GetComponentsInChildren<Transform>(true))
                if (t.name.EndsWith("Controller Visual"))
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            // Si el prefab del profesor perdio las acciones (otra version de XRI), se las ponemos a mano
            var anim = hand.GetComponent<AnimateHandOnInput>();
            if (anim != null)
            {
                string map = controller.name.StartsWith("Left") ? "XRI Left Interaction" : "XRI Right Interaction";
                if (anim.pinchAnimationAction.reference == null) anim.pinchAnimationAction = new UnityEngine.InputSystem.InputActionProperty(FindActionRef(map, "Activate Value"));
                if (anim.gripAnimationAction.reference == null) anim.gripAnimationAction = new UnityEngine.InputSystem.InputActionProperty(FindActionRef(map, "Select Value"));
                if (anim.handAnimator == null) anim.handAnimator = hand.GetComponent<Animator>();
                Debug.Log("[RickMorty] Mano " + prefabName + ": pinza=" + Describe(anim.pinchAnimationAction) + " grip=" + Describe(anim.gripAnimationAction));
            }

            // Agarrar a distancia trae el objeto A LA MANO (antes quedaba flotando en la punta del rayo)
            foreach (var nf in controller.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true))
            {
                nf.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractorFarAttachMode.Near;
                nf.blockUIOnInteractableSelection = false;   // con algo en la mano igual se pueden tocar los botones
                EditorUtility.SetDirty(nf);
            }
        }

        static string Describe(UnityEngine.InputSystem.InputActionProperty p)
        {
            if (p.reference != null && p.reference.action != null) return p.reference.action.actionMap.name + "/" + p.reference.action.name;
            return "(sin accion)";
        }

        static UnityEngine.InputSystem.InputActionReference FindActionRef(string map, string action)
        {
            foreach (var guid in AssetDatabase.FindAssets("XRI Default Input Actions"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".inputactions")) continue;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is UnityEngine.InputSystem.InputActionReference r && r.action != null && r.action.actionMap.name == map && r.action.name == action)
                        return r;
            }
            Debug.LogWarning("[RickMorty] No se encontro la accion " + map + "/" + action);
            return null;
        }

        static GameObject FindPrefab(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        static Transform BuildOnboardingMarker(Vector3 pos)
        {
            var go = new GameObject("Marcador Onboarding");
            go.transform.position = pos;
            Prim(PrimitiveType.Cylinder, "Circulo", go.transform, Vector3.zero, new Vector3(0.9f, 0.005f, 0.9f), MatLib.Transparent(new Color(0.3f, 1f, 0.3f, 0.45f)), collider: false);
            Prim(PrimitiveType.Cylinder, "Centro", go.transform, new Vector3(0, 0.002f, 0), new Vector3(0.6f, 0.005f, 0.6f), MatLib.Lit(new Color(0.3f, 1f, 0.3f), 2f), collider: false);
            Label("CAMINA AQUÍ", go.transform, new Vector3(0, 0.9f, 0), Quaternion.Euler(0, -130, 0), 0.9f, new Color(0.5f, 1f, 0.5f), 1.5f);
            go.SetActive(false);
            return go.transform;
        }

        // ==================================================================
        //  Rick (placeholder) y holograma
        // ==================================================================

        static RickNPC BuildRick(out RickHologram holo)
        {
            var rickGo = new GameObject("Rick");
            rickGo.transform.position = new Vector3(1.45f, 0, 1.25f);
            rickGo.transform.rotation = Quaternion.LookRotation(new Vector3(-1, 0, -0.3f));
            var vis = new GameObject("Visual").transform;
            vis.SetParent(rickGo.transform, false);
            BuildRickBody(vis, false);
            var mouth = new GameObject("Boca").transform;
            mouth.SetParent(vis, false);
            mouth.localPosition = new Vector3(0, 1.62f, 0.12f);
            var rick = rickGo.AddComponent<RickNPC>();
            rick.mouth = mouth;
            rick.exitPoint = Marker("SalidaRick", new Vector3(3.3f, 0, 0.5f));
            Label("RICK", rickGo.transform, new Vector3(0, 2.15f, 0), Quaternion.identity, 0.6f, new Color(0.6f, 0.9f, 1f));

            var holoGo = new GameObject("Holograma de Rick");
            holoGo.transform.position = new Vector3(0, 0.92f, 2.45f);
            var hv = new GameObject("Visual").transform;
            hv.SetParent(holoGo.transform, false);
            hv.localScale = Vector3.one * 0.35f;
            BuildRickBody(hv, true);
            var beamGo = new GameObject("Haz");
            beamGo.transform.SetParent(holoGo.transform, false);
            beamGo.transform.localPosition = new Vector3(0, 1.0f, -0.25f);
            var beam = beamGo.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.spotAngle = 40f;
            beam.range = 3f;
            beam.color = new Color(0.4f, 0.9f, 1f);
            beam.shadows = LightShadows.None;
            beamGo.transform.rotation = Quaternion.LookRotation(Vector3.down);
            holo = holoGo.AddComponent<RickHologram>();
            holo.parts = hv.GetComponentsInChildren<Renderer>();
            holo.beam = beam;
            return rick;
        }

        static void BuildRickBody(Transform vis, bool hologram)
        {
            Material M(Color c) => hologram ? MatLib.Transparent(new Color(0.4f, 0.9f, 1f, 0.45f)) : MatLib.Lit(c);
            Prim(PrimitiveType.Capsule, "Piernas", vis, new Vector3(0, 0.45f, 0), new Vector3(0.3f, 0.45f, 0.25f), M(new Color(0.45f, 0.3f, 0.2f)), null, false);
            Prim(PrimitiveType.Capsule, "BataBlanca", vis, new Vector3(0, 1.1f, 0), new Vector3(0.45f, 0.45f, 0.32f), M(new Color(0.95f, 0.95f, 0.95f)), null, false);
            Prim(PrimitiveType.Cube, "CamisaCeleste", vis, new Vector3(0, 1.2f, 0.14f), new Vector3(0.16f, 0.35f, 0.04f), M(new Color(0.55f, 0.75f, 0.9f)), null, false);
            Prim(PrimitiveType.Sphere, "Cabeza", vis, new Vector3(0, 1.62f, 0), new Vector3(0.24f, 0.28f, 0.24f), M(new Color(0.95f, 0.85f, 0.75f)), null, false);
            Prim(PrimitiveType.Cube, "Uniceja", vis, new Vector3(0, 1.7f, 0.115f), new Vector3(0.14f, 0.02f, 0.02f), M(new Color(0.55f, 0.75f, 0.9f)), null, false);
            for (int i = 0; i < 7; i++)
            {
                float a = -70f + i * 23f;
                Prim(PrimitiveType.Cube, "Pelo", vis, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.12f, 1.72f, -0.05f - Mathf.Cos(a * Mathf.Deg2Rad) * 0.06f),
                    new Vector3(0.05f, 0.05f, 0.22f), M(new Color(0.6f, 0.82f, 0.95f)), Quaternion.Euler(-30f, a, 0), false);
            }
        }

        // ==================================================================
        //  Sala final (vacio espacial)
        // ==================================================================

        static EndScreen BuildEndRoom()
        {
            var root = new GameObject("Sala Final (espacio)");
            root.transform.position = new Vector3(0, -500f, 0); // bajo tierra: invisible desde el mundo
            var floor = Prim(PrimitiveType.Cylinder, "Plataforma", root.transform, new Vector3(0, -0.05f, 0), new Vector3(4f, 0.05f, 4f),
                MatLib.Transparent(new Color(0.3f, 1f, 0.4f, 0.15f)));
            floor.AddComponent<SurfaceType>().kind = SurfaceKind.Ground;
            var voidMat = MatLib.Unlit(new Color(0.01f, 0.01f, 0.03f));
            var sky = Prim(PrimitiveType.Sphere, "Vacio", root.transform, Vector3.zero, Vector3.one * 80f, voidMat, null, false);
            // Invertir normales para ver la esfera desde adentro
            var mf = sky.GetComponent<MeshFilter>();
            var mesh = Object.Instantiate(mf.sharedMesh);
            mesh.name = "EsferaInvertida";
            mesh.triangles = mesh.triangles.Reverse().ToArray();
            var meshPath = RickMortySetup.GenMatDir + "/EsferaInvertida.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            mf.sharedMesh = mesh;
            var spot = new GameObject("PuntoJugador").transform;
            spot.SetParent(root.transform, false);
            var end = root.AddComponent<EndScreen>();
            end.playerSpot = spot;
            var l = new GameObject("Luz").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = new Vector3(0, 4, 0);
            l.type = LightType.Point; l.range = 15f; l.intensity = 1.5f; l.color = new Color(0.6f, 1f, 0.7f);
            return end;
        }
    }
}
