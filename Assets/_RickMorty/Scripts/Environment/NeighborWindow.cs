using UnityEngine;

namespace RickMorty
{
    /// <summary>Ventana del vecino: si le pega un objeto vivo se escuchan gritos de terror desde adentro.</summary>
    public class NeighborWindow : MonoBehaviour
    {
        float m_Next;

        public void Hit(StickyThrowable thing)
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 3f;
            AudioFX.Play("scream", transform.position + transform.forward * -1f, 1f, Random.Range(0.8f, 1.2f));
            FX.FloatingText(transform.position + Vector3.up * 0.8f, "¡¡AAAAAH!!", Color.white, 1.5f, 2f, 0.2f);
            GameEvents.RaiseBonus("Asustaste a los vecinos", 20);
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.relativeVelocity.magnitude > 3f)
                AudioFX.Play("impact_glass", c.GetContact(0).point, 1f);
        }
    }
}
