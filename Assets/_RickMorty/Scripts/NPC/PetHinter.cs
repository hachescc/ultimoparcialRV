using UnityEngine;

namespace RickMorty
{
    public class PetHinter : ItemBehaviour
    {
        float m_Next;

        protected override void Start()
        {
            base.Start();
            GameEvents.MixFailed += OnMixFailed;
        }

        void OnDestroy() => GameEvents.MixFailed -= OnMixFailed;

        void OnMixFailed(string _)
        {
            var rig = PlayerRig.Instance;
            if (rig != null && (rig.HeadPosition - transform.position).sqrMagnitude < 9f)
                Say("pantalones_fail");
        }

        protected override void OnGrabbed()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 15f;
            var gm = GameManager.Instance;
            var hint = gm != null ? gm.CurrentHintText() : null;
            if (!string.IsNullOrEmpty(hint))
                RickComms.Instance?.SayText("¡Oooh-wiii! Psst, Morty... " + hint, "Sr. Pantalones");
            else
                Say("pantalones_idle");
        }

        static void Say(string id)
        {
            if (RickComms.Instance != null) RickComms.Instance.Say(id);
        }
    }
}
