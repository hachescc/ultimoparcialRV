using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Armas de corte (Katana, Espada de Zord): un golpe con velocidad suficiente mata
    /// criaturas (one-hit kill). La Espada de Zord deja marcas de quemadura.
    /// </summary>
    public class MeleeWeapon : ItemBehaviour
    {
        public float minSpeed = 1.2f;
        public bool burnMarks;

        float m_NextMark;

        void OnCollisionEnter(Collision c)
        {
            if (!Item.IsHeld) return;
            float speed = c.relativeVelocity.magnitude;
            if (speed < minSpeed) return;
            var creature = c.collider.GetComponentInParent<Creature>();
            if (creature != null)
            {
                creature.Kill(DamageType.Melee);
                Item.Haptic(0.9f, 0.15f);
                return;
            }
            Item.Haptic(Mathf.Clamp01(speed / 4f), 0.06f);
            if (burnMarks && c.rigidbody == null && Time.time > m_NextMark)
            {
                m_NextMark = Time.time + 0.15f;
                var cp = c.GetContact(0);
                FX.Decal(cp.point, cp.normal, new Color(0.08f, 0.05f, 0.02f, 0.85f), 0.08f, 40f);
                FX.Burst(cp.point, new Color(1f, 0.5f, 0.2f), 8, 1.5f, 0.015f, 0.3f, 1f, true);
            }
        }
    }
}
