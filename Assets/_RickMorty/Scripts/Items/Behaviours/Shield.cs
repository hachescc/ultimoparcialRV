using UnityEngine;

namespace RickMorty
{
    public class Shield : ItemBehaviour
    {
        void OnCollisionEnter(Collision c)
        {
            var steve = c.collider.GetComponentInParent<FakeUncleSteve>();
            if (steve == null) return;
            steve.KnockBack(transform.position);
            Item.Haptic(0.7f, 0.15f);
            AudioFX.Play("impact_flesh", c.GetContact(0).point, 1f, 0.8f);
        }
    }
}
