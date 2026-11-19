using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Colisionador de la cabeza del jugador. En VR "comer" o "ponerse" algo se detecta
    /// cuando el objeto toca la cabeza (camara). Tambien detecta portales.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerHead : MonoBehaviour
    {
        public static PlayerHead Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.14f;
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            var consumable = other.GetComponentInParent<IHeadInteractable>();
            if (consumable != null)
            {
                consumable.OnTouchHead(this);
                return;
            }
            var portal = other.GetComponentInParent<EndingPortal>();
            if (portal != null) portal.PlayerEntered();
        }
    }

    /// <summary>Objetos que reaccionan al tocar la cabeza (comida, cascos...).</summary>
    public interface IHeadInteractable
    {
        void OnTouchHead(PlayerHead head);
    }
}
