using UnityEngine;

namespace RickMorty
{
    /// <summary>Sonido de impacto segun el material (metalico, carnoso, cristal, madera).</summary>
    public class ImpactSound : MonoBehaviour
    {
        public enum Kind { Wood, Metal, Flesh, Glass }

        public Kind kind = Kind.Wood;
        public float minSpeed = 0.8f;

        float m_Next;

        void OnCollisionEnter(Collision c)
        {
            float v = c.relativeVelocity.magnitude;
            if (v < minSpeed || Time.time < m_Next) return;
            m_Next = Time.time + 0.12f;
            string clip = kind == Kind.Metal ? "impact_metal" : kind == Kind.Flesh ? "impact_flesh" : kind == Kind.Glass ? "impact_glass" : "impact_wood";
            AudioFX.Play(clip, c.GetContact(0).point, Mathf.Clamp01(v / 5f), Random.Range(0.9f, 1.1f));
        }
    }
}
