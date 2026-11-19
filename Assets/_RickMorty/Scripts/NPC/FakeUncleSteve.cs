using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Falso Tio Steve (parasito de memoria): camina lentamente hacia ti pidiendo un abrazo.
    /// Si te toca: la pantalla se llena de corazones rosas 5 s (falsos recuerdos) y penalizacion.
    /// Hay que destruirlo con un arma antes, o bloquearlo con un escudo.
    /// </summary>
    public class FakeUncleSteve : CreatureMover
    {
        public float hugDistance = 0.55f;
        public float blindSeconds = 5f;
        public int penalty = 75;
        float m_NextVoice;
        float m_StunUntil;

        protected override void Awake()
        {
            base.Awake();
            speed = 0.55f;
            m_NextVoice = Time.time + 1f;
        }

        protected override Vector3? CurrentGoal()
        {
            if (Time.time < m_StunUntil) return null;
            var rig = PlayerRig.Instance;
            return rig != null ? rig.FeetPosition : (Vector3?)null;
        }

        public void KnockBack(Vector3 from)
        {
            m_StunUntil = Time.time + 2f;
            var dir = transform.position - from; dir.y = 0;
            m_Body.AddForce(dir.normalized * 3f + Vector3.up * 2f, ForceMode.VelocityChange);
        }

        protected override void Update()
        {
            base.Update();
            var rig = PlayerRig.Instance;
            if (rig == null || IsHeld || Time.timeScale == 0f) return;   // en pausa no abraza
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Ending || gm.State == GameState.GameOver)) return;
            var creature = GetComponent<Creature>();
            if (creature != null && creature.IsDead) return;

            if (Time.time > m_NextVoice)
            {
                m_NextVoice = Time.time + Random.Range(5f, 9f);
                var line = GameDatabase.GetLine("steve_hug");
                if (line != null) FX.FloatingText(transform.position + Vector3.up * 0.4f, line.PickText(), new Color(1f, 0.7f, 0.85f), 0.5f, 2.5f, 0.05f);
                AudioFX.Play("meeseeks", transform.position, 0.4f, 0.6f);
            }

            var d = rig.FeetPosition - transform.position; d.y = 0;
            if (d.magnitude < hugDistance && Time.time > m_StunUntil)
                Hug();
        }

        void Hug()
        {
            ScreenFX.Instance?.Blind(blindSeconds);
            PlayerRig.Instance?.Haptic(Hand.Both, 0.6f, 1f);
            GameEvents.RaisePenalty("El Falso Tío Steve te abrazó", penalty);
            RickComms.Instance?.Say("steve_hugged");
            FX.Burst(transform.position, new Color(1f, 0.6f, 0.85f), 50, 2f, 0.05f, 1f, -0.2f);
            var item = GetComponent<MixableItem>();
            if (item != null) item.Unavailable = true;
            Destroy(gameObject);
        }
    }
}
