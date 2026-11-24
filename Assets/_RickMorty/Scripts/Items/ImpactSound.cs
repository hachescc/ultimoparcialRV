using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Sonido de impacto segun el material (metalico, carnoso, cristal, madera).
    /// Lo usan: ItemFactory (se lo pone a todo objeto y elige el material).
    /// </summary>
    public class ImpactSound : MonoBehaviour
    {
        /// <summary>Material del objeto, que decide el sonido del golpe.</summary>
        public enum Kind { Wood, Metal, Flesh, Glass }

        public Kind kind = Kind.Wood;
        public float minSpeed = 0.8f;

        float m_Next;

        void OnCollisionEnter(Collision c)
        {
            float v = c.relativeVelocity.magnitude;
            if (v < minSpeed || Time.time < m_Next) return;
            m_Next = Time.time + 0.12f;
            string clip;
            if (kind == Kind.Metal) clip = "impact_metal";
            else if (kind == Kind.Flesh) clip = "impact_flesh";
            else if (kind == Kind.Glass) clip = "impact_glass";
            else clip = "impact_wood";
            AudioFX.Play(clip, c.GetContact(0).point, Mathf.Clamp01(v / 5f), Random.Range(0.9f, 1.1f));
        }
    }
}
