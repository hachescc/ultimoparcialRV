using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace RickMorty
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        public Transform head;
        public Transform rightControllerRoot;

        Canvas m_Canvas;
        GameObject m_Dim;
        TextMeshProUGUI m_VignetteLabel, m_TurnLabel, m_Info, m_RestartLabel, m_QuitLabel;
        readonly List<RectTransform> m_Rows = new List<RectTransform>();
        RectTransform m_SkipRow, m_HintRow;

        void AskHint()
        {
            Close();
            bool given = GameManager.Instance != null && GameManager.Instance.RequestHint();
            if (!given) HudStrip.Instance?.Notify("Rick no te va a dar otra pista todavía", UIKit.Cyan, 3f);
        }
        int m_Confirm;
        float m_ConfirmUntil;
        const string RestartText = "Reiniciar partida", QuitText = "Salir del juego";

        RectTransform Row(Button b)
        {
            var rt = (RectTransform)b.transform;
            m_Rows.Add(rt);
            return rt;
        }

        void Layout()
        {
            const float bh = 0.074f, gap = 0.038f;
            float y = 0.88f;
            foreach (var rt in m_Rows)
            {
                if (!rt.gameObject.activeSelf) continue;
                rt.anchorMin = new Vector2(0.1f, y - bh);
                rt.anchorMax = new Vector2(0.9f, y);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                y -= bh + gap;
            }
        }

        void SkipTutorial()
        {
            Close();
            GameManager.Instance?.DebugSkipTutorial();
        }

        void Update()
        {
            if (m_Confirm != 0 && Time.unscaledTime > m_ConfirmUntil) ResetConfirm();
        }

        void ResetConfirm()
        {
            m_Confirm = 0;
            if (m_RestartLabel != null) m_RestartLabel.text = RestartText;
            if (m_QuitLabel != null) m_QuitLabel.text = QuitText;
        }

        bool Confirmed(int what, TextMeshProUGUI label)
        {
            if (m_Confirm == what && Time.unscaledTime <= m_ConfirmUntil) return true;
            ResetConfirm();
            m_Confirm = what;
            m_ConfirmUntil = Time.unscaledTime + 4f;
            if (label != null) label.text = "¿Seguro? Toca otra vez";
            return false;
        }

#if !UNITY_EDITOR
        void OnApplicationFocus(bool focus) { if (!focus && !IsOpen) Open(); }
        void OnApplicationPause(bool paused) { if (paused && !IsOpen) Open(); }
#endif
        InputAction m_Toggle;
        bool m_ShownComfortLine;

        public bool IsOpen => m_Canvas != null && m_Canvas.gameObject.activeSelf;

        void Awake()
        {
            Instance = this;
            m_Toggle = new InputAction("PauseToggle", InputActionType.Button);
            m_Toggle.AddBinding("<XRController>{LeftHand}/menuButton");
            m_Toggle.AddBinding("<XRController>{LeftHand}/menu");
            m_Toggle.AddBinding("<Keyboard>/p");
            m_Toggle.performed += _ => Toggle();
            if (GetComponent<ControlsPanel>() == null) gameObject.AddComponent<ControlsPanel>();
            Build();
        }

        void OnEnable() => m_Toggle.Enable();
        void OnDisable() => m_Toggle.Disable();
        void OnDestroy()
        {
            m_Toggle.Dispose();
            Time.timeScale = 1f;
        }

        void Build()
        {
            m_Dim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(m_Dim.GetComponent<Collider>());
            m_Dim.name = "AtenuadorPausa";
            m_Dim.transform.SetParent(transform, false);
            m_Dim.transform.localScale = Vector3.one * 0.5f;
            var dr = m_Dim.GetComponent<Renderer>();
            dr.sharedMaterial = MatLib.Transparent(new Color(0.01f, 0.02f, 0.04f, 0.72f));
            dr.sortingOrder = 40;
            m_Dim.SetActive(false);

            m_Canvas = UIKit.WorldCanvas("PauseCanvas", transform, new Vector2(700, 820), 0.001f, true);
            m_Canvas.sortingOrder = 50;
            var root = m_Canvas.transform;
            UIKit.Fill("Marco", root, new Color(0.35f, 1f, 0.45f, 0.95f));
            var panel = UIKit.Panel("Bg", root, new Color(0.03f, 0.07f, 0.06f, 0.98f), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
            root = panel.transform;
            var title = UIKit.Text("Title", root, "PAUSA", 56, UIKit.Neon, TextAlignmentOptions.Center,
                new Vector2(0, 0.895f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            UIKit.AsTitle(title);

            Row(UIKit.Button("Resume", root, "Reanudar", Close, Vector2.zero, Vector2.one, fontSize: 30f));
            Row(UIKit.Button("Controls", root, "Ver controles", () => ControlsPanel.Instance?.Open(), Vector2.zero, Vector2.one, color: new Color(0.2f, 0.42f, 0.25f, 0.95f), fontSize: 30f));
            m_SkipRow = Row(UIKit.Button("Skip", root, "Saltar la explicación de Rick", SkipTutorial, Vector2.zero, Vector2.one, color: new Color(0.42f, 0.3f, 0.55f, 0.95f), fontSize: 30f));
            m_HintRow = Row(UIKit.Button("Hint", root, "Pedir una pista a Rick (-25 pts)", AskHint, Vector2.zero, Vector2.one, color: new Color(0.5f, 0.42f, 0.12f, 0.95f), fontSize: 30f));
            var v = UIKit.Button("Vignette", root, "", ToggleVignette, Vector2.zero, Vector2.one, color: new Color(0.15f, 0.3f, 0.5f, 0.95f), fontSize: 30f);
            Row(v);
            m_VignetteLabel = v.GetComponentInChildren<TextMeshProUGUI>();
            var t = UIKit.Button("Turn", root, "", ToggleTurn, Vector2.zero, Vector2.one, color: new Color(0.15f, 0.3f, 0.5f, 0.95f), fontSize: 30f);
            Row(t);
            m_TurnLabel = t.GetComponentInChildren<TextMeshProUGUI>();
            var rs = UIKit.Button("Restart", root, RestartText, Restart, Vector2.zero, Vector2.one, color: new Color(0.5f, 0.35f, 0.1f, 0.95f), fontSize: 30f);
            Row(rs);
            m_RestartLabel = rs.GetComponentInChildren<TextMeshProUGUI>();
            var q = UIKit.Button("Quit", root, QuitText, Quit, Vector2.zero, Vector2.one, color: new Color(0.5f, 0.12f, 0.1f, 0.95f), fontSize: 30f);
            Row(q);
            m_QuitLabel = q.GetComponentInChildren<TextMeshProUGUI>();
            m_Info = UIKit.Text("Info", root, "", 24, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center,
                new Vector2(0, 0.01f), new Vector2(1, 0.115f), new Vector2(10, 0), new Vector2(-10, 0));
            Layout();
            m_Canvas.gameObject.SetActive(false);
            RefreshLabels();
        }

        void Start()
        {
            ApplyTurnMode(SaveSystem.Data.smoothTurn);
        }

        void RefreshLabels()
        {
            bool vig = ComfortVignette.Instance == null || ComfortVignette.Instance.Enabled;
            m_VignetteLabel.text = "Viñeta de confort: " + (vig ? "ACTIVADA" : "DESACTIVADA");
            m_TurnLabel.text = "Giro: " + (SaveSystem.Data.smoothTurn ? "SUAVE" : "POR PASOS");
            var s = SaveSystem.Data;
            m_Info.text = $"Mejor puntaje: {s.bestScore}     Partidas: {s.runsPlayed}     Colección: {s.collection.Count}/{GameDatabase.DiscoverableCount}";
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsFinished) return;
            RefreshLabels();
            ResetConfirm();
            if (m_SkipRow != null)
                m_SkipRow.gameObject.SetActive(gm != null && (gm.State == GameState.Intro || gm.State == GameState.Onboarding || gm.State == GameState.Tutorial || gm.State == GameState.RickLeaving));
            if (m_HintRow != null) m_HintRow.gameObject.SetActive(gm != null && gm.State == GameState.FreePlay);
            Layout();
            ScreenFX.Instance?.ClearForPause();
            m_Canvas.gameObject.SetActive(true);
            AudioFX.Play2D("button", 0.4f, 0.8f);
            if (head != null)
            {
                var f = head.forward; f.y = 0;
                if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                f.Normalize();
                float dist = 0.9f;
                if (Physics.SphereCast(head.position, 0.2f, f, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    dist = Mathf.Clamp(hit.distance - 0.05f, 0.55f, 0.9f);
                m_Canvas.transform.position = head.position + f * dist;
                m_Canvas.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
                m_Dim.transform.SetParent(head, false);
                m_Dim.transform.localPosition = Vector3.zero;
            }
            m_Dim.SetActive(true);
            if (GameTimer.Instance != null) GameTimer.Instance.Paused = true;
            Time.timeScale = 0f;
        }

        public void Close()
        {
            m_Canvas.gameObject.SetActive(false);
            if (m_Dim != null) m_Dim.SetActive(false);
            if (ControlsPanel.Instance != null) ControlsPanel.Instance.Close();
            if (GameTimer.Instance != null) GameTimer.Instance.Paused = false;
            Time.timeScale = 1f;
        }

        void ToggleVignette()
        {
            var v = ComfortVignette.Instance;
            if (v == null) return;
            v.Enabled = !v.Enabled;
            RefreshLabels();
            if (v.Enabled && !m_ShownComfortLine)
            {
                m_ShownComfortLine = true;
                RickComms.Instance?.Say("comfort_on");
            }
        }

        void ToggleTurn()
        {
            SaveSystem.Data.smoothTurn = !SaveSystem.Data.smoothTurn;
            SaveSystem.Save();
            ApplyTurnMode(SaveSystem.Data.smoothTurn);
            RefreshLabels();
        }

        void ApplyTurnMode(bool smooth)
        {
            if (rightControllerRoot == null) return;
            foreach (var manager in rightControllerRoot.GetComponentsInChildren<ControllerInputActionManager>(true))
                manager.smoothTurnEnabled = smooth;
        }

        void Restart()
        {
            if (!Confirmed(1, m_RestartLabel)) return;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }

        void Quit()
        {
            if (!Confirmed(2, m_QuitLabel)) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
