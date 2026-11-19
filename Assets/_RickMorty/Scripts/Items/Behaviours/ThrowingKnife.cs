using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Cuchillos (CS:GO, Kunai): si se lanzan con fuerza se clavan en la madera o el corcho
    /// (FixedJoint temporal / kinematic). El de CS:GO gira en el dedo con el gatillo sostenido.
    /// </summary>
    public class ThrowingKnife : ItemBehaviour
    {
        public float stickSpeed = 2.5f;
        public bool canSpin;

        FixedJoint m_Joint;
        bool m_StuckKinematic;
        bool m_Spinning;
        Transform m_Visual;

        protected override void Start()
        {
            base.Start();
            m_Visual = transform.Find("Visual");
        }

        protected override void OnActivate() { if (canSpin) m_Spinning = true; }
        protected override void OnDeactivate() { m_Spinning = false; if (m_Visual) m_Visual.localRotation = Quaternion.identity; }

        protected override void OnGrabbed()
        {
            if (m_Joint != null) { Destroy(m_Joint); m_Joint = null; }
        }

        protected override void OnReleased()
        {
            m_Spinning = false;
            if (m_Visual) m_Visual.localRotation = Quaternion.identity;
            if (m_StuckKinematic)
            {
                // XRGrabInteractable restaura el estado kinematic de cuando se agarro; lo corregimos
                m_StuckKinematic = false;
                Body.isKinematic = false;
            }
        }

        void Update()
        {
            if (m_Spinning && m_Visual != null) m_Visual.Rotate(0, 0, 900f * Time.deltaTime, Space.Self);
        }

        void OnCollisionEnter(Collision c)
        {
            if (Item.IsHeld || m_Joint != null || m_StuckKinematic) return;
            if (c.relativeVelocity.magnitude < stickSpeed) return;

            var creature = c.collider.GetComponentInParent<Creature>();
            if (creature != null) { creature.Kill(DamageType.Melee); return; }

            var surface = c.collider.GetComponentInParent<SurfaceType>();
            if (surface == null || (surface.kind != SurfaceKind.Wood && surface.kind != SurfaceKind.Cork)) return;

            var cp = c.GetContact(0);
            AudioFX.Play("impact_wood", cp.point, 1f, 1.3f);
            if (surface.kind == SurfaceKind.Cork) GameEvents.RaiseBonus("Diana en el tablero de Rick", 25);

            if (c.rigidbody != null)
            {
                m_Joint = gameObject.AddComponent<FixedJoint>();
                m_Joint.connectedBody = c.rigidbody;
            }
            else
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
                Body.isKinematic = true;
                m_StuckKinematic = true;
            }
        }
    }
}
