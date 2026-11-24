using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class Burnable : ItemBehaviour
    {
        public float duration = 30f;
        static Coroutine s_GravityCo;
        static MonoBehaviour s_Runner;
        static Vector3 s_DefaultGravity = new Vector3(0, -9.81f, 0);

        protected override void Start()
        {
            base.Start();
            FX.Attach(transform, new Color(0.7f, 0.3f, 1f), 4f, 0.03f, 0.05f, -0.03f, 0.04f);
        }

        public void Burn()
        {
            var pos = transform.position;
            FX.Smoke(pos, new Color(0.6f, 0.2f, 0.9f, 0.6f), 6f, 1.5f, 1.2f, 50f);
            AudioFX.Play("fuuump", pos, 1f, 1.3f);
            ScreenFX.Instance?.PurpleHaze(duration);
            RickComms.Instance?.Say("weed_burn");
            Talker.SayAll("toallin_weed");
            GameEvents.RaiseUnexpectedEvent("Gravedad reducida");

            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (s_GravityCo != null && s_Runner != null) s_Runner.StopCoroutine(s_GravityCo);
                s_Runner = gm;
                s_GravityCo = gm.StartCoroutine(CoGravity(duration));
            }
            Item.Consume(0f, false);
        }

        static IEnumerator CoGravity(float seconds)
        {
            Physics.gravity = s_DefaultGravity * 0.5f;
            WristWatch.Instance?.Notify("GRAVEDAD AL 50%", new Color(0.8f, 0.5f, 1f), 3f);
            yield return new WaitForSeconds(seconds);
            Physics.gravity = s_DefaultGravity;
            s_GravityCo = null;
        }

        public static void ResetGravity() => Physics.gravity = s_DefaultGravity;
    }
}
