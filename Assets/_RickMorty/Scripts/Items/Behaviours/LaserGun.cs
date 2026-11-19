using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>Pistola laser: dispara un rayo que rebota hasta 3 veces en las paredes.</summary>
    public class LaserGun : ItemBehaviour
    {
        public int maxBounces = 3;
        public float range = 25f;
        public float cooldown = 0.35f;
        public Color beamColor = new Color(0.2f, 1f, 0.9f);

        LineRenderer m_Line;
        float m_Next;

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Beam");
            go.transform.SetParent(transform, false);
            m_Line = go.AddComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.widthMultiplier = 0.012f;
            m_Line.sharedMaterial = MatLib.Particles(beamColor, true);
            m_Line.startColor = m_Line.endColor = beamColor;
            m_Line.enabled = false;
        }

        protected override void OnActivate()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + cooldown;
            Fire();
        }

        void Fire()
        {
            var points = new List<Vector3>();
            var origin = Muzzle(0.18f);
            var dir = transform.forward;
            points.Add(origin);
            var ignore = GetComponentsInChildren<Collider>();

            for (int i = 0; i <= maxBounces; i++)
            {
                if (!Raycast(origin, dir, ignore, out var hit))
                {
                    points.Add(origin + dir * range);
                    break;
                }
                points.Add(hit.point);
                bool isStatic = hit.rigidbody == null;
                if (!isStatic || hit.collider.GetComponentInParent<Creature>() != null)
                {
                    ApplyProjectileHit(hit, dir, 2f, true);
                    FX.Burst(hit.point, beamColor, 12, 1.5f, 0.02f, 0.3f, 0f, true);
                    break;
                }
                FX.Burst(hit.point, beamColor, 6, 1f, 0.015f, 0.25f, 0f, true);
                FX.Decal(hit.point, hit.normal, new Color(0.05f, 0.05f, 0.05f, 0.8f), 0.04f, 20f);
                dir = Vector3.Reflect(dir, hit.normal);
                origin = hit.point + hit.normal * 0.005f;
            }

            AudioFX.Play("laser", transform.position, 0.8f, Random.Range(0.95f, 1.05f));
            Item.Haptic(0.6f, 0.08f);
            StopAllCoroutines();
            StartCoroutine(CoShowBeam(points));
        }

        bool Raycast(Vector3 origin, Vector3 dir, Collider[] ignore, out RaycastHit best)
        {
            best = default;
            var hits = Physics.RaycastAll(origin, dir, range, ~0, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            foreach (var h in hits)
            {
                bool skip = false;
                foreach (var c in ignore) if (h.collider == c) { skip = true; break; }
                if (skip || h.collider.GetComponentInParent<PlayerHead>() != null) continue;
                if (h.distance < bestDist) { bestDist = h.distance; best = h; found = true; }
            }
            return found;
        }

        IEnumerator CoShowBeam(List<Vector3> points)
        {
            m_Line.positionCount = points.Count;
            m_Line.SetPositions(points.ToArray());
            m_Line.enabled = true;
            float t = 0;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                m_Line.widthMultiplier = Mathf.Lerp(0.015f, 0.002f, t / 0.15f);
                yield return null;
            }
            m_Line.enabled = false;
        }
    }
}
