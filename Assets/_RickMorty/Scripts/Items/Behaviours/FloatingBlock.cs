using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Bloque de Minecraft (y Token de Fortnite): ignora la gravedad. Donde lo sueltas, ahi se
    /// queda (kinematic). Sirve como plataforma flotante o para hacer escaleras ridiculas.
    /// El token ademas gira sobre su eje y flota arriba y abajo.
    /// </summary>
    public class FloatingBlock : ItemBehaviour
    {
        public bool spin;
        Transform m_Visual;
        Vector3 m_VisualBase;

        protected override void Start()
        {
            base.Start();
            m_Visual = transform.Find("Visual");
            if (m_Visual != null) m_VisualBase = m_Visual.localPosition;
            Freeze();
        }

        protected override void OnReleased() => Freeze();

        void Freeze()
        {
            if (Body == null || Item.IsHeld) return;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            AudioFX.Play("impact_wood", transform.position, 0.4f, 0.6f);
        }

        void Update()
        {
            if (!spin || m_Visual == null) return;
            m_Visual.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            m_Visual.localPosition = m_VisualBase + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.02f;
        }
    }
}
