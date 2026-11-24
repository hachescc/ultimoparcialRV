using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace RickMorty
{
    /// <summary>
    /// Activa el XR Interaction Simulator (mouse + teclado) solo en el Editor y solo si no
    /// hay un visor conectado. En builds (Quest) se destruye. El objeto usa el tag EditorOnly.
    /// Lo usan: GameManager, RickComms y WristWatch (SimulatorOn para textos y ajustes de PC) y PlayerRig.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class EditorSimulatorToggle : MonoBehaviour
    {
        public GameObject simulator;

        /// <summary>true cuando se juega en el PC con mouse y teclado (lo usa PlayerRig para no atravesar paredes).</summary>
        public static bool SimulatorOn;

        void Awake()
        {
#if UNITY_EDITOR
            bool hmd = false;
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays) if (d.running) hmd = true;
            if (simulator != null) simulator.SetActive(!hmd);
            SimulatorOn = simulator != null && !hmd;
            if (hmd) Debug.Log("[RickMorty] Visor detectado: simulador desactivado.");
            else Debug.Log("[RickMorty] Sin visor: usando XR Interaction Simulator (mouse/teclado).");
#else
            SimulatorOn = false;
            Destroy(gameObject);
#endif
        }
    }
}
