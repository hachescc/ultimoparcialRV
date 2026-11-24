using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Texturas generadas por codigo (una sola vez, se reutilizan). Evitan depender de archivos de imagen
    /// para efectos simples: el remolino del portal, el halo del tazon y los planos tecnicos.
    /// Lo usan: FX.Portal (remolino) y el builder de escena (halo y planos, guardados como PNG).
    /// </summary>
    public static class ProcTex
    {
        static Texture2D s_Swirl;
        static Material s_PortalMat, s_PortalCoreMat;

        /// <summary>Remolino verde del portal (estilo Rick y Morty): espiral con borde irregular y centro claro.</summary>
        public static Texture2D PortalSwirl
        {
            get
            {
                if (s_Swirl != null) return s_Swirl;
                const int n = 256;
                s_Swirl = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "PortalSwirl" };
                var px = new Color32[n * n];
                var dark = new Color(0.02f, 0.22f, 0.03f);
                var mid = new Color(0.22f, 0.92f, 0.14f);
                var hot = new Color(0.86f, 1f, 0.42f);
                var rnd = new System.Random(11);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Atan2(v, u);
                    // dos brazos de espiral gruesos + una capa fina que gira al reves
                    float s1 = 0.5f + 0.5f * Mathf.Sin(a * 2f + r * 9.5f);
                    float s2 = 0.5f + 0.5f * Mathf.Sin(a * 5f - r * 17f + 1.3f);
                    float s3 = 0.5f + 0.5f * Mathf.Sin(a * 9f + r * 31f);
                    float val = Mathf.Clamp01(s1 * 0.62f + s2 * 0.28f + s3 * 0.10f);
                    val = val * val * (3f - 2f * val);
                    float core = Mathf.Pow(Mathf.Clamp01(1f - r * 1.35f), 1.6f);
                    Color c = Color.Lerp(dark, mid, val);
                    c = Color.Lerp(c, hot, Mathf.Clamp01(Mathf.Pow(val, 4f) * 0.75f + core * 0.9f));
                    // borde ondulado, como salpicado
                    float edge = r + 0.055f * Mathf.Sin(a * 7f) + 0.035f * Mathf.Sin(a * 13f + 1.7f) + 0.02f * Mathf.Sin(a * 23f + 0.4f);
                    float alpha = 1f - Mathf.SmoothStep(0.80f, 0.93f, edge);
                    // borde mas claro (aro de energia)
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(edge - 0.80f) / 0.07f);
                    c = Color.Lerp(c, hot, rim * 0.55f);
                    c.a = alpha;
                    px[y * n + x] = c;
                }
                // motas claras
                for (int i = 0; i < 90; i++)
                {
                    float rr = Mathf.Sqrt((float)rnd.NextDouble()) * 0.74f, aa = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    int cx = (int)((Mathf.Cos(aa) * rr * 0.5f + 0.5f) * n), cy = (int)((Mathf.Sin(aa) * rr * 0.5f + 0.5f) * n);
                    int rad = 1 + rnd.Next(3);
                    for (int dy = -rad; dy <= rad; dy++)
                    for (int dx = -rad; dx <= rad; dx++)
                    {
                        int xx = cx + dx, yy = cy + dy;
                        if (xx < 0 || yy < 0 || xx >= n || yy >= n || dx * dx + dy * dy > rad * rad) continue;
                        var p = (Color)px[yy * n + xx];
                        var q = Color.Lerp(p, hot, 0.8f); q.a = p.a;
                        px[yy * n + xx] = q;
                    }
                }
                s_Swirl.SetPixels32(px);
                s_Swirl.Apply(true, true);
                return s_Swirl;
            }
        }

        /// <summary>Resplandor radial suave (para el brillo del tazon: un halo, no un disco de color plano).</summary>
        public static Texture2D BuildSoftGlow(int n = 128)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SoftGlow" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                px[y * n + x] = new Color(1, 1, 1, a * a * (3f - 2f * a));
            }
            t.SetPixels32(px); t.Apply(false, false);
            return t;
        }

        static void Line(Color32[] px, int w, int h, float x0, float y0, float x1, float y1, Color32 c, int thick = 1)
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0))) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                for (int dy = 0; dy < thick; dy++)
                for (int dx = 0; dx < thick; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && yy >= 0 && xx < w && yy < h) px[yy * w + xx] = c;
                }
            }
        }

        static void Poly(Color32[] px, int w, int h, Color32 c, int thick, params float[] pts)
        {
            for (int i = 0; i + 3 < pts.Length; i += 2) Line(px, w, h, pts[i], pts[i + 1], pts[i + 2], pts[i + 3], c, thick);
        }

        static void Circle(Color32[] px, int w, int h, float cx, float cy, float r, Color32 c, int thick = 1)
        {
            const int seg = 40;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Line(px, w, h, cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, c, thick);
            }
        }

        /// <summary>Plano tecnico azul (cuadricula + silueta de la pistola de portales + cotas). variant cambia el dibujo.</summary>
        public static Texture2D BuildBlueprint(int variant = 0)
        {
            const int w = 384, h = 288;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Plano" + variant };
            var px = new Color32[w * h];
            var rnd = new System.Random(31 + variant * 7);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = (float)rnd.NextDouble() * 0.03f;
                var c = new Color(0.07f + n, 0.22f + n, 0.52f + n, 1f);
                if (x % 24 == 0 || y % 24 == 0) c = Color.Lerp(c, new Color(0.55f, 0.75f, 1f), 0.22f);
                if (x % 96 == 0 || y % 96 == 0) c = Color.Lerp(c, new Color(0.55f, 0.75f, 1f), 0.25f);
                px[y * w + x] = c;
            }
            var ink = new Color32(225, 240, 255, 255);
            var soft = new Color32(150, 195, 245, 255);
            // marco y cajetin
            Poly(px, w, h, ink, 2, 10, 10, w - 12, 10, w - 12, h - 12, 10, h - 12, 10, 10);
            Poly(px, w, h, ink, 1, w - 130, 10, w - 130, 52, w - 12, 52);
            for (int i = 0; i < 3; i++) Line(px, w, h, w - 122, 20 + i * 11, w - 30 - rnd.Next(40), 20 + i * 11, soft, 2);
            if (variant % 2 == 0)
            {
                // pistola de portales de perfil: cuerpo, empunadura, cupula y punta
                Poly(px, w, h, ink, 2, 70, 150, 250, 150, 275, 165, 275, 195, 250, 210, 70, 210, 70, 150);
                Poly(px, w, h, ink, 2, 95, 150, 80, 80, 125, 72, 140, 150);
                Circle(px, w, h, 200, 222, 26, ink, 2);
                Poly(px, w, h, ink, 2, 275, 172, 315, 172, 315, 188, 275, 188);
                Circle(px, w, h, 110, 180, 9, soft, 2);
                Circle(px, w, h, 140, 180, 9, soft, 2);
                // cotas
                Poly(px, w, h, soft, 1, 70, 232, 70, 250); Poly(px, w, h, soft, 1, 315, 232, 315, 250);
                Line(px, w, h, 70, 242, 315, 242, soft, 1);
                Line(px, w, h, 330, 150, 330, 210, soft, 1);
            }
            else
            {
                // esquema de un portal: circulos concentricos, ejes y una espiral
                Circle(px, w, h, 150, 150, 86, ink, 2);
                Circle(px, w, h, 150, 150, 62, soft, 1);
                Circle(px, w, h, 150, 150, 30, soft, 1);
                Line(px, w, h, 40, 150, 260, 150, soft, 1); Line(px, w, h, 150, 44, 150, 256, soft, 1);
                float pr = 4f, pa = 0f, lx = 150, ly = 150;
                for (int i = 0; i < 120; i++)
                {
                    pa += 0.21f; pr += 0.66f;
                    float nx = 150 + Mathf.Cos(pa) * pr, ny = 150 + Mathf.Sin(pa) * pr;
                    Line(px, w, h, lx, ly, nx, ny, ink, 2); lx = nx; ly = ny;
                }
                for (int i = 0; i < 6; i++) Line(px, w, h, 270, 230 - i * 16, 300 + rnd.Next(55), 230 - i * 16, soft, 2);
            }
            // "texto" manuscrito arriba
            for (int i = 0; i < 4; i++) Line(px, w, h, 26 + i * 62, h - 34, 26 + i * 62 + 30 + rnd.Next(22), h - 34, ink, 3);
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        /// <summary>Material compartido del disco del portal (sin luz, transparente, dos caras).</summary>
        public static Material PortalMaterial
        {
            get
            {
                if (s_PortalMat != null) return s_PortalMat;
                s_PortalMat = MatLib.TransparentInstance(Color.white);
                s_PortalMat.name = "Portal (remolino)";
                s_PortalMat.SetTexture("_BaseMap", PortalSwirl);
                s_PortalMat.mainTexture = PortalSwirl;
                return s_PortalMat;
            }
        }

        /// <summary>Capa interior del portal: mismo remolino, mas transparente (gira al reves).</summary>
        public static Material PortalCoreMaterial
        {
            get
            {
                if (s_PortalCoreMat != null) return s_PortalCoreMat;
                s_PortalCoreMat = MatLib.TransparentInstance(new Color(1f, 1f, 0.85f, 0.55f));
                s_PortalCoreMat.name = "Portal (centro)";
                s_PortalCoreMat.SetTexture("_BaseMap", PortalSwirl);
                s_PortalCoreMat.mainTexture = PortalSwirl;
                s_PortalCoreMat.renderQueue += 1;
                return s_PortalCoreMat;
            }
        }
    }
}
