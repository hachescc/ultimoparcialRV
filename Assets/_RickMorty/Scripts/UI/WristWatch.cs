using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    /// <summary>
    /// Reloj Multiversal (UI diegetica en la muneca izquierda): temporizador del FBI, puntaje,
    /// descubrimientos, subtitulos de Rick y notificaciones de neon. Al mirar la muneca
    /// se proyecta el Holograma de Descubrimientos.
    /// </summary>
    public class WristWatch : MonoBehaviour
    {
        public static WristWatch Instance { get; private set; }

        public Transform head;
        public DiscoveryHologram hologram;
        [Tooltip("Coseno del angulo maximo entre la cara del reloj y la cabeza para considerarlo 'mirado'")]
        public float lookDot = 0.55f;
        public float lookMaxDistance = 0.75f;

        TextMeshProUGUI m_Timer, m_Score, m_Subtitle, m_Speaker, m_Footer, m_Objective;
        Image m_Bg, m_Screen;
        Canvas m_Banner;
        TextMeshProUGUI m_BannerText;
        Image m_BannerBg;
        Coroutine m_BannerCo;
        float m_LookTime;

        public bool IsLooking { get; private set; }
        public float LookingDuration => m_LookTime;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            // Cuerpo fisico del reloj (placeholder)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.name = "WatchBody";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.07f, 0.012f, 0.06f);
            body.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.25f, 0.27f, 0.25f), 0f, 0.6f, 0.6f);
            var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(tape.GetComponent<Collider>());
            tape.name = "CintaAmericana";
            tape.transform.SetParent(transform, false);
            tape.transform.localPosition = new Vector3(0, -0.006f, 0);
            tape.transform.localScale = new Vector3(0.085f, 0.008f, 0.03f);
            tape.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.6f, 0.6f, 0.62f), 0f, 0.2f);

            // Pantalla del reloj: filas fijas, texto con auto-tamano y "..." si no cabe (nunca se superpone)
            body.transform.localScale = new Vector3(0.088f, 0.012f, 0.074f);
            var canvas = UIKit.WorldCanvas("WatchCanvas", transform, new Vector2(400, 330), 0.0002f, false);
            canvas.transform.localPosition = new Vector3(0, 0.0065f, 0);
            canvas.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var root = canvas.transform;

            m_Bg = UIKit.Fill("Bg", root, new Color(0.10f, 0.12f, 0.11f, 1f));
            m_Screen = UIKit.Panel("Screen", root, new Color(0.02f, 0.07f, 0.04f, 1f), Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            var s = m_Screen.transform;

            m_Timer = UIKit.Line("Timer", s, "--:--", 46, UIKit.Neon, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.80f), new Vector2(0.62f, 1f), 14, 4);
            m_Timer.fontStyle = FontStyles.Bold;
            m_Score = UIKit.Line("Score", s, "0 pts", 30, Color.white, TextAlignmentOptions.MidlineRight, new Vector2(0.55f, 0.80f), new Vector2(1f, 1f), 14, 4);
            UIKit.Panel("Sep", s, new Color(0.3f, 0.9f, 0.4f, 0.35f), new Vector2(0, 0.795f), new Vector2(1, 0.8f), new Vector2(12, 0), new Vector2(-12, 0), false);

            m_Speaker = UIKit.Line("Speaker", s, "", 21, UIKit.Cyan, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.69f), new Vector2(1, 0.79f), 14, 0);
            m_Subtitle = UIKit.Block("Subtitle", s, "", 16, 24, Color.white, new Vector2(0, 0.33f), new Vector2(1, 0.69f), 14);

            var obj = UIKit.Panel("ObjBox", s, new Color(1f, 0.8f, 0.25f, 0.12f), new Vector2(0, 0.105f), new Vector2(1, 0.32f), new Vector2(8, 0), new Vector2(-8, 0));
            UIKit.Line("ObjLabel", obj.transform, "OBJETIVO", 13, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.TopLeft, new Vector2(0, 0.68f), new Vector2(1, 1), 8, 2);
            m_Objective = UIKit.Block("Objective", obj.transform, "", 12, 17, new Color(1f, 0.93f, 0.75f), new Vector2(0, 0), new Vector2(1, 0.7f), 8);
            m_Footer = UIKit.Line("Footer", s, "", 14, new Color(0.62f, 0.85f, 0.65f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 0.1f), 14, 0);

            // Cartel de notificaciones (flota sobre la muneca): fondo opaco para que se lea sobre cualquier cosa
            m_Banner = UIKit.WorldCanvas("NeonBanner", transform, new Vector2(760, 180), 0.00035f, false);
            m_Banner.transform.localPosition = new Vector3(0, 0.1f, 0.02f);
            UIKit.Fill("Border", m_Banner.transform, new Color(0.35f, 1f, 0.45f, 0.9f));
            m_BannerBg = UIKit.Panel("Bg", m_Banner.transform, new Color(0.02f, 0.06f, 0.04f, 0.97f), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            m_BannerText = UIKit.Block("Text", m_BannerBg.transform, "", 26, 42, UIKit.Neon, Vector2.zero, Vector2.one, 20);
            m_BannerText.alignment = TextAlignmentOptions.Center;
            m_BannerText.fontStyle = FontStyles.Bold;
            m_Banner.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (RickComms.Instance != null) RickComms.Instance.SubtitleChanged += OnSubtitle;
            GameEvents.ItemDiscovered += OnDiscovered;
        }

        void Start()
        {
            if (RickComms.Instance != null)
            {
                RickComms.Instance.SubtitleChanged -= OnSubtitle;
                RickComms.Instance.SubtitleChanged += OnSubtitle;
            }
        }

        void OnDisable()
        {
            if (RickComms.Instance != null) RickComms.Instance.SubtitleChanged -= OnSubtitle;
            GameEvents.ItemDiscovered -= OnDiscovered;
        }

        void OnSubtitle(string speaker, string text)
        {
            m_Speaker.text = string.IsNullOrEmpty(text) ? "" : "<b>" + speaker + "</b> (radio)";
            m_Subtitle.text = text ?? "";
        }

        void OnDiscovered(ItemDef def)
        {
            Notify(UIKit.TitleTag("¡NUEVO DESCUBRIMIENTO!") + "\n" + def.name.ToUpper(), UIKit.Neon, 5f);
            AudioFX.Play("arcade_win", transform.position, 0.8f);
            PlayerRig.Instance?.Haptic(Hand.Left, 0.9f, 0.4f);
        }

        public void SetObjective(string text)
        {
            if (m_Objective != null) m_Objective.text = text;
        }

        public void Notify(string text, Color color, float seconds = 4f)
        {
            if (m_BannerCo != null) StopCoroutine(m_BannerCo);
            m_BannerCo = StartCoroutine(CoBanner(text, color, seconds));
        }

        IEnumerator CoBanner(string text, Color color, float seconds)
        {
            m_Banner.gameObject.SetActive(true);
            m_BannerText.text = text;
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float flicker = (t < 0.4f) ? (Random.value > 0.4f ? 1f : 0.3f) : 1f;
                var c = color; c.a = flicker * Mathf.Clamp01((seconds - t) / 0.4f);
                m_BannerText.color = c;
                var bg = m_BannerBg.color; bg.a = 0.97f * Mathf.Clamp01((seconds - t) / 0.4f); m_BannerBg.color = bg;
                if (head != null)
                {
                    var dir = m_Banner.transform.position - head.position;
                    if (dir.sqrMagnitude > 0.0001f) m_Banner.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
                yield return null;
            }
            m_Banner.gameObject.SetActive(false);
            m_BannerCo = null;
        }

        int m_LastSecs = -1, m_LastScore = int.MinValue, m_LastFound = -1, m_Discoverable = -1;

        void Update()
        {
            // Los textos solo se reescriben cuando cambia el valor (antes se armaban strings en cada cuadro → basura/tirones en Quest)
            var timer = GameTimer.Instance;
            if (timer != null)
            {
                if (timer.Running || timer.Remaining < timer.duration)
                {
                    int secs = Mathf.CeilToInt(timer.Remaining);
                    if (secs != m_LastSecs) { m_LastSecs = secs; m_Timer.text = "FBI " + GameTimer.Format(timer.Remaining); }
                    bool danger = timer.Remaining < 180f;
                    m_Timer.color = danger ? (Mathf.Repeat(Time.time, 1f) > 0.5f ? UIKit.Danger : Color.white) : UIKit.Neon;
                }
                else if (m_LastSecs != -2) { m_LastSecs = -2; m_Timer.text = "--:--"; }
            }
            var score = ScoreManager.Instance;
            if (score != null)
            {
                if (score.Score != m_LastScore) { m_LastScore = score.Score; m_Score.text = score.Score + " pts"; }
                int found = score.Discovered.Count;
                if (found != m_LastFound)
                {
                    m_LastFound = found;
                    if (m_Discoverable < 0) m_Discoverable = GameDatabase.DiscoverableCount;
                    m_Footer.text = $"Colección {found}/{m_Discoverable}  ·  mira la muñeca para verla";
                }
            }

            // Deteccion de "mirar el reloj" (angulo del visor respecto al reloj)
            if (head != null)
            {
                var toHead = head.position - transform.position;
                float dist = toHead.magnitude;
                float dot = dist > 0.0001f ? Vector3.Dot(transform.up, toHead / dist) : 0f;
                bool looking = dist < lookMaxDistance && dot > lookDot;
                if (looking) m_LookTime += Time.deltaTime; else m_LookTime = 0f;
                IsLooking = m_LookTime > 0.25f;
                if (hologram != null) hologram.SetVisible(IsLooking || hologram.IsPointerOver);
            }
        }
    }
}
