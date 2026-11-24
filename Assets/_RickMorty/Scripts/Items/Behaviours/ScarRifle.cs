using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// SCAR del Fortnite: rafaga rapida con dispersion horrible y numeros de dano amarillos.
    /// Lo usan: ItemFactory (behaviour "scar").
    /// </summary>
    public class ScarRifle : ItemBehaviour
    {
        public float fireRate = 10f;
        public float spreadDegrees = 8f;
        public float range = 30f;

        bool m_Firing;
        float m_Next;
        LineRenderer m_Tracer;
        static readonly int[] s_Damage = { 9, 22, 9, 18, 33, 9, 27, 200 };

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            m_Tracer = go.AddComponent<LineRenderer>();
            m_Tracer.widthMultiplier = 0.006f;
            m_Tracer.sharedMaterial = MatLib.Particles(new Color(1f, 0.85f, 0.3f), true);
            m_Tracer.enabled = false;
        }

        protected override void OnActivate() => m_Firing = true;
        protected override void OnDeactivate() => m_Firing = false;
        protected override void OnReleased() => m_Firing = false;

        void Update()
        {
            if (!m_Firing || Time.time < m_Next) return;
            m_Next = Time.time + 1f / fireRate;
            Shoot();
        }

        void Shoot()
        {
            var origin = Muzzle(0.32f);
            var dir = Quaternion.Euler(Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0) * transform.forward;
            AudioFX.Play("shot", origin, 0.7f, Random.Range(0.9f, 1.15f));
            Item.Haptic(0.45f, 0.04f);
            Vector3 end = origin + dir * range;
            if (Physics.Raycast(origin, dir, out var hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                ApplyProjectileHit(hit, dir, 1.5f, false);
                var dmg = s_Damage[Random.Range(0, s_Damage.Length)];
                FX.FloatingText(hit.point + hit.normal * 0.05f, dmg.ToString(), dmg >= 100 ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.9f, 0.1f), 0.9f, 1f, 0.6f);
                FX.Burst(hit.point, new Color(1f, 0.8f, 0.3f), 5, 1.2f, 0.015f, 0.2f, 1f, true);
            }
            StopAllCoroutines();
            StartCoroutine(CoTracer(origin, end));
        }

        IEnumerator CoTracer(Vector3 a, Vector3 b)
        {
            m_Tracer.positionCount = 2;
            m_Tracer.SetPosition(0, a);
            m_Tracer.SetPosition(1, b);
            m_Tracer.enabled = true;
            yield return new WaitForSeconds(0.04f);
            m_Tracer.enabled = false;
        }
    }
}
