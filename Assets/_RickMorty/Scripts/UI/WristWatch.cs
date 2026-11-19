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
        public float lookDot = 0.6f;
        public float lookMaxDistance = 0.6f;
        [Tooltip("Ademas el jugador debe estar MIRANDO hacia el reloj (coseno del angulo entre la vista y el reloj)")]
        public float gazeDot = 0.74f;

        TextMeshProUGUI m_Timer, m_Score, m_Footer, m_TimerLabel;
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

            // Pantalla del reloj: SOLO lo que se lee de un vistazo (tiempo, puntaje, coleccion) y en grande.
            // Antes tambien traia subtitulos y objetivo con letra de 0.2-0.5 grados: ilegibles en el visor.
            // Ese texto ahora sale en el monitor del garaje y en la franja de avisos (HudStrip).
            body.transform.localScale = new Vector3(0.088f, 0.012f, 0.074f);
            // marco y botones laterales para que no parezca una caja gris
            var bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bezel.GetComponent<Collider>());
            bezel.name = "WatchBezel";
            bezel.transform.SetParent(transform, false);
            bezel.transform.localPosition = new Vector3(0, 0.0045f, 0);
            bezel.transform.localScale = new Vector3(0.094f, 0.006f, 0.08f);
            bezel.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.12f, 0.13f, 0.14f), 0f, 0.75f, 0.8f);
            for (int i = 0; i < 2; i++)
            {
                var knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(knob.GetComponent<Collider>());
                knob.name = "WatchKnob";
                knob.transform.SetParent(transform, false);
                knob.transform.localPosition = new Vector3(0.05f, 0.002f, i == 0 ? -0.016f : 0.016f);
                knob.transform.localRotation = Quaternion.Euler(0, 0, 90);
                knob.transform.localScale = new Vector3(0.011f, 0.004f, 0.011f);
                knob.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(i == 0 ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.6f, 0.15f), 0.6f, 0.5f, 0.3f);
            }

            var canvas = UIKit.WorldCanvas("WatchCanvas", transform, new Vector2(400, 330), 0.0002f, false);
            canvas.transform.localPosition = new Vector3(0, 0.0078f, 0);
            canvas.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var root = canvas.transform;

            m_Bg = UIKit.Fill("Bg", root, new Color(0.10f, 0.12f, 0.11f, 1f));
            m_Screen = UIKit.Panel("Screen", root, new Color(0.02f, 0.07f, 0.04f, 1f), Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            var s = m_Screen.transform;

            m_TimerLabel = UIKit.Line("TimerLabel", s, "EL FBI ENTRA EN", 24, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.86f), new Vector2(1, 1f), 16, 2);
            m_Timer = UIKit.Line("Timer", s, "--:--", 104, UIKit.Neon, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.44f), new Vector2(1f, 0.88f), 14, 0);
            m_Timer.fontStyle = FontStyles.Bold;
            UIKit.Panel("Sep", s, new Color(0.3f, 0.9f, 0.4f, 0.35f), new Vector2(0, 0.425f), new Vector2(1, 0.432f), new Vector2(12, 0), new Vector2(-12, 0), false);
            m_Score = UIKit.Line("Score", s, "0 pts", 54, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.18f), new Vector2(1f, 0.42f), 16, 2);
            m_Score.fontStyle = FontStyles.Bold;
            m_Footer = UIKit.Line("Footer", s, "", 27, new Color(0.62f, 0.85f, 0.65f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.02f), new Vector2(1, 0.18f), 16, 0);

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
            GameEvents.ItemDiscovered += OnDiscovered;
        }

        void OnDisable()
        {
            GameEvents.ItemDiscovered -= OnDiscovered;
            // si el control se apaga (se pierde el tracking) con un cartel a medias, no debe quedar pegado
            if (m_Banner != null) m_Banner.gameObject.SetActive(false);
            m_BannerCo = null;
        }

        void OnDiscovered(ItemDef def)
        {
            Notify(UIKit.TitleTag("¡NUEVO DESCUBRIMIENTO!") + "\n" + def.name.ToUpper(), UIKit.Neon, 5f);
            AudioFX.Play("arcade_win", transform.position, 0.8f);
            PlayerRig.Instance?.Haptic(Hand.Left, 0.9f, 0.4f);
        }

        /// <summary>El objetivo ya no se escribe en el reloj (no se podia leer): va al monitor y a la franja de avisos.</summary>
        public void SetObjective(string text) { }

        public void Notify(string text, Color color, float seconds = 4f)
        {
            // Los avisos salen delante del jugador (franja pegada al cuerpo). Antes flotaban sobre la muneca:
            // con la mano abajo no se veian nunca.
            var strip = HudStrip.Instance;
            if (strip != null) { strip.Notify(text, color, seconds); return; }
            if (!isActiveAndEnabled) return;   // control apagado: no se puede arrancar la corrutina
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
                float flicker = 1f;   // sin estrobo: un cartel que parpadea a 30 cm de los ojos molesta
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
                    if (secs != m_LastSecs) { m_LastSecs = secs; m_Timer.text = GameTimer.Format(timer.Remaining); }
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
                    m_Footer.text = $"Colección {found}/{m_Discoverable}";
                }
            }

            // Deteccion de "mirar el reloj" (angulo del visor respecto al reloj)
            if (head != null)
            {
                var toHead = head.position - transform.position;
                float dist = toHead.magnitude;
                float dot = dist > 0.0001f ? Vector3.Dot(transform.up, toHead / dist) : 0f;
                // la cara del reloj apunta a la cabeza Y el jugador esta mirando hacia el reloj
                // (antes bastaba con tener la mano a la altura de la mesa: el holograma saltaba solo mientras se trabajaba)
                float gaze = dist > 0.0001f ? Vector3.Dot(head.forward, -toHead / dist) : 0f;
                bool looking = dist < lookMaxDistance && dot > lookDot && gaze > gazeDot;
                if (looking) m_LookTime += Time.deltaTime; else m_LookTime = 0f;
                IsLooking = m_LookTime > 0.25f;
                if (hologram != null) hologram.SetVisible(IsLooking || hologram.IsPointerOver);
            }
        }
    }
}
