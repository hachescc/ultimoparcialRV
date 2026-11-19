using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Comida: se "come" acercandola a la boca (colision con la cabeza).
    /// Szechuan = filtro VHS, Empanada Cuantica = teletransporte 3 s, Aguardiente/Papa = borrachera,
    /// Cereal Eyeholes = aparece el Hombre Agujero de Ojo y te arrebata la caja.
    /// </summary>
    public class Consumable : ItemBehaviour, IHeadInteractable
    {
        public enum Effect { Drunk, VHS, Teleport, Eyeholes }
        public Effect effect;

        bool m_Used;

        public void OnTouchHead(PlayerHead head)
        {
            if (m_Used || !Item.IsHeld) return;
            // El aguardiente es ingrediente de la receta final: avisamos pero se puede beber
            m_Used = true;
            var rig = PlayerRig.Instance;
            var gm = GameManager.Instance;

            switch (effect)
            {
                case Effect.VHS:
                    AudioFX.Play("chomp", head.transform.position, 0.8f);
                    ScreenFX.Instance?.VHS(10f);
                    RickComms.Instance?.Say("eat_szechuan", true);
                    Item.Consume(0f, false);
                    break;
                case Effect.Drunk:
                    AudioFX.Play(Item.itemId == "aguardiente" ? "bubbles" : "chomp", head.transform.position, 0.8f);
                    ScreenFX.Instance?.Drunk(12f);
                    RickComms.Instance?.Say(Item.itemId == "aguardiente" ? "eat_aguardiente" : "eat_papa");
                    Item.Consume(0f, false);
                    break;
                case Effect.Teleport:
                    AudioFX.Play("chomp", head.transform.position, 0.8f);
                    if (gm != null && rig != null) gm.StartCoroutine(CoTeleport(rig, gm));
                    Item.Consume(0f, false);
                    break;
                case Effect.Eyeholes:
                    if (gm != null) gm.StartCoroutine(CoEyeholes(this));
                    break;
            }
            GameEvents.RaiseBonus("Probaste: " + GameDatabase.NameOf(Item.itemId), 15);
        }

        static IEnumerator CoTeleport(PlayerRig rig, GameManager gm)
        {
            if (s_Teleporting) yield break;   // una segunda empanada no debe guardar el "otro lado" como punto de regreso
            var back = rig.FeetPosition;
            var fwd = rig.head != null ? rig.head.forward : Vector3.forward;
            var spots = gm.quantumSpots;
            if (spots == null || spots.Length == 0) yield break;
            s_Teleporting = true;
            var spot = spots[Random.Range(0, spots.Length)];
            ScreenFX.Instance?.Flash(new Color(0.6f, 1f, 0.6f), 0.05f, 0.5f);
            AudioFX.Play("portal", rig.HeadPosition, 0.7f);
            rig.TeleportTo(spot.position, spot.forward);
            WristWatch.Instance?.Notify("EXISTES EN OTRA REALIDAD (3 s)", new Color(0.6f, 1f, 0.6f), 3f);
            yield return new WaitForSeconds(3f);
            s_Teleporting = false;
            // si la partida termino mientras tanto, el jugador ya esta en la sala final: no lo devolvemos al garaje
            if (gm == null || rig == null || gm.State == GameState.Ending || gm.State == GameState.GameOver) yield break;
            ScreenFX.Instance?.Flash(new Color(0.6f, 1f, 0.6f), 0.05f, 0.5f);
            rig.TeleportTo(back, fwd);
        }
        static bool s_Teleporting;
        public static void ResetStatics() => s_Teleporting = false;

        static IEnumerator CoEyeholes(Consumable box)
        {
            AudioFX.Play("alarm", box.transform.position, 1f);
            var gm = GameManager.Instance;
            var spot = gm != null ? gm.eyeholesSpot : null;
            GameObject man = null;
            if (spot != null)
            {
                man = new GameObject("Hombre Agujero de Ojo");
                man.transform.SetPositionAndRotation(spot.position, spot.rotation);
                var reg = ItemVisualRegistry.Get();
                var entry = reg != null ? reg.Find("hombre_eyeholes") : null;
                if (entry != null)
                {
                    // modelo de cuerpo entero parado en el piso: se escala para que la cabeza quede a la altura de la ventana
                    var model = Instantiate(entry.prefab, man.transform);
                    float k = Mathf.Max(0.6f, (spot.position.y + 0.08f) / 1.92f);
                    model.transform.localScale = Vector3.one * k;
                    model.transform.position = new Vector3(spot.position.x, 0f, spot.position.z);
                    model.transform.rotation = spot.rotation;
                    man.AddComponent<EyeholeManShake>();
                }
                var head = entry != null ? new GameObject("SinCabezaPlaceholder") : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (entry != null) head.SetActive(false);
                head.transform.SetParent(man.transform, false);
                if (entry == null)
                {
                    Destroy(head.GetComponent<Collider>());
                    head.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
                    head.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.45f, 0.25f, 0.6f));
                }
                for (int i = -1; entry == null && i <= 1; i += 2)
                {
                    var hole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(hole.GetComponent<Collider>());
                    hole.transform.SetParent(head.transform, false);
                    hole.transform.localPosition = new Vector3(i * 0.2f, 0.12f, 0.42f);
                    hole.transform.localScale = Vector3.one * 0.18f;
                    hole.GetComponent<Renderer>().sharedMaterial = MatLib.Unlit(Color.black);
                }
                FX.FloatingText(spot.position + Vector3.up * 0.9f, "¡EYEHOLES!", new Color(1f, 0.8f, 0.2f), 1.5f, 2f, 0.1f);
            }
            AudioFX.Play("scream", spot != null ? spot.position : box.transform.position, 1f, 0.6f);
            yield return new WaitForSeconds(0.6f);
            PlayerRig.Instance?.Haptic(Hand.Both, 1f, 0.5f);
            RickComms.Instance?.Say("eyeholes");
            if (box != null)
            {
                box.Item.ForceRelease();
                yield return null;
                if (box != null && spot != null)
                {
                    var dir = (spot.position - box.transform.position).normalized + Vector3.up * 0.4f;
                    box.Body.AddForce(dir * 6f, ForceMode.VelocityChange);
                }
            }
            yield return new WaitForSeconds(2.4f);
            if (box != null) box.Item.Consume(0f, true);
            if (man != null) Destroy(man);
        }
    }

    /// <summary>El Hombre Agujero de Ojo golpea la ventana: sacudida corta y agresiva mientras esta en escena.</summary>
    public class EyeholeManShake : MonoBehaviour
    {
        Vector3 m_Base;
        Quaternion m_BaseRot;
        float m_T;
        void Start() { m_Base = transform.position; m_BaseRot = transform.rotation; }
        void Update()
        {
            m_T += Time.deltaTime;
            // embestidas hacia la ventana + temblor
            float lunge = Mathf.Abs(Mathf.Sin(m_T * 9f)) * 0.12f;
            transform.position = m_Base + (m_BaseRot * Vector3.forward) * lunge + Vector3.up * (Mathf.Sin(m_T * 23f) * 0.015f);
            transform.rotation = m_BaseRot * Quaternion.Euler(0, 0, Mathf.Sin(m_T * 17f) * 3f);
        }
    }
}
