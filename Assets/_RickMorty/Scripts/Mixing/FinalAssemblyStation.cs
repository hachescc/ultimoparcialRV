using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class FinalAssemblyStation : MonoBehaviour
    {
        public IngredientSocket socket;
        public float zoneRadius = 0.35f;

        public bool Done { get; private set; }
        bool m_Running;
        bool m_HintGiven;
        MixableItem m_RealGun;
        float m_GunLostTime;

        public static FinalAssemblyStation Instance { get; private set; }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ReturnGun(MixableItem gun)
        {
            if (gun == null) return;
            var st = Instance;
            var home = st != null && st.socket != null ? st.socket.transform.position + Vector3.up * 0.35f : Vector3.up * 1.3f;
            gun.ForceRelease();
            FX.Portal(gun.transform.position, Quaternion.identity, 0.15f, 0.8f);
            gun.Body.linearVelocity = Vector3.zero;
            gun.Body.angularVelocity = Vector3.zero;
            gun.Body.isKinematic = true;
            gun.transform.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.right));
            gun.Body.position = home;
            FX.Portal(home, Quaternion.identity, 0.15f, 0.8f);
            RickComms.Instance?.SayText("¡Morty! ¡Esa pistola es lo único que nos saca de aquí! Te la devolví al soporte.");
        }

        void Update()
        {
            var gm0 = GameManager.Instance;
            bool ended = gm0 != null && gm0.IsFinished;
            if (m_RealGun != null && !m_RealGun.IsHeld && !ended)
            {
                var p = m_RealGun.transform.position;
                if (p.y < -3f || (p - transform.position).sqrMagnitude > 45f * 45f) ReturnGun(m_RealGun);
                else
                {
                    var area = PlayAreaBoundary.Instance;
                    bool lost = (p.y > 2.3f || (area != null && !area.IsInside(p)))
                        && m_RealGun.Body != null && m_RealGun.Body.linearVelocity.sqrMagnitude < 0.02f && !m_RealGun.Body.isKinematic;
                    m_GunLostTime = lost ? m_GunLostTime + Time.deltaTime : 0f;
                    if (m_GunLostTime > 4f) { m_GunLostTime = 0f; ReturnGun(m_RealGun); }
                }
            }
            if (Done || m_Running || socket == null) return;
            var gmState = GameManager.Instance != null ? GameManager.Instance.State : GameState.FreePlay;
            if (gmState != GameState.FreePlay) return;
            var gun = socket.HeldItem;
            if (gun == null || gun.itemId != "portal_carton") return;

            if (!m_HintGiven)
            {
                m_HintGiven = true;
                RickComms.Instance?.Say("final_socket_hint");
            }

            MixableItem crystals = null, liquor = null;
            foreach (var m in MixableItem.All)
            {
                if (m == null || m.Unavailable) continue;
                if ((m.transform.position - socket.transform.position).sqrMagnitude > zoneRadius * zoneRadius) continue;
                if (m.itemId == "kalaxianos") crystals = m;
                else if (m.itemId == "aguardiente") liquor = m;
            }
            if (crystals != null && liquor != null) StartCoroutine(CoTransmute(gun, crystals, liquor));
        }

        IEnumerator CoTransmute(MixableItem gun, MixableItem crystals, MixableItem liquor)
        {
            m_Running = true;
            var gm = GameManager.Instance;
            gm?.StopMusic();
            var pos = socket.transform.position;

            liquor.Consume(0f, false);
            crystals.Consume(0.5f, false);
            socket.socketActive = false;
            gun.Unavailable = true;
            gun.ForceRelease();
            if (gun.Grab != null) gun.Grab.enabled = false;
            yield return null;
            gun.Body.isKinematic = true;

            var sparks = FX.Attach(gun.transform, new Color(0.4f, 1f, 0.3f), 80f, 0.03f, 1.5f, 0.5f, 0.1f);
            AudioFX.Play("zap", pos, 1f);
            PlayerRig.Instance?.HapticPattern(Hand.Both, 0.5f, 10, 0.08f, 0.05f);

            var start = gun.transform.position;
            float t = 0;
            while (t < 2f)
            {
                t += Time.deltaTime;
                gun.transform.position = start + Vector3.up * Mathf.SmoothStep(0, 0.35f, t / 2f) + Random.insideUnitSphere * 0.01f * t;
                gun.transform.Rotate(0, 180f * Time.deltaTime * t, 0, Space.World);
                yield return null;
            }

            ScreenFX.Instance?.Flash(Color.white, 0.6f, 2f);
            AudioFX.Play("explosion", pos, 0.6f, 1.5f);
            FX.FlashLight(pos, new Color(0.5f, 1f, 0.5f), 15f, 10f, 1f);
            var final = gun.transform.position;
            Destroy(sparks.gameObject);
            Destroy(gun.gameObject);
            yield return new WaitForSeconds(0.4f);

            var real = ItemFactory.Spawn("pistola_portales", final, Quaternion.LookRotation(Vector3.right));
            real.Body.isKinematic = true;
            real.Grab.selectExited.AddListener(_ => real.Body.isKinematic = false);
            m_RealGun = real;
            FX.Burst(final, new Color(0.5f, 1f, 0.5f), 80, 2.5f, 0.04f, 1f, 0f, true);
            ScoreManager.Instance?.RegisterDiscovery("pistola_portales");
            Done = true;
            m_Running = false;
            gm?.OnPortalGunCreated(real);
        }
    }
}
