using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace RickMorty
{
    /// <summary>
    /// Activa el XR Interaction Simulator (mouse + teclado) solo en el Editor y solo si no
    /// hay un visor conectado. En builds (Quest) se destruye. El objeto usa el tag EditorOnly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class EditorSimulatorToggle : MonoBehaviour
    {
        public GameObject simulator;

        void Awake()
        {
#if UNITY_EDITOR
            bool hmd = false;
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays) if (d.running) hmd = true;
            if (simulator != null) simulator.SetActive(!hmd);
            if (hmd) Debug.Log("[RickMorty] Visor detectado: simulador desactivado.");
            else Debug.Log("[RickMorty] Sin visor: usando XR Interaction Simulator (mouse/teclado).");
#else
            Destroy(gameObject);
#endif
        }
    }
}
