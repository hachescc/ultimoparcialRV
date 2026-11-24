using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Rick Orientoso (Caleno): te sigue por la mesa. Rechaza el Token de Fortnite y si le
    /// pones la Camiseta del America se vuelve hiperactivo cantando barras.
    /// Lo usan: ItemFactory (behaviour "creature_caleno"); GameManager no lo borra al terminar.
    /// </summary>
    public class RickCaleno : CreatureMover
    {
        float m_HyperUntil;
        float m_NextLine;
        float m_NextChant;

        protected override void Awake()
        {
            base.Awake();
            speed = 0.25f;
            wanderRadius = 0.4f;
        }

        protected override Vector3? CurrentGoal()
        {
            var rig = PlayerRig.Instance;
            if (Time.time < m_HyperUntil) return base.CurrentGoal();
            if (rig == null) return null;
            var p = rig.FeetPosition;
            p.y = transform.position.y;
            var d = p - transform.position;
            return d.magnitude > 0.6f ? transform.position + d.normalized * 0.5f : (Vector3?)null;
        }

        void OnCollisionEnter(Collision c)
        {
            var other = c.collider.GetComponentInParent<MixableItem>();
            if (other == null || Time.time < m_NextLine) return;
            if (other.itemId == "token_fortnite")
            {
                m_NextLine = Time.time + 6f;
                Say("caleno_token");
            }
            else if (other.itemId == "camiseta_america")
            {
                m_NextLine = Time.time + 6f;
                m_HyperUntil = Time.time + 20f;
                speed = 0.9f;
                wanderRadius = 1f;
                Say("caleno_america");
                GameEvents.RaiseBonus("Rick Caleño feliz", 40);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (Time.time >= m_HyperUntil)
            {
                speed = 0.25f;
                return;
            }
            if (Time.time > m_NextChant)
            {
                m_NextChant = Time.time + 2.5f;
                var line = GameDatabase.GetLine("caleno_chant");
                if (line != null) FX.FloatingText(transform.position + Vector3.up * 0.25f, line.PickText(), new Color(1f, 0.2f, 0.2f), 0.4f, 2f, 0.1f);
                AudioFX.Play("meeseeks", transform.position, 0.3f, 1.4f);
            }
        }

        /// <summary>Dice una linea de dialogues.json (el hablante sale del archivo: Rick Caleno).</summary>
        static void Say(string id)
        {
            if (RickComms.Instance != null) RickComms.Instance.Say(id);
        }
    }
}
