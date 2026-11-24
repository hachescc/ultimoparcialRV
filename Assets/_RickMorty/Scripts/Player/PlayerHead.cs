using UnityEngine;

namespace RickMorty
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerHead : MonoBehaviour
    {
        void Awake()
        {
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
        public float mouthDwell = 0.4f;

        void OnTriggerEnter(Collider other)
        {
            var consumable = other.GetComponentInParent<IHeadInteractable>();
            if (consumable != null)
            {
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

    public interface IHeadInteractable
    {
        void OnTouchHead(PlayerHead head);
    }
}
