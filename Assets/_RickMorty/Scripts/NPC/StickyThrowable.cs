using UnityEngine;

namespace RickMorty
{
    public class StickyThrowable : ItemBehaviour
    {
        bool m_Stuck;

        void OnCollisionEnter(Collision c)
        {
            if (m_Stuck || Item.IsHeld) return;
            var window = c.collider.GetComponentInParent<NeighborWindow>();
            if (window == null) return;
            m_Stuck = true;
            Body.isKinematic = true;
            transform.position = c.GetContact(0).point + c.GetContact(0).normal * 0.05f;
            window.Hit(this);
        }

        protected override void OnGrabbed() => m_Stuck = false;

        protected override void OnReleased() => Body.isKinematic = false;
    }
}
