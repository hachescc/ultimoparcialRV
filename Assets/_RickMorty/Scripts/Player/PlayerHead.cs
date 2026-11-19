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

        IHeadInteractable m_Candidate;
        float m_CandidateSince;
        [Tooltip("Segundos que hay que mantener la comida/casco en la cara para usarlo")]
        public float mouthDwell = 0.4f;

        void OnTriggerEnter(Collider other)
        {
            var consumable = other.GetComponentInParent<IHeadInteractable>();
            if (consumable != null)
            {
                // No se consume al instante: acercarse el Aguardiente para mirarlo se lo tomaba (y es ingrediente de la receta final).
                if (!ReferenceEquals(consumable, m_Candidate)) { m_Candidate = consumable; m_CandidateSince = Time.time; }
                return;
            }
            var portal = other.GetComponentInParent<EndingPortal>();
            if (portal != null) portal.PlayerEntered();
        }

        void OnTriggerStay(Collider other)
        {
            if (m_Candidate == null || Time.time - m_CandidateSince < mouthDwell) return;
            var c = other.GetComponentInParent<IHeadInteractable>();
            if (c == null || !ReferenceEquals(c, m_Candidate)) return;
            m_Candidate = null;
            c.OnTouchHead(this);
        }

        void OnTriggerExit(Collider other)
        {
            var c = other.GetComponentInParent<IHeadInteractable>();
            if (c != null && ReferenceEquals(c, m_Candidate)) m_Candidate = null;
        }
    }

    /// <summary>Objetos que reaccionan al tocar la cabeza (comida, cascos...).</summary>
    public interface IHeadInteractable
    {
        void OnTouchHead(PlayerHead head);
    }
}
