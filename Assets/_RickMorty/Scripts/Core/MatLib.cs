using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RickMorty
{
    /// <summary>
    /// Fabrica y cache de materiales URP por color (Lit, Unlit, transparente, particulas) para no crear uno nuevo cada vez.
    /// Lo usan: FX, ItemFactory, efectos de objetos y NPC, la UI 3D y el builder de escena.
    /// </summary>
    public static class MatLib
    {
        static readonly Dictionary<string, Material> s_Cache = new Dictionary<string, Material>();
        static Texture2D s_SoftDot;

        /// <summary>Solo editor: permite al constructor de escena guardar los materiales como assets.</summary>
        public static System.Func<Material, string, Material> PersistHook;

        static Material Store(string key, Material m)
        {
            if (PersistHook != null) m = PersistHook(m, key);
            s_Cache[key] = m;
            return m;
        }

        public static void ClearCache() => s_Cache.Clear();

        static Material Base(int kind)
        {
            var lib = MaterialLibrary.Get();
            Material m = null;
            if (lib != null)
            {
                m = kind == 0 ? lib.lit : kind == 1 ? lib.unlit : kind == 2 ? lib.transparent : lib.particles;
            }
            if (m != null) return m;

            // Fallback (solo si no se corrio el builder)
            var shaderName = kind == 0 ? "Universal Render Pipeline/Lit"
                : kind == 3 ? "Universal Render Pipeline/Particles/Unlit"
                : "Universal Render Pipeline/Unlit";
            var sh = Shader.Find(shaderName);
            if (sh == null) sh = Shader.Find("Sprites/Default");
            m = new Material(sh);
            if (kind >= 2) MakeTransparent(m, kind == 3);
            return m;
        }

        static string K(string kind, Color c, float a = 0, float b = 0)
        {
            return kind + ColorUtility.ToHtmlStringRGBA(c) + a.ToString("0.00") + b.ToString("0.00");
        }

        public static Material Lit(Color c, float emission = 0f, float smoothness = 0.35f, float metallic = 0f)
        {
            var key = K("L", c, emission, smoothness + metallic * 10f);
            if (s_Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Base(0));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return Store(key, m);
        }

        public static Material Unlit(Color c)
        {
            var key = K("U", c);
            if (s_Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Base(1));
            m.SetColor("_BaseColor", c);
            return Store(key, m);
        }

        /// <summary>Material transparente sin cache (para poder animar su alpha).</summary>
        public static Material TransparentInstance(Color c)
        {
            var m = new Material(Base(2));
            m.SetColor("_BaseColor", c);
            return m;
        }

        public static Material Transparent(Color c)
        {
            var key = K("T", c);
            if (s_Cache.TryGetValue(key, out var m) && m != null) return m;
            m = TransparentInstance(c);
            return Store(key, m);
        }

        public static Material Particles(Color c, bool additive = false)
        {
            var key = K(additive ? "PA" : "P", c);
            if (s_Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Base(3));
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", SoftDot);
            if (additive)
            {
                m.SetFloat("_Blend", 2f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.One);
            }
            return Store(key, m);
        }

        public static Texture2D SoftDot
        {
            get
            {
                if (s_SoftDot != null) return s_SoftDot;
                const int n = 32;
                s_SoftDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    s_SoftDot.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
                s_SoftDot.Apply();
                return s_SoftDot;
            }
        }

        /// <summary>Configura un material URP (Lit/Unlit/Particles) como transparente alpha-blend.</summary>
        public static void MakeTransparent(Material m, bool particles = false)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            m.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        static MaterialPropertyBlock s_Mpb;
        static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Cambia el color sin crear materiales. Reusa un unico MaterialPropertyBlock
        /// (se llama cada cuadro desde varios efectos: crear uno nuevo generaba basura y tirones en Quest).</summary>
        public static void SetColor(Renderer r, Color c)
        {
            if (r == null) return;
            if (s_Mpb == null) s_Mpb = new MaterialPropertyBlock();
            s_Mpb.Clear();
            r.GetPropertyBlock(s_Mpb);
            s_Mpb.SetColor(s_BaseColorId, c);
            r.SetPropertyBlock(s_Mpb);
        }
    }
}
