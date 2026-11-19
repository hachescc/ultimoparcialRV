using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Soporte de ensamblaje final. Socket que solo acepta la Portal Gun de carton. Con la
    /// pistola asegurada, se le acercan los Cristales Kalaxianos y el Aguardiente: el carton
    /// arde con chispas verdes, levita, destello cegador y se transmuta en la Pistola de Portales real.
    /// </summary>
    public class FinalAssemblyStation : MonoBehaviour
    {
        public IngredientSocket socket;
        [Tooltip("Radio alrededor del socket donde deben estar los otros dos ingredientes")]
        public float zoneRadius = 0.35f;

        public bool Done { get; private set; }
        bool m_Running;
        bool m_HintGiven;
        MixableItem m_RealGun;

        public static FinalAssemblyStation Instance { get; private set; }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Devuelve la Pistola de Portales real al soporte por un micro-portal (no se puede perder).</summary>
        public static void ReturnGun(MixableItem gun)
        {
            if (gun == null) return;
            var st = Instance;
            var home = st != null && st.socket != null ? st.socket.transform.position + Vector3.up * 0.35f : Vector3.up * 1.3f;
            gun.ForceRelease();
            FX.Portal(gun.transform.position, Quaternion.identity, 0.15f, 0.8f);
            gun.Body.linearVelocity = Vector3.zero;
            gun.Body.angularVelocity = Vector3.zero;
            gun.Body.isKinematic = true;   // flota sobre el soporte hasta que la vuelvan a agarrar
            gun.transform.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.right));
            gun.Body.position = home;
            FX.Portal(home, Quaternion.identity, 0.15f, 0.8f);
            RickComms.Instance?.SayText("¡Morty! ¡Esa pistola es lo único que nos saca de aquí! Te la devolví al soporte.");
        }

        void Update()
        {
            // la pistola real no puede quedar fuera del mundo ni del perimetro
            var gm0 = GameManager.Instance;
            bool ended = gm0 != null && (gm0.State == GameState.Ending || gm0.State == GameState.GameOver);
            if (m_RealGun != null && !m_RealGun.IsHeld && !ended)
            {
                var p = m_RealGun.transform.position;
                if (p.y < -3f || (p - transform.position).sqrMagnitude > 45f * 45f) ReturnGun(m_RealGun);
            }
            if (Done || m_Running || socket == null) return;
            // el ensamblaje final solo existe en el juego libre (no en intro/tutorial ni despues del final)
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
            if (gun.Grab != null) gun.Grab.enabled = false;   // no se puede arrancar del aire mientras se transmuta
            yield return null;
            gun.Body.isKinematic = true;

            // El carton arde con chispas verdes
            var sparks = FX.Attach(gun.transform, new Color(0.4f, 1f, 0.3f), 80f, 0.03f, 1.5f, 0.5f, 0.1f);
            AudioFX.Play("zap", pos, 1f);
            PlayerRig.Instance?.HapticPattern(Hand.Both, 0.5f, 10, 0.08f, 0.05f);

            // Levita y tiembla
            var start = gun.transform.position;
            float t = 0;
            while (t < 2f)
            {
                t += Time.deltaTime;
                gun.transform.position = start + Vector3.up * Mathf.SmoothStep(0, 0.35f, t / 2f) + Random.insideUnitSphere * 0.01f * t;
                gun.transform.Rotate(0, 180f * Time.deltaTime * t, 0, Space.World);
                yield return null;
            }

            // Destello cegador
            ScreenFX.Instance?.Flash(Color.white, 0.6f, 2f);
            AudioFX.Play("explosion", pos, 0.6f, 1.5f);
            FX.FlashLight(pos, new Color(0.5f, 1f, 0.5f), 15f, 10f, 1f);
            var final = gun.transform.position;
            Destroy(sparks.gameObject);
            Destroy(gun.gameObject);
            yield return new WaitForSeconds(0.4f);

            var real = ItemFactory.Spawn("pistola_portales", final, Quaternion.LookRotation(Vector3.right));
            real.Body.isKinematic = true; // flota hasta que el jugador la agarre
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
