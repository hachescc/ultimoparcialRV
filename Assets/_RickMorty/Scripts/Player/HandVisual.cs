using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    /// <summary>
    /// Manos del jugador en vez del modelo del control (pedido del equipo). Cada mano tiene dos poses:
    /// abierta (relajada) y cerrada (puno). Se cierra al apretar el Grip o mientras la mano sostiene algo.
    /// Modelos en el registro: mano_l_abierta, mano_l_cerrada, mano_r_abierta, mano_r_cerrada
    /// (generados con _Tools/blender/make_hands.py: dedos hacia +Z, palma hacia abajo, origen en la palma).
    /// Si falta alguno, queda el control como estaba.
    /// </summary>
    public class HandVisual : MonoBehaviour
    {
        public bool left;
        [Tooltip("Posicion de la palma respecto al control (valores de la mano DERECHA; la izquierda se refleja)")]
        public Vector3 localPosition = new Vector3(0.005f, -0.015f, -0.045f);
        [Tooltip("Giro respecto al control (mano DERECHA): Z inclina la palma hacia adentro")]
        public Vector3 localEuler = new Vector3(5f, 0f, -30f);

        GameObject m_Open, m_Closed;
        InputAction m_Grip;
        XRBaseInteractor[] m_Interactors;

        void Start()
        {
            var reg = ItemVisualRegistry.Get();
            string side = left ? "l" : "r";
            var open = reg != null ? reg.Find("mano_" + side + "_abierta") : null;
            var closed = reg != null ? reg.Find("mano_" + side + "_cerrada") : null;
            if (open == null || closed == null) { enabled = false; return; }

            // se ocultan los renderers del control, no el objeto: de el cuelgan cosas del rig
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.EndsWith("Controller Visual"))
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            var root = new GameObject(left ? "Mano Izquierda" : "Mano Derecha").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(left ? -localPosition.x : localPosition.x, localPosition.y, localPosition.z);
            root.localRotation = Quaternion.Euler(localEuler.x, left ? -localEuler.y : localEuler.y, left ? -localEuler.z : localEuler.z);
            m_Open = Instantiate(open.prefab, root);
            m_Closed = Instantiate(closed.prefab, root);
            foreach (var go in new[] { m_Open, m_Closed })
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            m_Closed.SetActive(false);
            m_Interactors = GetComponentsInChildren<XRBaseInteractor>(true);

            m_Grip = new InputAction("Grip", InputActionType.Button);
            string h = left ? "{LeftHand}" : "{RightHand}";
            m_Grip.AddBinding("<XRController>" + h + "/gripPressed");
            m_Grip.AddBinding("<XRController>" + h + "/gripButton");
            m_Grip.Enable();
        }

        void OnDestroy() { m_Grip?.Disable(); m_Grip?.Dispose(); }

        bool Holding()
        {
            if (m_Interactors == null) return false;
            foreach (var i in m_Interactors)
                if (i != null && i.isActiveAndEnabled && i.hasSelection) return true;
            return false;
        }

        void Update()
        {
            if (m_Open == null || m_Grip == null) return;
            bool closed = m_Grip.IsPressed() || Holding();
            if (m_Closed.activeSelf != closed)
            {
                m_Closed.SetActive(closed);
                m_Open.SetActive(!closed);
            }
        }
    }
}
