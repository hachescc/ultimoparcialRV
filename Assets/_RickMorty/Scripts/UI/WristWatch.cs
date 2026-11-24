using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    public class WristWatch : MonoBehaviour
    {
        public static WristWatch Instance { get; private set; }

        public Transform head;
        public DiscoveryHologram hologram;
        public float lookDot = 0.6f;
        public float lookMaxDistance = 0.6f;
        public float gazeDot = 0.74f;

        TextMeshProUGUI m_Timer, m_Score, m_Footer;
        Image m_Screen;
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
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.name = "WatchBody";
            body.transform.SetParent(transform, false);
            body.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.25f, 0.27f, 0.25f), 0f, 0.6f, 0.6f);
            var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(tape.GetComponent<Collider>());
            tape.name = "CintaAmericana";
            tape.transform.SetParent(transform, false);
            tape.transform.localPosition = new Vector3(0, -0.006f, 0);
            tape.transform.localScale = new Vector3(0.085f, 0.008f, 0.03f);
            tape.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.6f, 0.6f, 0.62f), 0f, 0.2f);

            body.transform.localScale = new Vector3(0.088f, 0.012f, 0.074f);
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

            UIKit.Fill("Bg", root, new Color(0.10f, 0.12f, 0.11f, 1f));
            m_Screen = UIKit.Panel("Screen", root, new Color(0.02f, 0.07f, 0.04f, 1f), Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            var s = m_Screen.transform;

            UIKit.Line("TimerLabel", s, "EL FBI ENTRA EN", 24, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.86f), new Vector2(1, 1f), 16, 2);
            m_Timer = UIKit.Line("Timer", s, "--:--", 104, UIKit.Neon, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.44f), new Vector2(1f, 0.88f), 14, 0);
            m_Timer.fontStyle = FontStyles.Bold;
            UIKit.Panel("Sep", s, new Color(0.3f, 0.9f, 0.4f, 0.35f), new Vector2(0, 0.425f), new Vector2(1, 0.432f), new Vector2(12, 0), new Vector2(-12, 0), false);
            m_Score = UIKit.Line("Score", s, "0 pts", 54, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.18f), new Vector2(1f, 0.42f), 16, 2);
            m_Score.fontStyle = FontStyles.Bold;
            m_Footer = UIKit.Line("Footer", s, "", 27, new Color(0.62f, 0.85f, 0.65f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.02f), new Vector2(1, 0.18f), 16, 0);
        }

        void OnEnable()
        {
            GameEvents.ItemDiscovered += OnDiscovered;
        }

        void OnDisable()
        {
            GameEvents.ItemDiscovered -= OnDiscovered;
        }

        void OnDiscovered(ItemDef def)
        {
            Notify(UIKit.TitleTag("¡NUEVO DESCUBRIMIENTO!") + "\n" + def.name.ToUpper(), UIKit.Neon, 5f);
            AudioFX.Play("arcade_win", transform.position, 0.8f);
            PlayerRig.Instance?.Haptic(Hand.Left, 0.9f, 0.4f);
        }

        public void Notify(string text, Color color, float seconds = 4f)
        {
            if (HudStrip.Instance != null) HudStrip.Instance.Notify(text, color, seconds);
        }

        int m_LastSecs = -1, m_LastScore = int.MinValue, m_LastFound = -1, m_Discoverable = -1;

        void Update()
        {
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

            if (head != null)
            {
                var toHead = head.position - transform.position;
                float dist = toHead.magnitude;
                float dot = dist > 0.0001f ? Vector3.Dot(transform.up, toHead / dist) : 0f;
                float gaze = dist > 0.0001f ? Vector3.Dot(head.forward, -toHead / dist) : 0f;
                bool faceOk = dot > lookDot || EditorSimulatorToggle.SimulatorOn;
                bool looking = dist < lookMaxDistance && faceOk && gaze > gazeDot;
                if (looking) m_LookTime += Time.deltaTime; else m_LookTime = 0f;
                IsLooking = m_LookTime > 0.25f;
                if (hologram != null) hologram.SetVisible(IsLooking || hologram.IsPointerOver);
            }
        }
    }
}
