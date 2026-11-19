using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RickMorty
{
    /// <summary>
    /// Menu de pausa / opciones (boton Menu del control izquierdo, o tecla P en el simulador).
    /// Opciones de confort: vineta dinamica y giro suave/por pasos.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        public Transform head;
        public Transform rightControllerRoot;

        Canvas m_Canvas;
        GameObject m_Dim;
        TextMeshProUGUI m_VignetteLabel, m_TurnLabel, m_Info;
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
            if (GetComponent<ControlsPanel>() == null) gameObject.AddComponent<ControlsPanel>();   // pantalla de controles (boton Y / tecla B)
            Build();
        }

        void OnEnable() => m_Toggle.Enable();
        void OnDisable() => m_Toggle.Disable();
        void OnDestroy()
        {
            m_Toggle.Dispose();
            Time.timeScale = 1f;   // reiniciar desde la pausa no debe dejar el juego congelado
        }

        void Build()
        {
            // Esfera oscura alrededor de la cabeza: atenua el mundo para que se sienta que es un menu
            m_Dim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(m_Dim.GetComponent<Collider>());
            m_Dim.name = "AtenuadorPausa";
            m_Dim.transform.SetParent(transform, false);
            m_Dim.transform.localScale = Vector3.one * 0.5f;   // radio 25 cm: queda delante de TODO el mundo (la mesa esta a < 1 m)
            var dr = m_Dim.GetComponent<Renderer>();
            dr.sharedMaterial = MatLib.Transparent(new Color(0.01f, 0.02f, 0.04f, 0.72f));
            dr.sortingOrder = 40;   // por encima de los paneles del mundo, por debajo del menu (50)
            m_Dim.SetActive(false);

            m_Canvas = UIKit.WorldCanvas("PauseCanvas", transform, new Vector2(700, 660), 0.001f, true);
            m_Canvas.sortingOrder = 50;
            var root = m_Canvas.transform;
            UIKit.Fill("Marco", root, new Color(0.35f, 1f, 0.45f, 0.95f));
            var panel = UIKit.Panel("Bg", root, new Color(0.03f, 0.07f, 0.06f, 0.98f), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));
            root = panel.transform;
            var title = UIKit.Text("Title", root, "PAUSA", 56, UIKit.Neon, TextAlignmentOptions.Center,
                new Vector2(0, 0.86f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            UIKit.AsTitle(title);

            UIKit.Button("Resume", root, "Reanudar", Close, new Vector2(0.1f, 0.745f), new Vector2(0.9f, 0.845f));
            UIKit.Button("Controls", root, "Ver controles  (botón Y · tecla B)", () => ControlsPanel.Instance?.Open(), new Vector2(0.1f, 0.63f), new Vector2(0.9f, 0.73f), color: new Color(0.2f, 0.42f, 0.25f, 0.95f));
            var v = UIKit.Button("Vignette", root, "", ToggleVignette, new Vector2(0.1f, 0.515f), new Vector2(0.9f, 0.615f), color: new Color(0.15f, 0.3f, 0.5f, 0.95f));
            m_VignetteLabel = v.GetComponentInChildren<TextMeshProUGUI>();
            var t = UIKit.Button("Turn", root, "", ToggleTurn, new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.50f), color: new Color(0.15f, 0.3f, 0.5f, 0.95f));
            m_TurnLabel = t.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.Button("Restart", root, "Reiniciar partida", Restart, new Vector2(0.1f, 0.285f), new Vector2(0.9f, 0.385f), color: new Color(0.5f, 0.35f, 0.1f, 0.95f));
            UIKit.Button("Quit", root, "Salir del juego", Quit, new Vector2(0.1f, 0.17f), new Vector2(0.9f, 0.27f), color: new Color(0.5f, 0.12f, 0.1f, 0.95f));
            m_Info = UIKit.Text("Info", root, "", 22, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center,
                new Vector2(0, 0.02f), new Vector2(1, 0.15f), new Vector2(10, 0), new Vector2(-10, 0));
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
            m_Info.text = $"Mejor puntaje: {s.bestScore}   ·   Partidas: {s.runsPlayed}   ·   Colección: {s.collection.Count}/{GameDatabase.DiscoverableCount}";
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Ending || gm.State == GameState.GameOver)) return;
            RefreshLabels();
            m_Canvas.gameObject.SetActive(true);
            if (head != null)
            {
                var f = head.forward; f.y = 0;
                if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                f.Normalize();
                m_Canvas.transform.position = head.position + f * 0.9f;
                m_Canvas.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
                m_Dim.transform.SetParent(head, false);   // sigue la cabeza mientras la pausa este abierta
                m_Dim.transform.localPosition = Vector3.zero;
            }
            m_Dim.SetActive(true);
            if (GameTimer.Instance != null) GameTimer.Instance.Paused = true;
            // pausa real: se congela el mundo (eventos, criaturas, fisica, locomocion). La UI XR sigue
            // respondiendo porque los interactores corren en Update, no dependen de deltaTime.
            Time.timeScale = 0f;
        }

        public void Close()
        {
            m_Canvas.gameObject.SetActive(false);
            if (m_Dim != null) m_Dim.SetActive(false);
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

        /// <summary>
        /// Cambia el giro del control derecho usando el ControllerInputActionManager de los
        /// Starter Assets (por reflexion para no depender del ensamblado del sample).
        /// </summary>
        void ApplyTurnMode(bool smooth)
        {
            if (rightControllerRoot == null) return;
            foreach (var mb in rightControllerRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "ControllerInputActionManager") continue;
                var prop = mb.GetType().GetProperty("smoothTurnEnabled", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    try { prop.SetValue(mb, smooth); }
                    catch (Exception e) { Debug.LogWarning("[PauseMenu] No se pudo cambiar el giro: " + e.Message); }
                }
            }
        }

        void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
