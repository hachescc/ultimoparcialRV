using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace RickMorty
{
    [DefaultExecutionOrder(-1000)]
    public class EditorSimulatorToggle : MonoBehaviour
    {
        public GameObject simulator;

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

        System.Collections.IEnumerator Start()
        {
            yield return null;
            yield return null;
            HideByName("InputSelectionWindow");
            HideByName("InputSelectionClosedWindow");
        }

        static void HideByName(string objectName)
        {
            var found = GameObject.Find(objectName);
            if (found != null) found.SetActive(false);
        }
    }
}
