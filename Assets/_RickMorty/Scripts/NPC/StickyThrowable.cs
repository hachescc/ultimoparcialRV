using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Pepino Mutante / Mini-Cronenberg: objetos vivos arrojadizos. Si los lanzas a las ventanas
    /// de la casa del vecino se pegan y se escuchan los gritos de terror desde adentro.
    /// Lo usan: ItemFactory (behaviour "creature_throwable") y NeighborWindow.
    /// </summary>
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

        // XRGrabInteractable restaura el isKinematic=true de cuando estaba pegado: sin esto quedaba congelado en el aire
        protected override void OnReleased() => Body.isKinematic = false;
    }
}
