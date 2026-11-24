using TMPro;
using UnityEngine;

namespace RickMorty
{
    public class RickMonitor : MonoBehaviour
    {
        public static RickMonitor Instance { get; private set; }

        public Vector2 sizeMeters = new Vector2(1.3f, 0.75f);

        TextMeshProUGUI m_Objective, m_Subtitle, m_Status, m_Title;
        string m_ObjectiveText = "";

        void Awake()
        {
            Instance = this;
            const float mpp = 0.001f;
            var canvas = UIKit.WorldCanvas("MonitorCanvas", transform, sizeMeters / mpp, mpp, false);
            var root = canvas.transform;
            UIKit.Fill("Frame", root, new Color(0.16f, 0.2f, 0.18f, 1f));
            var screen = UIKit.Panel("Screen", root, new Color(0.015f, 0.05f, 0.03f, 1f), Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18));
            var s = screen.transform;

            m_Title = UIKit.Line("Title", s, "COMUNICADOR", 34, UIKit.Neon, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.865f), new Vector2(0.42f, 1f), 22, 6);
            UIKit.AsTitle(m_Title);
            m_Status = UIKit.Line("Status", s, "", 52, Color.white, TextAlignmentOptions.MidlineRight, new Vector2(0.4f, 0.855f), new Vector2(1f, 1f), 22, 2);
            m_Status.fontStyle = FontStyles.Bold;
            UIKit.Panel("Sep", s, new Color(0.3f, 0.9f, 0.4f, 0.45f), new Vector2(0, 0.85f), new Vector2(1, 0.855f), new Vector2(22, 0), new Vector2(-22, 0), false);

            m_Objective = UIKit.Block("Objective", s, "", 40, 56, Color.white, new Vector2(0, 0.455f), new Vector2(1, 0.84f), 22);

            var radio = UIKit.Panel("RadioBox", s, new Color(0.3f, 0.75f, 1f, 0.1f), new Vector2(0, 0.025f), new Vector2(1, 0.44f), new Vector2(14, 0), new Vector2(-14, 0));
            UIKit.Line("SubLabel", radio.transform, "RADIO DE RICK", 28, UIKit.Cyan, TextAlignmentOptions.TopLeft, new Vector2(0, 0.8f), new Vector2(1, 1), 14, 4);
            m_Subtitle = UIKit.Block("Subtitle", radio.transform, "", 36, 48, new Color(0.9f, 1f, 0.95f), new Vector2(0, 0), new Vector2(1, 0.82f), 14);
        }

        void Start()
        {
            if (RickComms.Instance != null) RickComms.Instance.SubtitleChanged += OnSubtitle;
        }

        void OnDestroy()
        {
            if (RickComms.Instance != null) RickComms.Instance.SubtitleChanged -= OnSubtitle;
        }

        void OnSubtitle(string speaker, string text)
        {
            m_Subtitle.text = string.IsNullOrEmpty(text) ? "" : $"<b>{speaker}:</b> {text}";
        }

        public void SetObjective(string text)
        {
            m_ObjectiveText = text;
            m_Objective.text = "<size=55%><color=#FFCC4D>OBJETIVO</color></size>\n" + text;
        }

        public string Objective => m_ObjectiveText;

        int m_LastSecs = int.MinValue, m_LastScore = int.MinValue;

        void Update()
        {
            var t = GameTimer.Instance;
            var s = ScoreManager.Instance;
            bool showTimer = t != null && (t.Running || t.Remaining < t.duration);
            int secs = showTimer ? Mathf.CeilToInt(t.Remaining) : -1;
            int pts = s != null ? s.Score : 0;
            if (secs == m_LastSecs && pts == m_LastScore) return;
            m_LastSecs = secs; m_LastScore = pts;
            string timer = showTimer ? "FBI " + GameTimer.Format(t.Remaining) + "   " : "";
            m_Status.text = timer + (s != null ? s.Score + " pts" : "");
        }
    }
}
