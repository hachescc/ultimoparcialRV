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
            var back = rig.FeetPosition;
            var fwd = rig.head != null ? rig.head.forward : Vector3.forward;
            var spots = gm.quantumSpots;
            if (spots == null || spots.Length == 0) yield break;
            var spot = spots[Random.Range(0, spots.Length)];
            ScreenFX.Instance?.Flash(new Color(0.6f, 1f, 0.6f), 0.05f, 0.5f);
            AudioFX.Play("portal", rig.HeadPosition, 0.7f);
            rig.TeleportTo(spot.position, spot.forward);
            WristWatch.Instance?.Notify("EXISTES EN OTRA REALIDAD (3 s)", new Color(0.6f, 1f, 0.6f), 3f);
            yield return new WaitForSeconds(3f);
            ScreenFX.Instance?.Flash(new Color(0.6f, 1f, 0.6f), 0.05f, 0.5f);
            rig.TeleportTo(back, fwd);
        }

        static IEnumerator CoEyeholes(Consumable box)
        {
            AudioFX.Play("alarm", box.transform.position, 1f);
            var gm = GameManager.Instance;
            var spot = gm != null ? gm.eyeholesSpot : null;
            GameObject man = null;
            if (spot != null)
            {
                man = new GameObject("Hombre Agujero de Ojo (placeholder)");
                man.transform.SetPositionAndRotation(spot.position, spot.rotation);
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(man.transform, false);
                head.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
                head.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.45f, 0.25f, 0.6f));
                for (int i = -1; i <= 1; i += 2)
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
            yield return new WaitForSeconds(1.2f);
            if (box != null) box.Item.Consume(0f, true);
            if (man != null) Destroy(man);
        }
    }
}
