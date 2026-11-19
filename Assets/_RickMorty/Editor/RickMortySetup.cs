using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace RickMorty.EditorTools
{
    /// <summary>
    /// Paso 1: configura el proyecto (samples de XRI, recursos de TextMeshPro, OpenXR para Quest,
    /// Player Settings de Android y assets base: materiales y registro de modelos).
    /// </summary>
    public static class RickMortySetup
    {
        public const string Root = "Assets/_RickMorty";
        public const string MatDir = Root + "/Materials";
        public const string GenMatDir = Root + "/Materials/Generated";

        [MenuItem("Rick y Morty/1. Configurar proyecto (XR, samples, TMP)", priority = 1)]
        public static void SetupProject()
        {
            ImportXRISamples();
            ImportTMPEssentials();
            ConfigureXR();
            ConfigurePlayer();
            CreateMaterialLibrary();
            CreateVisualRegistry();
            SetupFont();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RickMorty] Proyecto configurado. Ahora ejecuta 'Rick y Morty/2. Construir escena del garaje'.");
        }

        /// <summary>Entrada para modo batch: Unity -batchmode -executeMethod RickMorty.EditorTools.RickMortySetup.BatchSetup</summary>
        public static void BatchSetup()
        {
            try { SetupProject(); }
            catch (Exception e) { Debug.LogError("[RickMorty] Setup fallo: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Fuentes del juego (OFL, Google Fonts):
        ///  - Texto: Chakra Petch SemiBold (+ Bold real para &lt;b&gt;) — aire de aparato de laboratorio, legible en VR.
        ///  - Titulos: Rubik Wet Paint (verde que chorrea, tipo baba de portal). Va en Resources/Fonts &amp; Materials
        ///    para usarla desde codigo (UIKit.AsTitle) o con la etiqueta &lt;font="Titulos SDF"&gt;.
        /// Crea los Font Assets una vez y deja el de texto como fuente por defecto de TMP.
        /// </summary>
        [MenuItem("Rick y Morty/Aplicar fuente del juego", priority = 21)]
        public static void SetupFont()
        {
            var fa = LoadOrCreateFont(Root + "/Fonts/ChakraPetch-SemiBold.ttf", Root + "/Fonts/Chakra Petch SDF.asset", "Chakra Petch SDF");
            if (fa == null) return;
            if (!AssetDatabase.IsValidFolder(Root + "/Resources/Fonts & Materials"))
                AssetDatabase.CreateFolder(Root + "/Resources", "Fonts & Materials");
            var title = LoadOrCreateFont(Root + "/Fonts/RubikWetPaint-Regular.ttf", Root + "/Resources/Fonts & Materials/Titulos SDF.asset", "Titulos SDF");
            if (title != null && (title.fallbackFontAssetTable == null || !title.fallbackFontAssetTable.Contains(fa)))
            {
                title.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMPro.TMP_FontAsset>();
                title.fallbackFontAssetTable.Add(fa);
                EditorUtility.SetDirty(title);
            }
            // Rubik Wet Paint no tiene negrita: si un texto en negrita usaba la fuente de titulos, TMP caia a la
            // fuente de respaldo (Chakra Petch Bold) y el titulo perdia su estilo. Su "negrita" es ella misma.
            if (title != null && title.fontWeightTable != null && title.fontWeightTable.Length > 7 && title.fontWeightTable[7].regularTypeface != title)
            {
                title.fontWeightTable[7].regularTypeface = title;
                EditorUtility.SetDirty(title);
            }
            // Peso Bold real para <b> (sin esto TMP engorda la letra y se "come" el espacio anterior)
            var bold = LoadOrCreateFont(Root + "/Fonts/ChakraPetch-Bold.ttf", Root + "/Fonts/Chakra Petch Bold SDF.asset", "Chakra Petch Bold SDF");
            if (bold != null && fa.fontWeightTable != null && fa.fontWeightTable.Length > 7 && fa.fontWeightTable[7].regularTypeface != bold)
            {
                fa.fontWeightTable[7].regularTypeface = bold;
                EditorUtility.SetDirty(fa);
            }
            // Fallback a la fuente original de TMP para cualquier simbolo que Fredoka no tenga
            var liberation = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (liberation != null && (fa.fallbackFontAssetTable == null || !fa.fallbackFontAssetTable.Contains(liberation)))
            {
                fa.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMPro.TMP_FontAsset>();
                fa.fallbackFontAssetTable.Add(liberation);
                EditorUtility.SetDirty(fa);
            }
            var settings = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[RickMorty] Fuente por defecto: Chakra Petch SDF (bold: " + (bold != null ? bold.name : "sintetica") + ", titulos: " + (title != null ? title.name : "-") + ")");
        }

        static TMPro.TMP_FontAsset LoadOrCreateFont(string ttf, string assetPath, string name)
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(assetPath);
            if (fa != null) return fa;
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null) { Debug.LogWarning("[RickMorty] No esta la fuente " + ttf); return null; }
            fa = TMPro.TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                TMPro.AtlasPopulationMode.Dynamic, true);
            fa.name = name;
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            // caracteres que usa el juego (se pre-cargan para no generar el atlas en el visor)
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~¡¿áéíóúüñÁÉÍÓÚÜÑ·…–—“”‘’×°", out _);
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static void ImportXRISamples()
        {
            var samples = Sample.FindByPackage("com.unity.xr.interaction.toolkit", string.Empty).ToList();
            foreach (var name in new[] { "Starter Assets", "XR Interaction Simulator" })
            {
                var s = samples.FirstOrDefault(x => x.displayName == name);
                if (string.IsNullOrEmpty(s.displayName))
                {
                    Debug.LogWarning("[RickMorty] Sample no encontrado: " + name);
                    continue;
                }
                if (s.isImported) { Debug.Log("[RickMorty] Sample ya importado: " + name); continue; }
                bool ok = s.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
                Debug.Log($"[RickMorty] Importando sample {name}: {ok}");
            }
        }

        static void ImportTMPEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("[RickMorty] TMP Essential Resources importados.");
        }

        static void ConfigureXR()
        {
            try
            {
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    Directory.CreateDirectory("Assets/XR");
                    AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
                }

                foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
                {
                    if (!perTarget.HasSettingsForBuildTarget(group))
                    {
                        var gs = ScriptableObject.CreateInstance<XRGeneralSettings>();
                        gs.name = group + " Settings";
                        AssetDatabase.AddObjectToAsset(gs, perTarget);
                        perTarget.SetSettingsForBuildTarget(group, gs);
                    }
                    if (!perTarget.HasManagerSettingsForBuildTarget(group))
                        perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

                    var settings = perTarget.SettingsForBuildTarget(group);
                    settings.InitManagerOnStart = true;
                    XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
                    EditorUtility.SetDirty(settings);
                    EditorUtility.SetDirty(settings.Manager);

                    var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                    if (openxr != null)
                    {
                        EnableFeature<OculusTouchControllerProfile>(openxr);
                        EnableFeature<MetaQuestTouchPlusControllerProfile>(openxr);
                        if (group == BuildTargetGroup.Android) EnableFeature<MetaQuestFeature>(openxr);
                        openxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                        EditorUtility.SetDirty(openxr);
                    }
                }
                EditorUtility.SetDirty(perTarget);
                Debug.Log("[RickMorty] XR Plug-in Management: OpenXR + perfiles Oculus/Meta Quest configurados.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RickMorty] No se pudo configurar XR automaticamente (hazlo en Project Settings > XR Plug-in Management): " + e.Message);
            }
        }

        static void EnableFeature<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var f = settings.GetFeature<T>();
            if (f != null) { f.enabled = true; EditorUtility.SetDirty(f); }
            // Antes fallaba en silencio: el repo quedo con Meta Quest Support y los perfiles Touch APAGADOS
            // (el APK habria salido sin controles). Si pasa, hay que volver a correr el menu 1.
            else Debug.LogError($"[RickMorty] OpenXR: no se encontro la feature {typeof(T).Name}. Vuelve a ejecutar 'Rick y Morty/1. Configurar proyecto' y revisa Project Settings > XR Plug-in Management > OpenXR.");
        }

        static void ConfigurePlayer()
        {
            try
            {
                PlayerSettings.runInBackground = true;   // validacion de XR: sin esto la imagen queda pegada a la cabeza al abrir el menu del sistema
                PlayerSettings.companyName = "UAO Immersive Solutions";
                PlayerSettings.productName = "Rick y Morty - Mezclas de otra dimension";
                PlayerSettings.colorSpace = ColorSpace.Linear;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.uao.rickmortyvr");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RickMorty] Player Settings parciales: " + e.Message);
            }
        }

        static Material MakeBaseMaterial(string name, string shader, bool transparent, bool particles)
        {
            Directory.CreateDirectory(MatDir);
            var path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[RickMorty] Shader no encontrado: " + shader); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
            m = new Material(sh) { name = name };
            if (transparent) MatLib.MakeTransparent(m, particles);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static void CreateMaterialLibrary()
        {
            Directory.CreateDirectory(Root + "/Resources");
            var path = Root + "/Resources/MaterialLibrary.asset";
            var lib = AssetDatabase.LoadAssetAtPath<MaterialLibrary>(path);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<MaterialLibrary>();
                AssetDatabase.CreateAsset(lib, path);
            }
            lib.lit = MakeBaseMaterial("RM_Lit", "Universal Render Pipeline/Lit", false, false);
            lib.unlit = MakeBaseMaterial("RM_Unlit", "Universal Render Pipeline/Unlit", false, false);
            lib.transparent = MakeBaseMaterial("RM_Transparent", "Universal Render Pipeline/Unlit", true, false);
            lib.particles = MakeBaseMaterial("RM_Particles", "Universal Render Pipeline/Particles/Unlit", true, true);
            EditorUtility.SetDirty(lib);
        }

        /// <summary>
        /// Llena el registro id → modelo. Los GLB optimizados viven en Models/Items/&lt;id&gt;.glb
        /// (los genera _Tools/blender). Un modelo asignado a mano no se sobrescribe.
        /// </summary>
        [MenuItem("Rick y Morty/Actualizar registro de modelos", priority = 20)]
        public static void CreateVisualRegistry()
        {
            var path = Root + "/Resources/ItemVisualRegistry.asset";
            var reg = AssetDatabase.LoadAssetAtPath<ItemVisualRegistry>(path);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ItemVisualRegistry>();
                AssetDatabase.CreateAsset(reg, path);
            }
            void Link(string id, string modelPath, float scale = 1f)
            {
                var e = reg.entries.FirstOrDefault(x => x.id == id);
                if (e == null) { e = new ItemVisualRegistry.Entry { id = id }; reg.entries.Add(e); }
                if (e.prefab != null) return;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (prefab == null) return;
                e.prefab = prefab;
                e.scale = scale;
                e.generateBoxCollider = true;
            }
            // entradas huerfanas (ids viejos que ya no existen, p. ej. uno que quedo con tilde): fuera
            reg.entries.RemoveAll(e => e == null || (e.prefab == null && GameDatabase.GetItem(e.id) == null));
            foreach (var def in GameDatabase.Items)
                Link(def.id, $"{Root}/Models/Items/{def.id}.glb");
            // Modelos usados por codigo que no son items
            Link("meeseeks_npc", $"{Root}/Models/Env/meeseeks_npc.glb");
            Link("rick_caleno", $"{Root}/Models/Env/rick.glb", 0.15f / 1.75f);
            Link("hombre_eyeholes", $"{Root}/Models/Env/hombre_eyeholes.glb");
            foreach (var id in new[] { "kalaxianos", "esporas", "plumbus_polvo", "marihuana" })
                Link("relleno_" + id, $"{Root}/Models/Env/relleno_{id}.glb");
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RickMorty] Registro de modelos: {reg.entries.Count(e => e.prefab != null)}/{reg.entries.Count} con modelo.");
        }

        /// <summary>Durante la construccion de la escena, guarda cada material generado como asset.</summary>
        public static void BeginPersistMaterials()
        {
            Directory.CreateDirectory(GenMatDir);
            MatLib.ClearCache();
            MatLib.PersistHook = (m, key) =>
            {
                var safe = new string(key.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                var p = GenMatDir + "/" + safe + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (existing != null)
                {
                    existing.CopyPropertiesFromMaterial(m);
                    existing.shaderKeywords = m.shaderKeywords;
                    existing.renderQueue = m.renderQueue;
                    EditorUtility.SetDirty(existing);
                    return existing;
                }
                m.name = safe;
                AssetDatabase.CreateAsset(m, p);
                return m;
            };
        }

        public static void EndPersistMaterials()
        {
            MatLib.PersistHook = null;
            MatLib.ClearCache();
            AssetDatabase.SaveAssets();
        }
    }
}
