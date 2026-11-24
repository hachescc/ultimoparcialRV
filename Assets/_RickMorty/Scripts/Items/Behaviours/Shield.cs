using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Dakimakura / Peluche de Teto: escudos perfectos e irrompibles (repelen al Tio Steve).
    /// Lo usan: ItemFactory (behaviour "shield") y FakeUncleSteve (no abraza si choca con un escudo).
    /// </summary>
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
