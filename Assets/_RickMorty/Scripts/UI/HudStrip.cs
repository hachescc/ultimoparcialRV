using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    /// <summary>
    /// Franja de avisos "pegada al cuerpo": flota delante del jugador, un poco por debajo de la vista,
    /// y lo sigue con retardo (no esta clavada a la cabeza, que marea). Muestra:
    ///  1) notificaciones (descubrimientos, eventos, penalizaciones) — siempre;
    ///  2) subtitulos de Rick y el objetivo cuando cambia — solo si el monitor del garaje NO esta a la vista.
    /// Antes todo eso vivia en el reloj de la muneca con letra de 0.3 grados: no se podia leer en el visor
    /// y lejos de la mesa el jugador se quedaba sin saber que hacer.
    /// </summary>
    public class HudStrip : MonoBehaviour
    {
        public static HudStrip Instance { get; private set; }

        public Transform head;
        [Tooltip("Distancia a la cabeza (m)")] public float distance = 1.05f;
        [Tooltip("Cuanto baja respecto a los ojos (m)")] public float drop = 0.34f;
        [Tooltip("Si el jugador gira mas que esto, la franja se vuelve a centrar")] public float recenterAngle = 32f;
        public float objectiveSeconds = 7f;

        Canvas m_Canvas;
        CanvasGroup m_Group;
        Image m_Border;
        TextMeshProUGUI m_Label, m_Text;
        float m_Yaw, m_YawVel;
        bool m_Recentering, m_Subscribed;
        Vector3 m_PosVel;

        string m_Note, m_SubSpeaker, m_SubText, m_Objective;
        Color m_NoteColor;
        float m_NoteUntil, m_ObjUntil, m_Alpha, m_Pop;
        int m_ShownKind = -1;
        string m_ShownText;

        static readonly Color Amber = new Color(1f, 0.82f, 0.35f);

        public static HudStrip Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("HUD (franja de avisos)").AddComponent<HudStrip>();
        }

        void Awake()
        {
            Instance = this;
            m_Canvas = UIKit.WorldCanvas("HudCanvas", transform, new Vector2(920, 230), 0.001f, false);
            m_Canvas.sortingOrder = 8;
            m_Group = m_Canvas.gameObject.AddComponent<CanvasGroup>();
            m_Group.alpha = 0f;
            m_Group.blocksRaycasts = false;
            m_Group.interactable = false;
            var root = m_Canvas.transform;
            m_Border = UIKit.Fill("Borde", root, new Color(0.35f, 1f, 0.45f, 0.9f));
            var bg = UIKit.Panel("Fondo", root, new Color(0.02f, 0.05f, 0.04f, 0.93f), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            m_Label = UIKit.Line("Etiqueta", bg.transform, "", 30, UIKit.Cyan, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.74f), new Vector2(1, 1f), 26, 4);
            m_Label.fontStyle = FontStyles.Bold;
            m_Text = UIKit.Block("Texto", bg.transform, "", 30, 44, Color.white, new Vector2(0, 0), new Vector2(1, 0.76f), 26);
            m_Canvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (m_Subscribed && RickComms.Instance != null) RickComms.Instance.SubtitleChanged -= OnSubtitle;
        }

        void OnSubtitle(string speaker, string text)
        {
            m_SubSpeaker = speaker;
            m_SubText = text;
        }

        /// <summary>Aviso corto y destacado (se ve siempre, aunque el monitor este a la vista).</summary>
        public void Notify(string text, Color color, float seconds = 4f)
        {
            m_Note = text;
            m_NoteColor = color;
            m_NoteUntil = Time.unscaledTime + seconds;
            m_Pop = 0f;
        }

        /// <summary>Objetivo nuevo: se muestra unos segundos cuando cambia.</summary>
        public void ShowObjective(string text)
        {
            if (string.IsNullOrEmpty(text) || text == "..." || text == m_Objective) return;
            m_Objective = text;
            m_ObjUntil = Time.unscaledTime + objectiveSeconds;
        }

        /// <summary>Vuelve a mostrar el objetivo actual unos segundos (recordatorio cuando el jugador no avanza).</summary>
        public void Remind()
        {
            if (!string.IsNullOrEmpty(m_Objective)) m_ObjUntil = Time.unscaledTime + objectiveSeconds;
        }

        bool MonitorInView()
        {
            var mon = RickMonitor.Instance;
            if (mon == null || head == null) return false;
            var v = mon.transform.position - head.position;
            float d = v.magnitude;
            return d < 3.0f && d > 0.01f && Vector3.Angle(head.forward, v) < 36f;
        }

        void LateUpdate()
        {
            if (head == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                head = cam.transform;
            }
            if (!m_Subscribed && RickComms.Instance != null)
            {
                RickComms.Instance.SubtitleChanged += OnSubtitle;
                m_Subscribed = true;
            }

            // --- que mostrar (prioridad: aviso > subtitulo > objetivo)
            var gm = GameManager.Instance;
            bool ended = gm != null && (gm.State == GameState.Ending || gm.State == GameState.GameOver);
            bool paused = Time.timeScale == 0f;
            int kind = -1;
            string text = null;
            if (!ended && !paused)
            {
                if (Time.unscaledTime < m_NoteUntil && !string.IsNullOrEmpty(m_Note)) { kind = 0; text = m_Note; }
                else if (!MonitorInView())
                {
                    if (!string.IsNullOrEmpty(m_SubText)) { kind = 1; text = m_SubText; }
                    else if (Time.unscaledTime < m_ObjUntil && !string.IsNullOrEmpty(m_Objective)) { kind = 2; text = m_Objective; }
                }
            }

            if (kind >= 0 && (kind != m_ShownKind || text != m_ShownText))
            {
                m_ShownKind = kind;
                m_ShownText = text;
                if (kind == 0)
                {
                    m_Label.text = "";
                    m_Text.text = text;
                    m_Text.color = m_NoteColor;
                    m_Text.fontStyle = FontStyles.Bold;
                    m_Text.alignment = TextAlignmentOptions.Center;
                    m_Text.rectTransform.anchorMax = new Vector2(1, 1);
                    m_Border.color = new Color(m_NoteColor.r, m_NoteColor.g, m_NoteColor.b, 0.95f);
                }
                else
                {
                    m_Label.text = kind == 1 ? (m_SubSpeaker ?? "Rick") : "OBJETIVO";
                    m_Label.color = kind == 1 ? UIKit.Cyan : Amber;
                    m_Text.text = text;
                    m_Text.color = kind == 1 ? Color.white : new Color(1f, 0.95f, 0.8f);
                    m_Text.fontStyle = FontStyles.Normal;
                    m_Text.alignment = TextAlignmentOptions.TopLeft;
                    m_Text.rectTransform.anchorMax = new Vector2(1, 0.76f);
                    m_Border.color = kind == 1 ? new Color(0.3f, 0.75f, 1f, 0.9f) : new Color(1f, 0.8f, 0.3f, 0.9f);
                }
            }

            float dt = Time.unscaledDeltaTime;
            bool wasHidden = m_Alpha <= 0.001f;
            m_Alpha = Mathf.MoveTowards(m_Alpha, kind >= 0 ? 1f : 0f, dt * 5f);
            if (m_Alpha <= 0.001f)
            {
                if (m_Canvas.gameObject.activeSelf) m_Canvas.gameObject.SetActive(false);
                m_ShownKind = -1;
                return;
            }
            if (!m_Canvas.gameObject.activeSelf) m_Canvas.gameObject.SetActive(true);
            m_Group.alpha = m_Alpha;

            // --- seguimiento: gira con el cuerpo solo cuando el jugador se aleja mucho del centro
            float headYaw = head.eulerAngles.y;
            if (wasHidden) { m_Yaw = headYaw; m_YawVel = 0f; m_Recentering = false; }
            float err = Mathf.Abs(Mathf.DeltaAngle(m_Yaw, headYaw));
            if (err > recenterAngle) m_Recentering = true;
            if (m_Recentering)
            {
                m_Yaw = Mathf.SmoothDampAngle(m_Yaw, headYaw, ref m_YawVel, 0.28f, 720f, dt);
                if (err < 3f) m_Recentering = false;
            }
            var fwd = Quaternion.Euler(0, m_Yaw, 0) * Vector3.forward;
            var target = head.position + fwd * distance + Vector3.down * drop;
            var t = m_Canvas.transform;
            t.position = wasHidden ? target : Vector3.SmoothDamp(t.position, target, ref m_PosVel, 0.12f, 50f, dt);
            t.rotation = Quaternion.LookRotation(t.position - head.position, Vector3.up);

            // entrada con un pequeno "pop" (sin parpadeo)
            m_Pop = Mathf.MoveTowards(m_Pop, 1f, dt * 5f);
            float e = 1f - (1f - m_Pop) * (1f - m_Pop);
            t.localScale = Vector3.one * (0.001f * Mathf.Lerp(0.86f, 1f, e));
        }
    }
}
