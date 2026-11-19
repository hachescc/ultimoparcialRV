using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Caja de Meeseeks. Gatillo: aparece un Meeseeks.
    /// Original: limpia el garaje llevando basura al agujero negro ("¡Oooh, can do!").
    /// Contrabando: se roba un ingrediente al azar, tose y explota.
    /// </summary>
    public class MeeseeksBox : ItemBehaviour
    {
        public bool contraband;
        GameObject m_Active;
        float m_Next;

        protected override void OnActivate()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 1f;
            Item.Haptic(0.5f, 0.1f);
            AudioFX.Play("button", transform.position, 0.8f);
            if (m_Active != null)
            {
                RickComms.Instance?.SayText("¡Ya hay un Meeseeks trabajando! ¡La existencia es dolor!", "Meeseeks");
                return;
            }
            var pos = transform.position + Vector3.up * 0.3f;
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                var toPlayer = rig.FeetPosition - transform.position; toPlayer.y = 0;
                pos = transform.position + Vector3.Cross(Vector3.up, toPlayer.normalized) * 0.6f;
                pos.y = Mathf.Max(0.05f, transform.position.y - 0.5f);
                if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                    pos = hit.point;
            }
            m_Active = MeeseeksNPC.Spawn(pos, contraband);
        }
    }
}
