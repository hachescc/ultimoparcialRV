using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    public class HudStrip : MonoBehaviour
    {
        public static HudStrip Instance { get; private set; }

        public Transform head;
        public float distance = 1.0f;
        public float angleDown = 15f;
        public float followSpeed = 6f;
        public float objectiveSeconds = 7f;

        Canvas m_Canvas;
        CanvasGroup m_Group;
        Image m_Border;
        TextMeshProUGUI m_Label, m_Text;
        bool m_Subscribed;
        Vector3 m_Dir;

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
            m_Canvas = UIKit.WorldCanvas("HudCanvas", transform, new Vector2(600, 170), 0.001f, false);
            m_Canvas.sortingOrder = 8;
            m_Group = m_Canvas.gameObject.AddComponent<CanvasGroup>();
            m_Group.alpha = 0f;
            m_Group.blocksRaycasts = false;
            m_Group.interactable = false;
            var root = m_Canvas.transform;
            m_Border = UIKit.Fill("Borde", root, new Color(0.35f, 1f, 0.45f, 0.75f));
            var bg = UIKit.Panel("Fondo", root, new Color(0.02f, 0.07f, 0.06f, 0.84f), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            m_Label = UIKit.Line("Etiqueta", bg.transform, "", 26, UIKit.Cyan, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.74f), new Vector2(1, 1f), 20, 4);
            m_Label.fontStyle = FontStyles.Bold;
            m_Text = UIKit.Block("Texto", bg.transform, "", 24, 32, Color.white, new Vector2(0, 0), new Vector2(1, 0.76f), 20);
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

        public void Notify(string text, Color color, float seconds = 4f)
        {
            m_Note = text;
            m_NoteColor = color;
            m_NoteUntil = Time.unscaledTime + seconds;
            m_Pop = 0f;
        }

        public void ShowObjective(string text)
        {
            if (string.IsNullOrEmpty(text) || text == "..." || text == m_Objective) return;
            m_Objective = text;
            m_ObjUntil = Time.unscaledTime + objectiveSeconds;
        }

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

            var gm = GameManager.Instance;
            bool ended = gm != null && gm.IsFinished;
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

            Vector3 wanted = Quaternion.AngleAxis(angleDown, head.right) * head.forward;
            if (wasHidden || m_Dir == Vector3.zero) m_Dir = wanted;
            else m_Dir = Vector3.Slerp(m_Dir, wanted, 1f - Mathf.Exp(-followSpeed * dt));
            var t = m_Canvas.transform;
            t.position = head.position + m_Dir.normalized * distance;
            t.rotation = Quaternion.LookRotation(t.position - head.position, Vector3.up);

            m_Pop = Mathf.MoveTowards(m_Pop, 1f, dt * 5f);
            float e = 1f - (1f - m_Pop) * (1f - m_Pop);
            t.localScale = Vector3.one * (0.001f * Mathf.Lerp(0.86f, 1f, e));
        }
    }
}
