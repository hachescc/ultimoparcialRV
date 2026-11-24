using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RickMorty
{
    [DefaultExecutionOrder(-1000)]
    public class EditorSimulatorToggle : MonoBehaviour
    {
        public GameObject simulator;
        public float waitForHeadsetSeconds = 2f;

        public static bool SimulatorOn;

        void Awake()
        {
            SimulatorOn = false;
#if UNITY_EDITOR
            if (simulator != null) simulator.SetActive(false);
#else
            Destroy(gameObject);
#endif
        }

        IEnumerator Start()
        {
            float until = Time.realtimeSinceStartup + waitForHeadsetSeconds;
            while (Time.realtimeSinceStartup < until)
            {
                if (HeadsetRunning())
                {
                    Debug.Log("[RickMorty] Visor detectado: se juega con las gafas.");
                    yield break;
                }
                yield return null;
            }

            SimulatorOn = true;
            if (simulator != null) simulator.SetActive(true);
            if (PlayerRig.Instance != null) PlayerRig.Instance.UsePcMode();
            Debug.Log("[RickMorty] Sin visor: usando XR Interaction Simulator (mouse/teclado).");

            yield return null;
            yield return null;
            HideByName("InputSelectionWindow");
            HideByName("InputSelectionClosedWindow");
        }

        void Update()
        {
            if (!SimulatorOn || !HeadsetRunning()) return;
            SimulatorOn = false;
            if (simulator != null) simulator.SetActive(false);
            if (PlayerRig.Instance != null) PlayerRig.Instance.UseHeadsetMode();
            Debug.Log("[RickMorty] Visor conectado durante el juego: simulador desactivado.");
        }

        static bool HeadsetRunning()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays) if (d.running) return true;
            if (InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid) return true;
            return XRSettings.isDeviceActive;
        }

        static void HideByName(string objectName)
        {
            var found = GameObject.Find(objectName);
            if (found != null) found.SetActive(false);
        }
    }
}
