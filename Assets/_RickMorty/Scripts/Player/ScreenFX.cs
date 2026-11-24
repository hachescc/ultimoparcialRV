using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RickMorty
{
    /// <summary>
    /// Efectos de pantalla para VR: fundidos, destellos (flashbang), ceguera rosa del Tio Steve,
    /// filtro VHS, borrachera y sonido amortiguado (tinnitus / low pass).
    /// Va en la camara del XR Origin.
    /// Lo usan: GameManager, tazon (explosion), Tio Steve, objetos (consumibles, guantelete, casco), soporte final, PlayAreaBoundary y la pausa.
    /// </summary>
    public class ScreenFX : MonoBehaviour
    {
        public static ScreenFX Instance { get; private set; }

        Renderer m_Fade;
        Material m_FadeMat;
        Renderer m_Overlay;
        Material m_OverlayMat;
        TextMeshPro m_OverlayText;
        Volume m_Volume;
        ChromaticAberration m_Chroma;
        ColorAdjustments m_Color;
        AudioLowPassFilter m_LowPass;
        Coroutine m_FadeCo, m_VolumeCo, m_AudioCo, m_OverlayCo;

        void Awake()
        {
            Instance = this;
            BuildQuads();
            BuildVolume();
            m_LowPass = gameObject.GetComponent<AudioLowPassFilter>();
            if (m_LowPass == null) m_LowPass = gameObject.AddComponent<AudioLowPassFilter>();
            m_LowPass.cutoffFrequency = 22000f;
        }

        void BuildQuads()
        {
            var fade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(fade.GetComponent<Collider>());
            fade.name = "ScreenFade";
            fade.transform.SetParent(transform, false);
            fade.transform.localPosition = new Vector3(0, 0, 0.12f);
            fade.transform.localScale = new Vector3(1f, 1f, 1f);
            m_Fade = fade.GetComponent<Renderer>();
            m_FadeMat = MatLib.TransparentInstance(new Color(0, 0, 0, 0));
            m_FadeMat.renderQueue = 4000;
            m_Fade.sharedMaterial = m_FadeMat;
            m_Fade.sortingOrder = 32000;   // por encima de los canvas en espacio mundo
            m_Fade.enabled = false;

            var ov = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ov.GetComponent<Collider>());
            ov.name = "ScreenOverlay";
            ov.transform.SetParent(transform, false);
            ov.transform.localPosition = new Vector3(0, 0, 0.13f);
            ov.transform.localScale = new Vector3(0.5f, 0.35f, 1f);
            m_Overlay = ov.GetComponent<Renderer>();
            m_OverlayMat = MatLib.TransparentInstance(new Color(1, 1, 1, 0));
            m_OverlayMat.renderQueue = 3990;
            m_Overlay.sharedMaterial = m_OverlayMat;
            m_Overlay.sortingOrder = 31990;
            m_Overlay.enabled = false;

            var txtGo = new GameObject("OverlayText");
            txtGo.transform.SetParent(transform, false);
            // A 1 m (antes 40 cm y ~60 grados de ancho: las puntas del texto quedaban en la zona borrosa de la lente)
            txtGo.transform.localPosition = new Vector3(0, 0.03f, 1.0f);
            m_OverlayText = txtGo.AddComponent<TextMeshPro>();
            m_OverlayText.fontSize = 0.44f;
            m_OverlayText.textWrappingMode = TextWrappingModes.Normal;
            m_OverlayText.outlineWidth = 0.18f;
            m_OverlayText.outlineColor = new Color32(40, 0, 30, 255);
            m_OverlayText.alignment = TextAlignmentOptions.Center;
            m_OverlayText.rectTransform.sizeDelta = new Vector2(0.9f, 0.45f);
            SetOverlayText("");
            m_OverlayText.color = Color.white;
            m_OverlayText.fontStyle = FontStyles.Bold;
            m_OverlayText.renderer.sharedMaterial.renderQueue = 4001;
            m_OverlayText.sortingOrder = 32010;

            // placa oscura detras del texto: blanco sobre el rosado del abrazo o el rojo del francotirador no tenia contraste
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bg.GetComponent<Collider>());
            bg.name = "OverlayTextBg";
            bg.layer = txtGo.layer;
            bg.transform.SetParent(txtGo.transform, false);
            bg.transform.localPosition = new Vector3(0, 0, 0.01f);
            bg.transform.localScale = new Vector3(0.98f, 0.34f, 1f);
            m_OverlayTextBg = bg.GetComponent<Renderer>();
            var bgMat = MatLib.TransparentInstance(new Color(0.02f, 0.02f, 0.03f, 0.62f));
            bgMat.renderQueue = 4000;
            m_OverlayTextBg.sharedMaterial = bgMat;
            m_OverlayTextBg.sortingOrder = 32005;
            m_OverlayTextBg.enabled = false;
        }

        Renderer m_OverlayTextBg;

        void SetOverlayText(string text)
        {
            if (m_OverlayText != null) m_OverlayText.text = text;
            if (m_OverlayTextBg != null) m_OverlayTextBg.enabled = !string.IsNullOrEmpty(text);
        }

        void BuildVolume()
        {
            var go = new GameObject("FX Volume");
            go.transform.SetParent(transform, false);
            m_Volume = go.AddComponent<Volume>();
            m_Volume.isGlobal = true;
            m_Volume.priority = 10;
            m_Volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            // Solo color y aberracion cromatica: deformar la lente o poner grano marea y es caro en el visor
            m_Chroma = profile.Add<ChromaticAberration>(true);
            m_Color = profile.Add<ColorAdjustments>(true);
            m_Chroma.intensity.Override(0f);
            m_Color.saturation.Override(0f);
            m_Color.colorFilter.Override(Color.white);
            m_Volume.sharedProfile = profile;
        }

        // ---------------- Fundidos ----------------

        public Coroutine Fade(Color color, float from, float to, float duration)
        {
            if (m_FadeCo != null) StopCoroutine(m_FadeCo);
            m_FadeCo = StartCoroutine(CoFade(color, from, to, duration));
            return m_FadeCo;
        }

        public void SetFade(Color color, float alpha)
        {
            if (m_FadeCo != null) { StopCoroutine(m_FadeCo); m_FadeCo = null; }
            color.a = alpha;
            m_FadeMat.SetColor("_BaseColor", color);
            m_Fade.enabled = alpha > 0.001f;
        }

        IEnumerator CoFade(Color color, float from, float to, float duration)
        {
            m_Fade.enabled = true;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(from, to, duration > 0 ? t / duration : 1f);
                m_FadeMat.SetColor("_BaseColor", color);
                yield return null;
            }
            color.a = to;
            m_FadeMat.SetColor("_BaseColor", color);
            m_Fade.enabled = to > 0.001f;
            m_FadeCo = null;
        }

        /// <summary>Al abrir la pausa: quita fundidos, destellos y textos (corren con tiempo de juego y quedarian congelados encima del menu).</summary>
        public void ClearForPause()
        {
            SetFade(Color.black, 0f);
            ClearOverlay();
        }

        /// <summary>Destello: sube instantaneo, se mantiene y se desvanece.</summary>
        public void Flash(Color color, float hold = 0.1f, float fadeOut = 1.2f)
        {
            if (m_FadeCo != null) StopCoroutine(m_FadeCo);
            m_FadeCo = StartCoroutine(CoFlash(color, hold, fadeOut));
        }

        IEnumerator CoFlash(Color color, float hold, float fadeOut)
        {
            // alpha del color = intensidad maxima del destello (1 si no se indica)
            float peak = color.a > 0.01f ? color.a : 1f;
            color.a = peak;
            m_FadeMat.SetColor("_BaseColor", color);
            m_Fade.enabled = true;
            yield return new WaitForSeconds(hold);
            m_FadeCo = null;
            yield return Fade(color, peak, 0f, fadeOut);
        }

        // ---------------- Overlays ----------------

        /// <summary>Ceguera de "falsos recuerdos hermosos" del Tio Steve.</summary>
        public void Blind(float seconds)
        {
            ClearOverlay();
            m_OverlayCo = StartCoroutine(CoBlind(seconds));
        }

        List<Transform> m_Hearts;
        Renderer[] m_HeartRenderers;

        /// <summary>Corta cualquier overlay en curso y deja todo limpio (si no, los corazones o el rosado
        /// quedaban pegados al interrumpir el abrazo con otro aviso, p. ej. el francotirador).</summary>
        void ClearOverlay()
        {
            if (m_OverlayCo != null) StopCoroutine(m_OverlayCo);
            m_OverlayCo = null;
            if (m_Hearts != null) foreach (var h in m_Hearts) if (h != null) Destroy(h.gameObject);
            m_Hearts = null;
            m_HeartRenderers = null;
            if (m_Overlay != null) m_Overlay.enabled = false;
            if (m_OverlayText != null) SetOverlayText("");
        }

        IEnumerator CoBlind(float seconds)
        {
            m_Overlay.enabled = true;
            var hearts = m_Hearts = SpawnHearts(14);
            m_HeartRenderers = new Renderer[hearts.Count];
            for (int i = 0; i < hearts.Count; i++) m_HeartRenderers[i] = hearts[i].GetComponent<Renderer>();
            SetOverlayText(UIKit.TitleTag("EL TÍO STEVE TE ABRAZÓ") + "\n<size=55%>Recuerdos falsos de un tío que nunca existió...</size>");
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((seconds - t) / 0.8f);
                float pulse = 0.9f + 0.1f * Mathf.Sin(t * 6f);
                m_OverlayMat.SetColor("_BaseColor", new Color(1f, 0.5f, 0.75f, a * 0.78f * pulse));
                m_OverlayText.color = new Color(1, 1, 1, a);
                for (int i = 0; i < hearts.Count; i++)
                {
                    var h = hearts[i];
                    float ph = t * 0.6f + i * 0.37f;
                    h.localPosition = new Vector3(Mathf.Sin(i * 2.1f) * 0.32f + Mathf.Sin(t * 1.3f + i) * 0.02f, Mathf.Repeat(ph, 1f) * 0.5f - 0.25f, 0.45f + (i % 3) * 0.05f);
                    h.localScale = Vector3.one * (0.05f + 0.025f * (i % 3)) * (0.9f + 0.15f * Mathf.Sin(t * 5f + i));
                    MatLib.SetColor(m_HeartRenderers[i], new Color(1f, 0.25f + 0.1f * (i % 3), 0.55f, a));
                }
                yield return null;
            }
            m_OverlayCo = null;
            ClearOverlay();
        }

        static Texture2D s_Heart;
        static Material s_HeartMat;

        List<Transform> SpawnHearts(int n)
        {
            if (s_Heart == null)
            {
                const int size = 64;
                s_Heart = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2.6f - 1.3f, v = (y + 0.5f) / size * 2.6f - 1.15f;
                    float f = Mathf.Pow(u * u + v * v - 1f, 3f) - u * u * v * v * v;   // ecuacion del corazon
                    s_Heart.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(-f * 40f)));
                }
                s_Heart.Apply();
            }
            var list = new List<Transform>();
            for (int i = 0; i < n; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Corazon";
                q.layer = m_Overlay.gameObject.layer;
                q.transform.SetParent(transform, false);
                var r = q.GetComponent<Renderer>();
                // un solo material para todos los corazones (el color/alpha va por MaterialPropertyBlock);
                // antes se creaban 14 materiales por abrazo y nunca se destruian
                if (s_HeartMat == null)
                {
                    s_HeartMat = MatLib.TransparentInstance(Color.white);
                    s_HeartMat.SetTexture("_BaseMap", s_Heart);
                    s_HeartMat.renderQueue = 3995;
                }
                r.sharedMaterial = s_HeartMat;
                r.sortingOrder = 31995;
                list.Add(q.transform);
            }
            return list;
        }

        public void ShowCenterText(string text, Color color, float seconds)
        {
            ClearOverlay();
            m_OverlayCo = StartCoroutine(CoCenterText(text, color, seconds));
        }

        IEnumerator CoCenterText(string text, Color color, float seconds)
        {
            SetOverlayText(text);
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime;
                color.a = Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((seconds - t) / 0.5f);
                m_OverlayText.color = color;
                yield return null;
            }
            SetOverlayText("");
            m_OverlayCo = null;
        }

        // ---------------- Post-proceso ----------------

        public void VHS(float seconds) => RunVolume(seconds, 0);
        public void Drunk(float seconds) => RunVolume(seconds, 1);
        public void PurpleHaze(float seconds) => RunVolume(seconds, 2);

        void RunVolume(float seconds, int mode)
        {
            if (m_VolumeCo != null) StopCoroutine(m_VolumeCo);
            m_VolumeCo = StartCoroutine(CoVolume(seconds, mode));
        }

        /// <summary>El post-procesado cuesta una pasada extra por cuadro en Quest: solo se enciende durante los efectos.</summary>
        static void SetPostProcessing(bool on)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = on;
        }

        IEnumerator CoVolume(float seconds, int mode)
        {
            SetPostProcessing(true);
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float w = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((seconds - t) / 1f);
                m_Volume.weight = w;
                switch (mode)
                {
                    case 0: // VHS
                        m_Color.saturation.Override(60f);
                        m_Color.colorFilter.Override(new Color(1f, 0.92f, 0.8f));
                        m_Chroma.intensity.Override(0.8f);
                        break;
                    case 1: // borrachera
                        // borrachera por COLOR (vaiven de tono y saturacion). Antes ondulaba la lente ±0.35 durante 12 s:
                        // deformar la imagen del visor es de lo que mas marea en VR.
                        m_Chroma.intensity.Override(0.5f + 0.3f * Mathf.Sin(t * 2f));
                        m_Color.saturation.Override(25f + 35f * Mathf.Sin(t * 1.1f));
                        m_Color.colorFilter.Override(Color.Lerp(new Color(1f, 0.9f, 0.7f), new Color(0.8f, 1f, 0.85f), 0.5f + 0.5f * Mathf.Sin(t * 0.9f)));
                        break;
                    default: // humo morado
                        m_Color.colorFilter.Override(new Color(0.85f, 0.6f, 1f));
                        m_Color.saturation.Override(30f);
                        m_Chroma.intensity.Override(0.3f);
                        break;
                }
                yield return null;
            }
            m_Volume.weight = 0f;
            m_VolumeCo = null;
            SetPostProcessing(false);
        }

        // ---------------- Audio ----------------

        /// <summary>Todo suena "bajo el agua" durante unos segundos (despues de una explosion).</summary>
        public void Muffle(float seconds, float cutoff = 700f)
        {
            if (m_AudioCo != null) StopCoroutine(m_AudioCo);
            m_AudioCo = StartCoroutine(CoMuffle(seconds, cutoff));
        }

        public void SetMuffled(bool muffled, float cutoff = 1200f)
        {
            if (m_AudioCo != null) { StopCoroutine(m_AudioCo); m_AudioCo = null; }
            m_LowPass.cutoffFrequency = muffled ? cutoff : 22000f;
        }

        IEnumerator CoMuffle(float seconds, float cutoff)
        {
            m_LowPass.cutoffFrequency = cutoff;
            yield return new WaitForSeconds(seconds * 0.7f);
            float t = 0, dur = seconds * 0.3f;
            while (t < dur)
            {
                t += Time.deltaTime;
                m_LowPass.cutoffFrequency = Mathf.Lerp(cutoff, 22000f, t / dur);
                yield return null;
            }
            m_LowPass.cutoffFrequency = 22000f;
            m_AudioCo = null;
        }
    }
}
