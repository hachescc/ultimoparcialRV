using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Movimiento simple de criaturas sobre el suelo (deambular o perseguir un objetivo)
    /// usando la velocidad del Rigidbody. No se mueve mientras el jugador la sostiene.
    /// Lo usan: ItemFactory (criaturas que deambulan) y sus hijos: FakeUncleSteve, MeeseeksNPC y RickCaleno.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CreatureMover : MonoBehaviour
    {
        public float speed = 0.4f;
        public float turnSpeed = 240f;
        public float wanderRadius = 1.5f;
        public bool hop = true;

        protected Rigidbody m_Body;
        protected MixableItem m_Item;
        Vector3 m_Home;
        Vector3 m_WanderTarget;
        float m_NextWander;
        float m_HopPhase;
        Transform m_Visual;

        protected virtual void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Item = GetComponent<MixableItem>();
            m_Home = transform.position;
            m_WanderTarget = m_Home;
            m_Visual = transform.Find("Visual");
            m_Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        protected bool IsHeld => m_Item != null && m_Item.IsHeld;

        protected bool Grounded()
        {
            var col = GetComponent<Collider>();
            float h = col != null ? col.bounds.extents.y + 0.05f : 0.2f;
            return Physics.Raycast(transform.position, Vector3.down, h + 0.05f, ~0, QueryTriggerInteraction.Ignore);
        }

        protected virtual Vector3? CurrentGoal()
        {
            if (Time.time > m_NextWander || (FlatDelta(m_WanderTarget).sqrMagnitude < 0.04f))
            {
                m_NextWander = Time.time + Random.Range(2f, 5f);
                var r = Random.insideUnitCircle * wanderRadius;
                m_WanderTarget = new Vector3(transform.position.x + r.x, transform.position.y, transform.position.z + r.y);
            }
            return m_WanderTarget;
        }

        protected Vector3 FlatDelta(Vector3 goal)
        {
            var d = goal - transform.position;
            d.y = 0;
            return d;
        }

        protected virtual void FixedUpdate()
        {
            if (IsHeld || m_Body.isKinematic) return;
            if (!Grounded()) return;
            var goal = CurrentGoal();
            if (!goal.HasValue) return;
            var d = FlatDelta(goal.Value);
            if (d.sqrMagnitude < 0.01f)
            {
                var v0 = m_Body.linearVelocity; v0.x = 0; v0.z = 0; m_Body.linearVelocity = v0;
                return;
            }
            var dir = d.normalized;
            var v = m_Body.linearVelocity;
            v.x = dir.x * speed;
            v.z = dir.z * speed;
            m_Body.linearVelocity = v;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            m_Body.MoveRotation(Quaternion.RotateTowards(m_Body.rotation, rot, turnSpeed * Time.fixedDeltaTime));
        }

        protected virtual void Update()
        {
            if (!hop || m_Visual == null) return;
            if (IsHeld) { m_Visual.localPosition = Vector3.zero; return; }
            m_HopPhase += Time.deltaTime * speed * 18f;
            m_Visual.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(m_HopPhase)) * 0.02f;
        }
    }
}
