using UnityEngine;
using UnityEngine.InputSystem;

namespace RickMorty
{
    /// <summary>
    /// Atajos de prueba (solo Editor / Development Build):
    /// F1 saltar tutorial · F2 kit de ensamblaje final · F3 +60 s · F4 evento inesperado ·
    /// F5 spawnear objeto aleatorio · F6 terminar (victoria) · F7 forzar game over.
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        void Update()
        {
            if (!(Application.isEditor || Debug.isDebugBuild)) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (kb.f1Key.wasPressedThisFrame) gm.DebugSkipTutorial();
            if (kb.f2Key.wasPressedThisFrame) gm.DebugSpawnFinalKit();
            if (kb.f3Key.wasPressedThisFrame) GameTimer.Instance?.AddTime(60f);
            if (kb.f4Key.wasPressedThisFrame && gm.eventDirector != null)
            {
                int r = Random.Range(0, 3);
                if (r == 0) gm.eventDirector.SteveInvasion();
                else if (r == 1) gm.eventDirector.ContrabandRaid();
                else gm.eventDirector.Quake();
            }
            if (kb.f5Key.wasPressedThisFrame)
            {
                var all = new System.Collections.Generic.List<ItemDef>(GameDatabase.Discoverables);
                var def = all[Random.Range(0, all.Count)];
                var p = gm.mixingBowl != null && gm.mixingBowl.outputPoint != null ? gm.mixingBowl.outputPoint.position : Vector3.up;
                ItemFactory.Spawn(def.id, p + Vector3.up * 0.3f, Quaternion.identity);
                ScoreManager.Instance?.RegisterDiscovery(def.id);
            }
            if (kb.f6Key.wasPressedThisFrame) gm.EnterFinalPortal();
            if (kb.f7Key.wasPressedThisFrame) gm.DebugGameOver();
        }
    }
}
