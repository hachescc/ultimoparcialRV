using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RickMorty
{
    /// <summary>
    /// Pantalla de ayuda con los controles (visor y PC). Se abre/cierra con el boton Y del control
    /// izquierdo (en el simulador XR: tecla 2 con la mano izquierda) o la tecla B del teclado, o
    /// desde el menu de pausa. No pausa el juego: aparece frente a la vista y se puede cerrar igual.
    /// </summary>
    public class ControlsPanel : MonoBehaviour
    {
        public static ControlsPanel Instance { get; private set; }

        Canvas m_Canvas;
        InputAction m_Toggle;

        public bool IsOpen => m_Canvas != null && m_Canvas.gameObject.activeSelf;

        const string Quest =
            "<color=#7DFF6A>Joystick izquierdo</color>   caminar\n" +
            "<color=#7DFF6A>Joystick derecho</color>   girar (por pasos o suave)\n" +
            "<color=#7DFF6A>Grip</color> (el del dedo medio)   agarrar y soltar\n" +
            "<color=#7DFF6A>Gatillo</color> (índice)   usar lo que tienes en la mano\n" +
            "<color=#7DFF6A>Tocar el botón rojo</color>   mezclar lo que hay en el tazón\n" +
            "<color=#7DFF6A>Mirar la muñeca izq.</color>   tiempo, puntos y colección\n" +
            "<color=#7DFF6A>Apuntar al holograma</color>   ver recetas descubiertas\n" +
            "<color=#7DFF6A>Botón Menú</color> (izq.)   pausa\n" +
            "<color=#7DFF6A>Botón Y</color> (izq.)   esta pantalla de controles";

        const string Pc =
            "<color=#5FD0E8>W A S D</color>   caminar · <color=#5FD0E8>Q / E</color> bajar / subir\n" +
            "<color=#5FD0E8>Clic derecho + mouse</color>   mirar alrededor\n" +
            "<color=#5FD0E8>Tab</color>   cambiar entre cabeza y manos\n" +
            "<color=#5FD0E8>[  /  ]</color>   mover la mano izquierda / derecha\n" +
            "<color=#5FD0E8>G</color>   agarrar · <color=#5FD0E8>T</color>   gatillo (usar)\n" +
            "<color=#5FD0E8>M</color>  o  <color=#5FD0E8>P</color>   pausa\n" +
            "<color=#5FD0E8>B</color>   esta pantalla de controles";

        void Awake()
        {
            Instance = this;
            m_Toggle = new InputAction("ControlsToggle", InputActionType.Button);
            m_Toggle.AddBinding("<XRController>{LeftHand}/secondaryButton");
            m_Toggle.AddBinding("<Keyboard>/b");
            m_Toggle.performed += _ => Toggle();
            Build();
        }

        System.Collections.IEnumerator Start()
        {
            // recordatorio unico, cuando Rick ya termino de explicar lo basico (antes salia a los 9 s, en plena charla)
            yield return new WaitForSeconds(4f);
            while (GameManager.Instance != null && (GameManager.Instance.State == GameState.Intro || GameManager.Instance.State == GameState.Onboarding))
                yield return null;
            yield return new WaitForSeconds(3f);
            bool vr = UnityEngine.XR.XRSettings.isDeviceActive;
            WristWatch.Instance?.Notify(vr ? "Botón Y (mano izquierda)\nver los controles" : "Botón Y  ·  tecla B\nver los controles", UIKit.Cyan, 5f);
        }

        void OnEnable() => m_Toggle.Enable();
        void OnDisable() => m_Toggle.Disable();
        void OnDestroy() { m_Toggle.Dispose(); if (Instance == this) Instance = null; }

        bool m_BuiltForVr;

        void Build()
        {
            // En el visor solo se muestra la columna del visor (la del teclado no sirve ahi y ensanchaba el panel a 73 grados).
            // En el PC (simulador) se muestran las dos.
            bool vr = UnityEngine.XR.XRSettings.isDeviceActive;
            m_BuiltForVr = vr;
            if (m_Canvas != null) Destroy(m_Canvas.gameObject);
            m_Canvas = UIKit.WorldCanvas("ControlsCanvas", transform, new Vector2(vr ? 720 : 1280, 760), 0.001f, false);
            m_Canvas.sortingOrder = 60;   // por encima del menu de pausa
            var root = m_Canvas.transform;
            UIKit.Fill("Marco", root, new Color(0.35f, 1f, 0.45f, 0.95f));
            root = UIKit.Panel("Bg", root, new Color(0.03f, 0.07f, 0.06f, 0.98f), Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5)).transform;
            UIKit.AsTitle(UIKit.Line("Title", root, "CONTROLES", 64, UIKit.Neon, TextAlignmentOptions.Center,
                new Vector2(0, 0.87f), new Vector2(1, 0.99f), 20, 0));

            if (vr) Column(root, "CONTROLES DEL VISOR", Quest, 0.02f, 0.98f, new Color(0.06f, 0.16f, 0.09f));
            else
            {
                Column(root, "EN EL VISOR (META QUEST)", Quest, 0.02f, 0.5f, new Color(0.06f, 0.16f, 0.09f));
                Column(root, "EN EL PC (SIMULADOR XR)", Pc, 0.51f, 0.98f, new Color(0.05f, 0.12f, 0.17f));
            }

            UIKit.Line("Hint", root, vr ? "Botón Y para cerrar" : "Botón Y  ·  tecla B  ·  para cerrar", 26, new Color(0.75f, 0.85f, 0.8f), TextAlignmentOptions.Center,
                new Vector2(0, 0.01f), new Vector2(1, 0.08f), 10, 0);
            m_Canvas.gameObject.SetActive(false);
        }

        static void Column(Transform root, string header, string body, float x0, float x1, Color bg)
        {
            var box = UIKit.Panel("Col_" + header, root, bg, new Vector2(x0, 0.1f), new Vector2(x1, 0.85f), new Vector2(8, 4), new Vector2(-8, -4)).transform;
            UIKit.Line("H", box, header, 30, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.88f), new Vector2(1, 1), 18, 4);
            var b = UIKit.Block("B", box, body, 22, 27, new Color(0.9f, 0.95f, 0.92f), new Vector2(0, 0), new Vector2(1, 0.88f), 18);
            b.lineSpacing = 14;
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            // el visor puede activarse despues de Awake: si cambio el modo, se rehace el panel
            if (UnityEngine.XR.XRSettings.isDeviceActive != m_BuiltForVr) Build();
            var cam = Camera.main;
            if (cam != null)
            {
                var head = cam.transform;
                var f = head.forward; f.y = 0;
                if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                f.Normalize();
                // un poco mas cerca que la pausa (0.9 m) para quedar delante si ambas estan abiertas
                m_Canvas.transform.position = head.position + f * 0.86f + Vector3.down * 0.05f;
                m_Canvas.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            }
            m_Canvas.gameObject.SetActive(true);
            AudioFX.Play2D("button", 0.5f);
        }

        public void Close()
        {
            if (m_Canvas != null) m_Canvas.gameObject.SetActive(false);
        }
    }
}
