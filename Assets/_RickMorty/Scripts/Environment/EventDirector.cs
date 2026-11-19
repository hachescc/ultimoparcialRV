using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Director de eventos inesperados durante el juego libre: apagon del FBI, invasion del
    /// Falso Tio Steve, Meeseeks de contrabando que roba ingredientes y temblor interdimensional.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        public float firstEventDelay = 70f;
        public Vector2 interval = new Vector2(80f, 130f);
        public List<FlickerLight> lights = new List<FlickerLight>();
        public Light sun;
        public Transform steveSpawn;
        public Transform meeseeksSpawn;

        bool m_Running;
        int m_Last = -1;

        public void Begin()
        {
            if (m_Running) return;
            m_Running = true;
            StartCoroutine(CoLoop());
        }

        public void Stop()
        {
            m_Running = false;
            StopAllCoroutines();
            foreach (var l in lights) if (l != null) l.SetBlackout(false);
        }

        IEnumerator CoLoop()
        {
            yield return new WaitForSeconds(firstEventDelay);
            while (m_Running)
            {
                int pick;
                do pick = Random.Range(0, 4); while (pick == m_Last);
                m_Last = pick;
                switch (pick)
                {
                    case 0: yield return CoBlackout(); break;
                    case 1: SteveInvasion(); break;
                    case 2: ContrabandRaid(); break;
                    default: Quake(); break;
                }
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
        }

        public IEnumerator CoBlackout()
        {
            GameEvents.RaiseUnexpectedEvent("Apagon");
            RickComms.Instance?.Say("event_blackout");
            AudioFX.Play("impact_metal", transform.position + Vector3.up * 2.8f, 1f, 0.4f);
            WristWatch.Instance?.Notify("¡APAGÓN! El FBI cortó la luz", UIKit.Danger, 4f);
            foreach (var l in lights) if (l != null) l.SetBlackout(true);
            yield return new WaitForSeconds(25f);
            foreach (var l in lights) if (l != null) l.SetBlackout(false);
            AudioFX.Play("button", transform.position + Vector3.up * 2.8f, 1f, 0.5f);
        }

        public void SteveInvasion()
        {
            GameEvents.RaiseUnexpectedEvent("Invasion del Tío Steve");
            RickComms.Instance?.Say("event_steve");
            WristWatch.Instance?.Notify("¡PARÁSITO DETECTADO! No dejes que te abrace", UIKit.Danger, 4f);
            var pos = steveSpawn != null ? steveSpawn.position : transform.position;
            FX.Portal(pos + Vector3.up * 0.4f, Quaternion.identity, 0.4f, 2f);
            ItemFactory.Spawn("tio_steve", pos + Vector3.up * 0.3f, Quaternion.identity);
        }

        public void ContrabandRaid()
        {
            GameEvents.RaiseUnexpectedEvent("Meeseeks de contrabando");
            var pos = meeseeksSpawn != null ? meeseeksSpawn.position : transform.position;
            FX.Portal(pos + Vector3.up * 0.4f, Quaternion.identity, 0.4f, 2f);
            MeeseeksNPC.Spawn(pos, true);
            WristWatch.Instance?.Notify("¡UN MEESEEKS PIRATA VA POR TUS INGREDIENTES!", UIKit.Danger, 4f);
        }

        public void Quake()
        {
            GameEvents.RaiseUnexpectedEvent("Temblor interdimensional");
            RickComms.Instance?.Say("event_quake");
            AudioFX.Play("explosion", transform.position, 0.6f, 0.4f);
            PlayerRig.Instance?.HapticPattern(Hand.Both, 0.7f, 8, 0.1f, 0.05f);
            foreach (var m in MixableItem.All)
            {
                if (m == null || m.IsHeld || m.InSocket || m.Unavailable || m.Body.isKinematic) continue;
                m.Body.AddForce(Vector3.up * Random.Range(1f, 2.5f) + Random.insideUnitSphere * 0.8f, ForceMode.VelocityChange);
            }
        }
    }
}
