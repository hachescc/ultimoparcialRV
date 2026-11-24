using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    public class PlayAreaBoundary : MonoBehaviour
    {
        public List<Bounds> allowedZones = new List<Bounds>();
        public Transform respawnPoint;
        public float warningDistance = 1.5f;
        public int penaltyPoints = 100;
        public float penaltySeconds = 30f;

        float m_Cooldown;
        float m_NextWarning;

        public static PlayAreaBoundary Instance { get; private set; }
        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public Bounds safeInterior;

        public bool IsInside(Vector3 p)
        {
            foreach (var b in allowedZones)
            {
                var bb = b;
                bb.extents = new Vector3(bb.extents.x, 50f, bb.extents.z);
                if (bb.Contains(p)) return true;
            }
            return false;
        }

        bool NearEdge(Vector3 p)
        {
            if (safeInterior.size != Vector3.zero && safeInterior.Contains(p)) return false;
            var d = warningDistance;
            return !IsInside(p + new Vector3(d, 0, 0)) || !IsInside(p + new Vector3(-d, 0, 0)) ||
                   !IsInside(p + new Vector3(0, 0, d)) || !IsInside(p + new Vector3(0, 0, -d));
        }

        void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || allowedZones.Count == 0) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.IsFinished) return;
            if (Time.timeScale == 0f) return;

            m_Cooldown -= Time.deltaTime;
            var p = rig.HeadPosition;

            if (!IsInside(p))
            {
                if (m_Cooldown > 0) return;
                m_Cooldown = 3f;
                Snipe(rig);
            }
            else if (NearEdge(p) && Time.time > m_NextWarning)
            {
                m_NextWarning = Time.time + 6f;
                RickComms.Instance?.Say("fbi_warning");
                WristWatch.Instance?.Notify("¡CINTA DEL FBI! No salgas del perímetro", UIKit.Danger);
                rig.HapticPattern(Hand.Both, 0.6f, 3, 0.08f, 0.08f);
            }
        }

        void Snipe(PlayerRig rig)
        {
            AudioFX.Play("sniper", rig.HeadPosition + Vector3.up * 3f, 1f);
            ScreenFX.Instance?.Flash(new Color(0.8f, 0, 0), 0.4f, 1.2f);
            ScreenFX.Instance?.ShowCenterText(UIKit.TitleTag("¡FRANCOTIRADOR DEL FBI!") + $"\n<size=60%>-{penaltyPoints} pts y -{penaltySeconds:0} s</size>", Color.white, 2.6f);
            rig.Haptic(Hand.Both, 1f, 0.4f);
            GameEvents.RaisePenalty("Saliste de la cinta del FBI", penaltyPoints);
            GameTimer.Instance?.AddTime(-penaltySeconds);
            RickComms.Instance?.Say("fbi_shot", true);
            if (respawnPoint != null) rig.TeleportTo(respawnPoint.position, respawnPoint.forward);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.8f, 0, 0.5f);
            foreach (var b in allowedZones) Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
